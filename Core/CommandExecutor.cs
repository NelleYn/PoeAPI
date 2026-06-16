using System;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Reflection;
using System.Threading.Tasks;
using System.Windows.Forms;
using ExileCore.Shared;

namespace ExileCore
{
    public static class CommandExecutor
    {
        public const string Offset = "offset";
        public const string OffsetS = "offsets";
        public const string LoaderOffsets = "loader_offsets";
        public const string CompilePlugins = "compile_plugins";
        public const string GameOffsets = "GameOffsets.dll";


        public static void Execute(string cmd)
        {
            switch (cmd)
            {
                case Offset:
                case OffsetS:
                    CreateOffsets(true);
                    return;
                case CompilePlugins:
                    CompilePluginsIntoDll();
                    return;
                case LoaderOffsets:
                    CreateOffsets();
                    return;
                default:
                    if (cmd.StartsWith("compile_"))
                    {
                        var directoryInfo = new DirectoryInfo(Path.Combine("Plugins", "Source"));
                        var plugin = cmd.Replace("compile_", "");
                        if (directoryInfo.GetDirectories().FirstOrDefault(x => x.Name.Equals(plugin, StringComparison.OrdinalIgnoreCase)) != null)
                            CompilePluginIntoDll(plugin);
                    }

                    return;
            }
        }

        private static void CompilePluginIntoDll(string plugin)
        {
            var rootDirectory = AppContext.BaseDirectory;
            var pathToSources = Path.Combine(rootDirectory, "Plugins", "Source");
            var directories = new DirectoryInfo(pathToSources).GetDirectories();
            var pluginDir = directories.FirstOrDefault(x => x.Name.Equals(plugin, StringComparison.OrdinalIgnoreCase));
            if (pluginDir == null)
            {
                DebugWindow.LogError($"{plugin} directory not found.");
                return;
            }

            CompileSourceIntoDll(pluginDir);
        }

        private static string[] _dllFiles;

        private static string[] GetAllDllFilesFromRootDirectory()
        {
            if (_dllFiles == null)
            {
                var rootDirInfo = new DirectoryInfo(AppContext.BaseDirectory);
                _dllFiles = rootDirInfo.GetFiles("*.dll", SearchOption.TopDirectoryOnly)
                    .Where(x => !x.Name.Equals("cimgui.dll") && x.Name.Count(c => c == '-' || c == '_') != 5)
                    .Select(x => x.FullName).ToArray();
            }

            return _dllFiles;
        }

        private static void CompileSourceIntoDll(DirectoryInfo info)
        {
            var sw = Stopwatch.StartNew();

            var csFiles = info.GetFiles("*.cs", SearchOption.AllDirectories).Select(x => x.FullName)
                .ToArray();
            var csProj = info.GetFiles("*.csproj", SearchOption.AllDirectories).FirstOrDefault();
            if (csProj == null)
            {
                MessageBox.Show($".csproj for plugin {info.Name} not found.", "Error", MessageBoxButtons.OK,
                    MessageBoxIcon.Error);
                return;
            }

            var compiledDir = info.FullName.Replace("\\Source\\", "\\Compiled\\");

            if (!Directory.Exists(compiledDir))
                Directory.CreateDirectory(compiledDir);

            var references = GetAllDllFilesFromRootDirectory().ToList();

            var libsFolder = Path.Combine(info.FullName, "libs");
            if (Directory.Exists(libsFolder))
                references.AddRange(Directory.GetFiles(libsFolder, "*.dll"));

            var result = RoslynCompiler.Compile(info.Name, csFiles, references);

            if (!result.Success)
            {
                MessageBox.Show($"{info.Name} -> Failed (Errors: {result.Errors.Count}) look in {info.FullName}/Errors.txt",
                    "Error", MessageBoxButtons.OK, MessageBoxIcon.Error);
                File.WriteAllText(Path.Combine(info.FullName, "Errors.txt"),
                    string.Join(Environment.NewLine, result.Errors));
                return;
            }

            File.WriteAllBytes(Path.Combine(compiledDir, $"{info.Name}.dll"), result.Dll);
            if (result.Pdb != null)
                File.WriteAllBytes(Path.Combine(compiledDir, $"{info.Name}.pdb"), result.Pdb);

            MessageBox.Show($"{info.Name}  >>> Successful <<< (Working time: {sw.ElapsedMilliseconds} ms.)");
        }


        private static void CreateOffsets(bool force = false)
        {
            var dllInfo = new FileInfo(GameOffsets);
            var dirInfo = new DirectoryInfo(Path.Combine(AppContext.BaseDirectory, "GameOffsets"));

            if (!dllInfo.Exists && !dirInfo.Exists)
            {
                MessageBox.Show("Offsets dll and folder not found.", "Error", MessageBoxButtons.OK,
                    MessageBoxIcon.Error);
                Environment.Exit(0);
                return;
            }

            if (!dirInfo.Exists)
            {
                if (force)
                    MessageBox.Show("Offsets folder not found.", "Error", MessageBoxButtons.OK, MessageBoxIcon.Error);

                return;
            }

            var filesNames = dirInfo.GetFiles("*.cs", SearchOption.AllDirectories).Select(x => x.FullName).ToArray();
            var shouldCompile = force;

            if (!shouldCompile)
                foreach (var filesName in filesNames)
                    if (new FileInfo(filesName).LastWriteTimeUtc > dllInfo.LastWriteTimeUtc)
                    {
                        shouldCompile = true;
                        break;
                    }

            if (!shouldCompile)
                return;

            var result = RoslynCompiler.Compile("GameOffsets", filesNames, GetAllDllFilesFromRootDirectory());

            if (!result.Success)
            {
                MessageBox.Show(string.Join(Environment.NewLine, result.Errors), "Error", MessageBoxButtons.OK,
                    MessageBoxIcon.Error);
                Environment.Exit(1);
                return;
            }

            File.WriteAllBytes(GameOffsets, result.Dll);
            Assembly.Load(result.Dll);
        }

        private static void CompilePluginsIntoDll()
        {
            var rootDirectory = AppContext.BaseDirectory;
            var pathToSources = Path.Combine(rootDirectory, "Plugins", "Source");
            var directories = new DirectoryInfo(pathToSources).GetDirectories();
            var directoryInfos = directories.Where(x => (x.Attributes & FileAttributes.Hidden) == 0).ToList();
            if (directoryInfos.Count == 0)
            {
                MessageBox.Show("Plugins/Source/ is empty.", "Error", MessageBoxButtons.OK, MessageBoxIcon.Error);
                return;
            }

            Parallel.ForEach(directoryInfos, CompileSourceIntoDll);
        }
    }
}
