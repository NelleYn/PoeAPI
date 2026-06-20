using System;
using System.ComponentModel;
using System.Runtime.InteropServices;
using System.Runtime.Serialization;

namespace ExileCore.Shared.PInvoke;

/// <summary>
/// Resolves native library modules and exported methods at runtime, returning managed delegates.
/// </summary>
internal static class DynamicImport
{
    [DllImport("kernel32.dll", EntryPoint = "GetModuleHandleW", SetLastError = true, CharSet = CharSet.Unicode)]
    private static extern IntPtr GetModuleHandle(string modulename);

    [DllImport("kernel32.dll", EntryPoint = "GetProcAddress", SetLastError = true, CharSet = CharSet.Ansi)]
    private static extern IntPtr GetProcAddress(IntPtr hModule, string procname);

    [DllImport("kernel32.dll", EntryPoint = "LoadLibraryW", SetLastError = true, CharSet = CharSet.Unicode)]
    private static extern IntPtr LoadLibrary(string lpFileName);

    /// <summary>
    /// Resolves an exported method from an already-loaded module and returns it as a managed delegate.
    /// </summary>
    /// <typeparam name="T">The delegate type matching the native function signature.</typeparam>
    /// <param name="moduleHandle">The handle of the loaded module.</param>
    /// <param name="methodName">The name of the exported method.</param>
    /// <returns>A delegate bound to the native method.</returns>
    public static T Import<T>(IntPtr moduleHandle, string methodName)
    {
        var address = ImportMethod(moduleHandle, methodName);

        return Marshal.GetDelegateForFunctionPointer<T>(address);
    }

    /// <summary>
    /// Loads (or finds) a library and resolves an exported method, returning it as a managed delegate.
    /// </summary>
    /// <typeparam name="T">The delegate type matching the native function signature.</typeparam>
    /// <param name="libraryName">The name of the library to import from.</param>
    /// <param name="methodName">The name of the exported method.</param>
    /// <returns>A delegate bound to the native method.</returns>
    public static T Import<T>(string libraryName, string methodName)
    {
        var address = ImportMethod(libraryName, methodName);

        return Marshal.GetDelegateForFunctionPointer<T>(address);
    }

    /// <summary>
    /// Returns a handle to the named library, loading it if it is not already mapped.
    /// </summary>
    /// <param name="libraryName">The name of the library.</param>
    /// <returns>The module handle.</returns>
    /// <exception cref="DynamicImportException">Thrown when the library cannot be found or loaded.</exception>
    public static IntPtr ImportLibrary(string libraryName)
    {
        if (libraryName == string.Empty) throw new ArgumentOutOfRangeException(nameof(libraryName));

        var hModule = GetModuleHandle(libraryName);

        if (hModule == IntPtr.Zero) hModule = LoadLibrary(libraryName);

        if (hModule == IntPtr.Zero)
            throw new DynamicImportException("DynamicImport failed to import library \"" + libraryName + "\"!");

        return hModule;
    }

    /// <summary>
    /// Returns the address of an exported method within the given module.
    /// </summary>
    /// <param name="moduleHandle">The handle of the loaded module.</param>
    /// <param name="methodName">The name of the exported method.</param>
    /// <returns>The procedure address.</returns>
    /// <exception cref="DynamicImportException">Thrown when the method cannot be found.</exception>
    public static IntPtr ImportMethod(IntPtr moduleHandle, string methodName)
    {
        if (moduleHandle == IntPtr.Zero) throw new ArgumentOutOfRangeException(nameof(moduleHandle));
        if (string.IsNullOrEmpty(methodName)) throw new ArgumentOutOfRangeException(nameof(methodName));

        var procAddress = GetProcAddress(moduleHandle, methodName);

        if (procAddress == IntPtr.Zero)
        {
            throw new DynamicImportException("DynamicImport failed to find method \"" + methodName + "\" in module \"0x" +
                                             moduleHandle.ToString("X") + "\"!");
        }

        return procAddress;
    }

    /// <summary>
    /// Loads (or finds) a library and returns the address of an exported method.
    /// </summary>
    /// <param name="libraryName">The name of the library.</param>
    /// <param name="methodName">The name of the exported method.</param>
    /// <returns>The procedure address.</returns>
    public static IntPtr ImportMethod(string libraryName, string methodName)
    {
        return ImportMethod(ImportLibrary(libraryName), methodName);
    }
}

/// <summary>
/// Exception thrown when a <see cref="DynamicImport" /> operation fails.
/// </summary>
internal class DynamicImportException : Win32Exception
{
    protected DynamicImportException(SerializationInfo info, StreamingContext context) : base(info, context)
    {
    }

    public DynamicImportException()
    {
    }

    public DynamicImportException(int error) : base(error)
    {
    }

    public DynamicImportException(string message) : base(message + Environment.NewLine + "ErrorCode: " + Marshal.GetLastWin32Error())
    {
    }

    public DynamicImportException(int error, string message) : base(error, message)
    {
    }

    public DynamicImportException(string message, Exception innerException) : base(message, innerException)
    {
    }
}
