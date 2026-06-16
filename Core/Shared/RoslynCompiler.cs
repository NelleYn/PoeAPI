using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp;

namespace ExileCore.Shared
{
    public static class RoslynCompiler
    {
        public sealed class CompileResult
        {
            public byte[] Dll { get; set; }
            public byte[] Pdb { get; set; }
            public IReadOnlyList<string> Errors { get; set; } = Array.Empty<string>();
            public bool Success => Errors.Count == 0 && Dll != null;
        }

        public static CompileResult Compile(string assemblyName, IEnumerable<string> sourceFiles,
            IEnumerable<string> referencePaths)
        {
            var parseOptions = new CSharpParseOptions(LanguageVersion.Latest);

            var syntaxTrees = sourceFiles
                .Select(f => CSharpSyntaxTree.ParseText(File.ReadAllText(f), parseOptions, f))
                .ToList();

            var compilation = CSharpCompilation.Create(
                assemblyName,
                syntaxTrees,
                BuildReferences(referencePaths),
                new CSharpCompilationOptions(
                    OutputKind.DynamicallyLinkedLibrary,
                    optimizationLevel: OptimizationLevel.Release,
                    allowUnsafe: true,
                    platform: Platform.X64));

            using var dllStream = new MemoryStream();
            using var pdbStream = new MemoryStream();
            var emitResult = compilation.Emit(dllStream, pdbStream);

            if (!emitResult.Success)
            {
                return new CompileResult
                {
                    Errors = emitResult.Diagnostics
                        .Where(d => d.Severity == DiagnosticSeverity.Error)
                        .Select(d => d.ToString())
                        .ToList()
                };
            }

            return new CompileResult { Dll = dllStream.ToArray(), Pdb = pdbStream.ToArray() };
        }

        private static List<MetadataReference> BuildReferences(IEnumerable<string> referencePaths)
        {
            var refs = new Dictionary<string, MetadataReference>(StringComparer.OrdinalIgnoreCase);

            void Add(string path)
            {
                if (string.IsNullOrWhiteSpace(path)) return;
                var key = Path.GetFileName(path);
                if (refs.ContainsKey(key) || !File.Exists(path)) return;
                try { refs[key] = MetadataReference.CreateFromFile(path); }
                catch { }
            }

            var trustedAssemblies = AppContext.GetData("TRUSTED_PLATFORM_ASSEMBLIES") as string ?? "";
            foreach (var path in trustedAssemblies.Split(Path.PathSeparator))
                Add(path);

            foreach (var path in referencePaths)
                Add(path);

            return refs.Values.ToList();
        }
    }
}
