using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Reflection;
using System.Threading;
using System.Threading.Tasks;
using ExileCore.PoEMemory.MemoryObjects;
using ExileCore.Shared.Helpers;
using ExileCore.Shared.Interfaces;
using JM.LinqFaster;
using MoreLinq.Extensions;
using SharpDX;

namespace ExileCore.Shared
{
    public class PluginManager
    {
        private const string PluginsDirectory = "Plugins";
        private const string CompiledPluginsDirectory = "Compiled";
        private const string SourcePluginsDirectory = "Source";
        private readonly GameController _gameController;
        private readonly Graphics _graphics;
        private readonly MultiThreadManager _multiThreadManager;
        private readonly Dictionary<string, string> Directories = new Dictionary<string, string>();
        private  Dictionary<string, Stopwatch> PluginLoadTime { get; } = new Dictionary<string, Stopwatch>();
        private bool parallelLoading = false;

        object locker = new object();
        public PluginManager(GameController gameController, Graphics graphics, MultiThreadManager multiThreadManager)
        {
            _gameController = gameController;
            _graphics = graphics;
            _multiThreadManager = multiThreadManager;
            RootDirectory = AppDomain.CurrentDomain.BaseDirectory;
            Directories["Temp"] = Path.Combine(RootDirectory, PluginsDirectory, "Temp");
            Directories[PluginsDirectory] = Path.Combine(RootDirectory, PluginsDirectory);

            Directories[CompiledPluginsDirectory] =
                Path.Combine(Directories[PluginsDirectory], CompiledPluginsDirectory);

            Directories[SourcePluginsDirectory] = Path.Combine(Directories[PluginsDirectory], SourcePluginsDirectory);
            _gameController.EntityListWrapper.EntityAdded += EntityListWrapperOnEntityAdded;
            _gameController.EntityListWrapper.EntityRemoved += EntityListWrapperOnEntityRemoved;
            _gameController.EntityListWrapper.EntityAddedAny += EntityListWrapperOnEntityAddedAny;
            _gameController.EntityListWrapper.EntityIgnored += EntityListWrapperOnEntityIgnored;
            _gameController.Area.OnAreaChange += AreaOnOnAreaChange;

            parallelLoading = _gameController.Settings.CoreSettings.MultiThreadLoadPlugins;

            foreach (var directory in Directories)
            {
                if (!Directory.Exists(directory.Value))
                {
                    DebugWindow.LogMsg($"{directory.Value} doesn't exists, but don't worry i created it for you.");
                    Directory.CreateDirectory(directory.Value);
                }
            }

            var (compiledPlugins, sourcePlugins) = SearchPlugins();
            List<(Assembly asm, DirectoryInfo directoryInfo)> assemblies = new List<(Assembly, DirectoryInfo)>();

            Task task = null;
            if (sourcePlugins.Length > 0)
            {
                task = Task.Run(() =>
                {
                    var compilePluginsFromSource = CompilePluginsFromSource(sourcePlugins);
                });
            }
            

            var compiledAssemblies = GetCompiledAssemblies(compiledPlugins, parallelLoading);
           
            var devTree = Plugins.FirstOrDefault(x=>x.Name.Equals("DevTree"));
            task?.Wait();
            Plugins = Plugins.OrderBy(x => x.Order).ThenByDescending(x => x.CanBeMultiThreading).ThenBy(x => x.Name)
                .ToList();

            if (devTree != null)
            {
                try
                {
                    var fieldInfo = devTree.Plugin.GetType().GetField("Plugins");
                    List<PluginWrapper> devTreePlugins() => Plugins;
                    fieldInfo.SetValue(devTree.Plugin,(Func<List<PluginWrapper>>) devTreePlugins);
                } 
                catch (Exception e)
                {
                    LogError(e.ToString());
                }
                
            }
            if (parallelLoading)
            {
                //Pre init some general objects because with multi threading load they can null sometimes for some plugin
                var ingameStateIngameUi = gameController.IngameState.IngameUi;
                var ingameStateData = gameController.IngameState.Data;
                var ingameStateServerData = gameController.IngameState.ServerData;
                Parallel.ForEach(Plugins, wrapper => wrapper.Initialise(gameController));
            }
            else
                Plugins.ForEach(wrapper => wrapper.Initialise(gameController));

            AreaOnOnAreaChange(gameController.Area.CurrentArea);
            Plugins.ForEach(x=> x.SubscrideOnFile(HotReloadDll));
            AllPluginsLoaded = true;
        }

        public bool AllPluginsLoaded { get; }
        public string RootDirectory { get; }
        public List<PluginWrapper> Plugins { get; } = new List<PluginWrapper>();

        private List<(Assembly loadedAssembly, DirectoryInfo directoryInfoinfo)> GetCompiledAssemblies(DirectoryInfo[] compiledPlugins, bool parallel)
        {
             object lockerLoadAsm = new object();
            var results = new List<(Assembly loadedAssembly, DirectoryInfo info)>(compiledPlugins.Length);
            void Load(DirectoryInfo info)
            {
                var loadedAssembly = LoadAssembly(info);
                if (loadedAssembly != null)
                {
                    TryLoadPlugin((loadedAssembly,info));
                }
            }
            
            if (parallel)
            {
                Parallel.ForEach(compiledPlugins, Load);
            }
            else
            {
                compiledPlugins.ForEach(Load);
            }

            return results;
        }

        private Assembly LoadAssembly(DirectoryInfo dir)
        {
            try
            {
                var directoryInfo = new DirectoryInfo(dir.FullName);
                if (!directoryInfo.Exists)
                {
                    LogError($"Directory - {dir} not found.");
                    return null;
                }

                var dll = directoryInfo.GetFiles($"{directoryInfo.Name}*.dll", SearchOption.TopDirectoryOnly)
                    .FirstOrDefault();

                if (dll == null)
                {
                    LogError($"Not found plugin dll in {dir.FullName}. (Dll should be like folder)");
                    return null;
                }

                var pdbPath = dll.FullName.Replace(".dll", ".pdb");
                var pdbExists = File.Exists(pdbPath);

                var dllBytes = File.ReadAllBytes(dll.FullName);
                var asm = pdbExists ? Assembly.Load(dllBytes, File.ReadAllBytes(pdbPath)) : Assembly.Load(dllBytes);

                return asm;
            }
            catch (Exception e)
            {
                LogError($"{nameof(LoadAssembly)} -> {e}");
                return null;
            }
        }


        /// <summary>
        /// Compiles a single source plugin directory into an in-memory assembly using
        /// <see cref="RoslynCompiler"/>. On failure the diagnostics are logged and written to an
        /// <c>Errors.txt</c> file inside the plugin directory. Returns <c>null</c> on failure.
        /// </summary>
        private Assembly CompilePlugin(DirectoryInfo info, string[] dllFiles)
        {
            if (info == null || !info.Exists)
            {
                LogError($"{nameof(CompilePlugin)} -> plugin directory not found.");
                return null;
            }

            var csFiles = info.GetFiles("*.cs", SearchOption.AllDirectories).Select(x => x.FullName)
                .ToArray();

            var references = new List<string>(dllFiles ?? Array.Empty<string>());

            var libsFolder = Path.Combine(info.FullName, "libs");
            if (Directory.Exists(libsFolder))
                references.AddRange(Directory.GetFiles(libsFolder, "*.dll"));

            var result = RoslynCompiler.Compile(info.Name, csFiles, references);

            if (!result.Success)
            {
                foreach (var error in result.Errors)
                    Logger.Log.Error($"{info.Name} -> {error}");

                try
                {
                    File.WriteAllText(Path.Combine(info.FullName, "Errors.txt"),
                        string.Join(Environment.NewLine, result.Errors));
                }
                catch (Exception e)
                {
                    LogError($"{info.Name} -> failed to write Errors.txt: {e.Message}");
                }

                return null;
            }

            return result.Pdb != null
                ? Assembly.Load(result.Dll, result.Pdb)
                : Assembly.Load(result.Dll);
        }

        private List<(Assembly CompiledAssembly, DirectoryInfo directoryInfoinfo)> CompilePluginsFromSource(DirectoryInfo[] sourcePlugins)
        {
            using (new PerformanceTimer("Compile and load source plugins"))
            {
                var rootDirInfo = new DirectoryInfo(RootDirectory);

                var dllFiles = rootDirInfo.GetFiles("*.dll", SearchOption.TopDirectoryOnly)
                    .WhereF(x => !x.Name.Equals("cimgui.dll") && x.Name.Count(c => c == '-' || c == '_') != 5)
                    .SelectF(x => x.FullName).ToArray();

                var results = new List<(Assembly CompiledAssembly, DirectoryInfo info)>(sourcePlugins.Length);

                void CompileAndLoad(DirectoryInfo info)
                {
                    using (new PerformanceTimer($"Compile and load source plugin: {info.Name}"))
                    {
                        (Assembly ass, DirectoryInfo info) valueTuple = (CompilePlugin(info, dllFiles), info);
                        if (valueTuple.ass != null)
                        {
                            TryLoadPlugin(valueTuple);
                        }
                    }
                }

                if (parallelLoading)
                    Parallel.ForEach(sourcePlugins, CompileAndLoad);
                else
                    foreach (var info in sourcePlugins)
                        CompileAndLoad(info);

                return results;
            }
        }

        private void TryLoadPlugin((Assembly asm, DirectoryInfo directoryInfo) tuple)
        {
            try
            {
                var dir = tuple.asm.ManifestModule.ScopeName.Replace(".dll", "");

                var fullPath = tuple.directoryInfo.FullName;

                var types = tuple.asm.GetTypes();
                if (types.Length == 0)
                {
                    LogError($"Not found any types in plugin {fullPath}");
                    return;
                }

                var classesWithIPlugin = types.WhereF(type => typeof(IPlugin).IsAssignableFrom(type));
                var settings = types.FirstOrDefaultF(type => typeof(ISettings).IsAssignableFrom(type));

                if (settings == null)
                {
                    LogError("Not found setting class");
                    return;
                }


                foreach (var type in classesWithIPlugin)
                {
                    if (type.IsAbstract) continue;

                    if (Activator.CreateInstance(type) is IPlugin instance)
                    {
                        instance.DirectoryName = dir;
                        instance.DirectoryFullName = fullPath;
                        var pluginWrapper = new PluginWrapper(instance);
                        pluginWrapper.SetApi(_gameController, _graphics, this);
                        pluginWrapper.LoadSettings();
                        pluginWrapper.Onload();
                        var sw = PluginLoadTime[tuple.directoryInfo.FullName];
                        sw.Stop();
                        var elapsedTotalMilliseconds = sw.Elapsed.TotalMilliseconds;
                        pluginWrapper.LoadedTime = elapsedTotalMilliseconds;
                        DebugWindow.LogMsg($"{pluginWrapper.Name} loaded in {elapsedTotalMilliseconds} ms.",1,Color.Orange);
                        lock (locker)
                        {
                            Plugins.Add(pluginWrapper);
                        }
                    }
                }
            }
            catch (Exception e)
            {
                LogError($"Error when load plugin ({tuple.asm.ManifestModule.ScopeName}): {e})");
            }
        }
        
        
        private void HotReloadDll(PluginWrapper wrapper, FileSystemEventArgs args)
        {
            try
            {
                var fullPath = args.FullPath;
                if (!fullPath.EndsWith(".dll"))
                {
                    return;
                }
                var strings = fullPath.Split('\\');
                var firstF = strings.LastF().Split('.').FirstF();
                if (firstF != strings[strings.Length - 2])
                {
                    return;
                }

                //TODO DOUBLE LOAD
                if (Math.Abs((DateTime.UtcNow-wrapper.LastWrite).TotalSeconds)<2)
                    return;
                wrapper.LastWrite = DateTime.UtcNow;
                Core.MainRunner.Run(new Coroutine(() =>
                {
                    var fileInfo = new FileInfo(fullPath);
                    var directoryInfo = fileInfo.Directory;
                    var asm = LoadAssembly(directoryInfo);
                    if (asm == null)
                    {
                        LogError($"{firstF} cant load assembly for reloading.");
                        return;
                    }

                    var types = asm.GetTypes();
                    if (types.Length == 0)
                    {
                        LogError($"Not found any types in plugin {fullPath}");
                        return;
                    }

                    var classesWithIPlugin = types.WhereF(type => typeof(IPlugin).IsAssignableFrom(type));
                    var settings = types.FirstOrDefaultF(type => typeof(ISettings).IsAssignableFrom(type));

                    if (settings == null)
                    {
                        LogError($"Not found setting class in plugin {fullPath}");
                        return;
                    }

                    foreach (var type in classesWithIPlugin)
                    {
                        if (type.IsAbstract) continue;

                        if (Activator.CreateInstance(type) is IPlugin instance)
                        {
                            wrapper.ReloadPlugin(instance, _gameController, _graphics, this);
                        }
                    }
                }, new WaitTime(1000), null, $"Reload: {firstF}", false){SyncModWork = true});



            }
            catch (Exception e)
            {
                DebugWindow.LogError($"HotRealod error: {e}");
            }
        }
        //By default priority - Compiled 
        private (DirectoryInfo[] CompiledDirectories, DirectoryInfo[] SourceDirectories) SearchPlugins()
        {
            var CompiledDirectories = new DirectoryInfo(Directories[CompiledPluginsDirectory]).GetDirectories();

            var directoryInfos = new DirectoryInfo(Directories[SourcePluginsDirectory]).GetDirectories().Where(x => (x.Attributes & FileAttributes.Hidden) == 0).ToList();
            for (var index = 0; index < directoryInfos.Count; index++)
            {
                var directoryInfo = directoryInfos[index];
                if (directoryInfo.GetFiles().AnyF(x => x.Name.Equals("Errors.txt", StringComparison.Ordinal)))
                {
                     directoryInfos.RemoveAt(index);
                     DebugWindow.LogMsg($"Skip {directoryInfo.Name} for loading from source because file Errors.txt exists.");
                }
            }

            var SourceDirectories = directoryInfos
                .ExceptBy(CompiledDirectories, info => info.Name)
                .ToArray();
            foreach (var info in CompiledDirectories)
            {
                PluginLoadTime[info.FullName] = Stopwatch.StartNew();
            }

            foreach (var info in SourceDirectories)
            {
                PluginLoadTime[info.FullName] = Stopwatch.StartNew();
            }
            return (CompiledDirectories, SourceDirectories);
        }

        public void CloseAllPlugins()
        {
            foreach (var plugin in Plugins)
            {
                plugin.Close();
            }
        }

        private void AreaOnOnAreaChange(AreaInstance area)
        {
            foreach (var plugin in Plugins)
            {
                if (plugin.IsEnable)
                    plugin.AreaChange(area);
            }
        }

        private void EntityListWrapperOnEntityIgnored(Entity entity)
        {
            foreach (var plugin in Plugins)
            {
                if (plugin.IsEnable)
                    plugin.EntityIgnored(entity);
            }
        }

        private void EntityListWrapperOnEntityAddedAny(Entity entity)
        {
            foreach (var plugin in Plugins)
            {
                if (plugin.IsEnable)
                    plugin.EntityAddedAny(entity);
            }
        }

        private void EntityListWrapperOnEntityAdded(Entity entity)
        {
            if (_gameController.Settings.CoreSettings.AddedMultiThread && _multiThreadManager.ThreadsCount > 0)
            {
                var listJob = new List<Job>();

                Plugins.WhereF(x => x.IsEnable).Batch(_multiThreadManager.ThreadsCount)
                    .ForEach(wrappers =>
                        listJob.Add(_multiThreadManager.AddJob(() => wrappers.ForEach(x => x.EntityAdded(entity)),
                            "Entity added")));

                _multiThreadManager.Process(this);
                SpinWait.SpinUntil(() => listJob.AllF(x => x.IsCompleted), 500);
            }
            else
            {
                foreach (var plugin in Plugins)
                {
                    if (plugin.IsEnable)
                        plugin.EntityAdded(entity);
                }
            }
        }

        private void EntityListWrapperOnEntityRemoved(Entity entity)
        {
            foreach (var plugin in Plugins)
            {
                if (plugin.IsEnable)
                    plugin.EntityRemoved(entity);
            }
        }

        private void LogError(string msg)
        {
            DebugWindow.LogError(msg, 5);
        }
        
        public void ReceivePluginEvent(string eventId, object args, IPlugin owner)
        {
            foreach (var pluginWrapper in Plugins)
            {
                if (pluginWrapper.IsEnable && pluginWrapper.Plugin != owner)
                    pluginWrapper.ReceiveEvent(eventId, args);
            }
        }
    }
}
