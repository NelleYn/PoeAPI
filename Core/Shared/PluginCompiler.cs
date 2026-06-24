// NOTE: full implementation not recoverable from the protected DLL; emitted as a signature-only stub.
namespace ExileCore.Shared;
public partial class PluginCompiler
{
    public static System.Threading.SemaphoreSlim BuildSemaphore;
    public System.Lazy<Microsoft.Build.Execution.BuildManager> _buildManager;
    public static System.String CompilationManifestFileName;
    public static System.Boolean IsEnabled
    {
        get
        {
            throw new global::System.NotImplementedException();
        }
    }

    public static ExileCore.Shared.PluginCompiler Create()
    {
        throw new global::System.NotImplementedException();
    }

    public static ExileCore.Shared.PluginCompiler CreateOrThrow()
    {
        throw new global::System.NotImplementedException();
    }

    public static ExileCore.Shared.PluginCompiler.InputManifest GetManifestHashes(ExileCore.Shared.PluginCompiler.InputManifestFileList input, System.String projectDirectory)
    {
        throw new global::System.NotImplementedException();
    }

    public static ExileCore.Shared.PluginCompiler.OutputManifest GetManifestHashes(ExileCore.Shared.PluginCompiler.OutputManifestFileList input, System.String outputDirectory)
    {
        throw new global::System.NotImplementedException();
    }

    public static ExileCore.Shared.PluginCompiler.OutputManifestFileList GetCheckedOutputFiles(ExileCore.Shared.PluginCompiler.InputManifest inputManifest, System.String outputDirectory)
    {
        throw new global::System.NotImplementedException();
    }

    public static System.Boolean ManifestsAreEqual(ExileCore.Shared.PluginCompiler.Manifest manifest1, ExileCore.Shared.PluginCompiler.Manifest manifest2, System.Boolean ignoreReferenceHashes)
    {
        throw new global::System.NotImplementedException();
    }

    public static void DeleteCacheManifest(System.String directory)
    {
        throw new global::System.NotImplementedException();
    }

    public System.Collections.Generic.Dictionary<System.String, System.Exception> CompilePlugins(System.ValueTuple[] plugins, System.Boolean cacheCompilationResults, System.Boolean ignoreReferenceHashChanges)
    {
        throw new global::System.NotImplementedException();
    }

    public static System.Exception BuildException(ExileCore.Shared.MsBuildLogger logger, Microsoft.Build.Execution.ProjectInstance projectInstance, Microsoft.Build.Execution.BuildResult buildResult)
    {
        throw new global::System.NotImplementedException();
    }

    public void CompilePlugin(System.IO.FileInfo csProj, System.String outputDirectory)
    {
        throw new global::System.NotImplementedException();
    }

    public static void PatchProject(Microsoft.Build.Construction.ProjectRootElement pre)
    {
        throw new global::System.NotImplementedException();
    }

    public static void ReplacePackageVersion(Microsoft.Build.Construction.ProjectRootElement pre, System.String packageName, System.Type typeFromPackage, System.Int32 versionParts)
    {
        throw new global::System.NotImplementedException();
    }

    public void Dispose()
    {
        throw new global::System.NotImplementedException();
    }
}