using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp;

namespace ExileCore.Shared;

/// <summary>
/// Compiles plugin and offset source files into an in-memory assembly using the
/// Roslyn (Microsoft.CodeAnalysis) C# compiler. Replaces the legacy CodeDom based compiler.
/// </summary>
public static class RoslynCompiler
{
    /// <summary>
    /// Result of a <see cref="Compile"/> invocation. Carries the emitted assembly and debug
    /// symbol bytes on success, or the collected compiler error messages on failure.
    /// </summary>
    public sealed class CompileResult
    {
        /// <summary>The emitted assembly bytes, or <c>null</c> when compilation failed.</summary>
        public byte[] Dll { get; set; }

        /// <summary>The emitted debug-symbol (PDB) bytes, or <c>null</c> when unavailable.</summary>
        public byte[] Pdb { get; set; }

        /// <summary>
        /// The collected compiler error messages. Never <c>null</c>; empty on success.
        /// Each entry includes the diagnostic id, severity, location and message.
        /// </summary>
        public IReadOnlyList<string> Errors { get; set; } = Array.Empty<string>();

        /// <summary>
        /// <c>true</c> when the assembly was emitted and no errors were collected.
        /// </summary>
        public bool Success => (Errors == null || Errors.Count == 0) && Dll != null;
    }

    /// <summary>
    /// Compiles the supplied C# source files into a dynamically linked library assembly.
    /// </summary>
    /// <param name="assemblyName">The name of the assembly to produce.</param>
    /// <param name="sourceFiles">
    /// Paths of the <c>.cs</c> files to compile. Missing, empty or unreadable paths are reported
    /// as errors rather than throwing.
    /// </param>
    /// <param name="referencePaths">
    /// Paths of additional assemblies to reference, in addition to the trusted platform assemblies.
    /// May be <c>null</c>.
    /// </param>
    /// <returns>
    /// A <see cref="CompileResult"/> describing either the emitted assembly bytes or the collected
    /// compiler errors. The method never throws for invalid input; failures are surfaced through
    /// <see cref="CompileResult.Errors"/>.
    /// </returns>
    public static CompileResult Compile(string assemblyName, IEnumerable<string> sourceFiles,
        IEnumerable<string> referencePaths)
    {
        if (string.IsNullOrWhiteSpace(assemblyName))
            return Failure($"{nameof(assemblyName)} must not be null or empty.");

        if (sourceFiles == null)
            return Failure($"{nameof(sourceFiles)} must not be null.");

        var parseOptions = new CSharpParseOptions(LanguageVersion.Latest);

        var errors = new List<string>();
        var syntaxTrees = new List<SyntaxTree>();

        foreach (var file in sourceFiles)
        {
            if (string.IsNullOrWhiteSpace(file))
            {
                errors.Add("Encountered a null or empty source file path.");
                continue;
            }

            if (!File.Exists(file))
            {
                errors.Add($"Source file not found: {file}");
                continue;
            }

            try
            {
                var text = File.ReadAllText(file);
                syntaxTrees.Add(CSharpSyntaxTree.ParseText(text, parseOptions, file));
            }
            catch (Exception e)
            {
                errors.Add($"Failed to read source file '{file}': {e.Message}");
            }
        }

        if (errors.Count > 0)
            return new CompileResult { Errors = errors };

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
            return new CompileResult { Errors = FormatErrors(emitResult.Diagnostics) };

        return new CompileResult { Dll = dllStream.ToArray(), Pdb = pdbStream.ToArray() };
    }

    /// <summary>
    /// Projects the error-severity diagnostics into human-readable strings, falling back to a
    /// generic message when the emit failed without reporting any error diagnostic.
    /// </summary>
    private static List<string> FormatErrors(IEnumerable<Diagnostic> diagnostics)
    {
        var errors = diagnostics
            .Where(d => d.Severity == DiagnosticSeverity.Error)
            .Select(Describe)
            .ToList();

        if (errors.Count == 0)
            errors.Add("Compilation failed but no error diagnostics were reported.");

        return errors;
    }

    /// <summary>
    /// Builds a single diagnostic line containing its id, severity, source location and message.
    /// </summary>
    private static string Describe(Diagnostic diagnostic)
    {
        var location = diagnostic.Location.GetLineSpan();
        var where = location.IsValid
            ? $"{location.Path}({location.StartLinePosition.Line + 1},{location.StartLinePosition.Character + 1})"
            : "<no location>";

        return $"{diagnostic.Id} [{diagnostic.Severity}] {where}: {diagnostic.GetMessage()}";
    }

    /// <summary>Creates a failed <see cref="CompileResult"/> carrying a single error message.</summary>
    private static CompileResult Failure(string message) =>
        new CompileResult { Errors = new[] { message } };

    /// <summary>
    /// Gathers metadata references from the runtime's trusted platform assemblies and the supplied
    /// <paramref name="referencePaths"/>, de-duplicating by file name and ignoring missing or
    /// unreadable files.
    /// </summary>
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

        if (referencePaths != null)
            foreach (var path in referencePaths)
                Add(path);

        return refs.Values.ToList();
    }
}
