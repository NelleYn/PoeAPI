using System;
using System.ComponentModel;
using System.Diagnostics;
using System.Runtime.InteropServices;
using System.Text;
using ExileCore.Shared.Enums;

namespace ExileCore.Shared.SomeMagic;

/// <summary>
/// Managed wrappers around the Win32 process and memory APIs that translate failures into exceptions.
/// </summary>
public static unsafe class NativeMethods
{
    /// <summary>
    /// When <see langword="true" />, read/write failures are logged with a stack trace instead of being silently ignored.
    /// </summary>
    public static bool LogError = false;

    /// <summary>
    /// Opens the process with the given id and access rights.
    /// </summary>
    /// <param name="pId">The process id.</param>
    /// <param name="accessRights">The requested access rights.</param>
    /// <returns>A safe handle to the opened process.</returns>
    /// <exception cref="Win32Exception">Thrown when the process cannot be opened.</exception>
    public static SafeMemoryHandle OpenProcess(int pId, ProcessAccessRights accessRights = ProcessAccessRights.PROCESS_ALL_ACCESS)
    {
        var processHandle = Imports.OpenProcess(accessRights, false, pId);

        if (processHandle == null || processHandle.IsInvalid || processHandle.IsClosed)
        {
            throw new Win32Exception(
                $"[Error Code: {Marshal.GetLastWin32Error()}] Unable to open process {pId} with access {accessRights:X}");
        }

        return processHandle;
    }

    /// <summary>
    /// Returns the process id associated with the given process handle.
    /// </summary>
    /// <param name="processHandle">The process handle.</param>
    /// <returns>The process id.</returns>
    /// <exception cref="Win32Exception">Thrown when the id cannot be determined.</exception>
    public static int GetProcessId(SafeMemoryHandle processHandle)
    {
        var pId = Imports.GetProcessId(processHandle);

        if (pId == 0)
        {
            throw new Win32Exception(
                $"[Error Code: {Marshal.GetLastWin32Error()}] Unable to get Id from process handle 0x{processHandle.DangerousGetHandle().ToString("X")}");
        }

        return pId;
    }

    /// <summary>
    /// Determines whether the given process is running as a native 64-bit process.
    /// </summary>
    /// <param name="processHandle">The process handle.</param>
    /// <returns><see langword="true" /> if the process is 64-bit; otherwise, <see langword="false" />.</returns>
    /// <exception cref="Win32Exception">Thrown when the WOW64 status cannot be determined.</exception>
    public static bool Is64BitProcess(SafeMemoryHandle processHandle)
    {
        if (!Imports.IsWow64Process(processHandle, out var Is64BitProcess))
        {
            throw new Win32Exception(
                $"[Error Code: {Marshal.GetLastWin32Error()}] Unable to determine if process handle 0x{processHandle.DangerousGetHandle().ToString("X")} is 64 bit");
        }

        return !Is64BitProcess;
    }

    /// <summary>
    /// Returns the window class name for the given window handle.
    /// </summary>
    /// <param name="windowHandle">The window handle.</param>
    /// <returns>The window class name.</returns>
    /// <exception cref="Win32Exception">Thrown when the class name cannot be retrieved.</exception>
    public static string GetClassName(IntPtr windowHandle)
    {
        var stringBuilder = new StringBuilder(char.MaxValue);

        if (Imports.GetClassName(windowHandle, stringBuilder, stringBuilder.Capacity) == 0)
        {
            throw new Win32Exception(
                $"[Error Code: {Marshal.GetLastWin32Error()}] Unable to get class name from window handle 0x{windowHandle.ToString("X")}");
        }

        return stringBuilder.ToString();
    }

    /// <summary>
    /// Closes the given native handle.
    /// </summary>
    /// <param name="handle">The handle to close.</param>
    /// <returns><see langword="true" /> when the handle is closed successfully.</returns>
    /// <exception cref="Win32Exception">Thrown when the handle cannot be closed.</exception>
    public static bool CloseHandle(IntPtr handle)
    {
        if (!Imports.CloseHandle(handle))
            throw new Win32Exception($"[Error Code: {Marshal.GetLastWin32Error()}] Unable to close handle 0x{handle.ToString("X")}");

        return true;
    }

    /// <summary>
    /// Reads memory from the target process into the supplied buffer.
    /// </summary>
    /// <param name="processHandle">The process handle.</param>
    /// <param name="address">The address to read from.</param>
    /// <param name="buffer">The buffer to fill.</param>
    /// <param name="size">The number of bytes to read.</param>
    /// <returns>The number of bytes actually read.</returns>
    public static int ReadProcessMemory(SafeMemoryHandle processHandle, IntPtr address, [Out] byte[] buffer, int size)
    {
        if (!Imports.ReadProcessMemory(processHandle, address, buffer, size, out var bytesRead))
        {
            if (LogError)
            {
                var finalError = new StringBuilder();

                finalError.AppendLine(
                    $"[Error Code: {Marshal.GetLastWin32Error()}] Unable to read memory from 0x{address.ToString($"X{IntPtr.Size}")}[Size: {size}]");

                var frames = new StackTrace(true).GetFrames();

                if (frames != null)
                {
                    for (var i = 1; i < Math.Min(frames.Length, 10); i++)
                    {
                        var stackFrame = frames[i];
                        finalError.Append(stackFrame);
                    }
                }

                Core.Logger.Error(finalError.ToString());
            }
        }

        return bytesRead;
    }

    /// <summary>
    /// Writes the supplied buffer to memory in the target process.
    /// </summary>
    /// <param name="processHandle">The process handle.</param>
    /// <param name="address">The address to write to.</param>
    /// <param name="buffer">The bytes to write.</param>
    /// <param name="size">The number of bytes to write.</param>
    /// <returns>The number of bytes actually written.</returns>
    public static int WriteProcessMemory(SafeMemoryHandle processHandle, IntPtr address, [Out] byte[] buffer, int size)
    {
        var bytesWritten = 0;

        if (!Imports.WriteProcessMemory(processHandle, address, buffer, size, out bytesWritten))
        {
            if (LogError)
            {
                var finalError = new StringBuilder();

                finalError.AppendLine(
                    $"[Error Code: {Marshal.GetLastWin32Error()}] Unable to write memory at 0x{address.ToString($"X{IntPtr.Size}")}[Size: {size}]");

                var frames = new StackTrace(true).GetFrames();

                for (var i = 1; i < Math.Min(frames.Length, 10); i++)
                {
                    var stackFrame = frames[i];

                    finalError.AppendLine(
                        $"{stackFrame.GetFileName()} -> {stackFrame.GetMethod().Name}, line: {stackFrame.GetFileLineNumber()}");
                }

                Core.Logger.Error(finalError.ToString());
            }
        }

        return bytesWritten;
    }

    /// <summary>
    /// Commits a region of memory in the current process.
    /// </summary>
    /// <param name="address">The desired base address, or zero to let the system choose.</param>
    /// <param name="size">The size of the region, in bytes.</param>
    /// <param name="protect">The memory protection to apply.</param>
    /// <returns>The base address of the allocated region.</returns>
    /// <exception cref="Win32Exception">Thrown when the allocation fails.</exception>
    public static IntPtr Allocate([Optional] IntPtr address, int size,
        MemoryProtectionType protect = MemoryProtectionType.PAGE_EXECUTE_READWRITE)
    {
        var ret = Imports.VirtualAlloc(address, size, MemoryAllocationState.MEM_COMMIT, protect);

        if (ret.Equals(0))
        {
            throw new Win32Exception(string.Format("[Error Code: {0}] Unable to allocate memory at 0x{1}[Size: {2}]",
                Marshal.GetLastWin32Error(), address.ToString($"X{IntPtr.Size}"), size));
        }

        return ret;
    }

    /// <summary>
    /// Commits a region of memory in the target process.
    /// </summary>
    /// <param name="processHandle">The process handle.</param>
    /// <param name="address">The desired base address, or zero to let the system choose.</param>
    /// <param name="size">The size of the region, in bytes.</param>
    /// <param name="protect">The memory protection to apply.</param>
    /// <returns>The base address of the allocated region.</returns>
    /// <exception cref="Win32Exception">Thrown when the allocation fails.</exception>
    public static IntPtr Allocate(SafeMemoryHandle processHandle, [Optional] IntPtr address, int size,
        MemoryProtectionType protect = MemoryProtectionType.PAGE_EXECUTE_READWRITE)
    {
        var ret = Imports.VirtualAllocEx(processHandle, address, size, MemoryAllocationState.MEM_COMMIT, protect);

        if (ret.Equals(0))
        {
            throw new Win32Exception(string.Format(
                "[Error Code: {0}] Unable to allocate memory to process handle 0x{1} at 0x{2}[Size: {3}]",
                Marshal.GetLastWin32Error(), processHandle.DangerousGetHandle().ToString("X"),
                address.ToString($"X{IntPtr.Size}"), size));
        }

        return ret;
    }

    public static bool Free(IntPtr address, int size = 0, MemoryFreeType free = MemoryFreeType.MEM_RELEASE)
    {
        if (!Imports.VirtualFree(address, size, free))
        {
            throw new Win32Exception(string.Format("[Error Code: {0}] Unable to free memory at 0x{1}[Size: {2}]",
                Marshal.GetLastWin32Error(), address.ToString($"X{IntPtr.Size}"), size));
        }

        return true;
    }

    public static bool Free(SafeMemoryHandle processHandle, IntPtr address, int size = 0,
        MemoryFreeType free = MemoryFreeType.MEM_RELEASE)
    {
        if (!Imports.VirtualFreeEx(processHandle, address, size, free))
        {
            throw new Win32Exception(string.Format(
                "[Error Code: {0}] Unable to free memory from process handle 0x{1} at 0x{2}[Size: {3}]",
                Marshal.GetLastWin32Error(), processHandle.DangerousGetHandle().ToString("X"),
                address.ToString($"X{IntPtr.Size}"), size));
        }

        return true;
    }

    public static void Copy(void* destination, void* source, int size)
    {
        try
        {
            Imports.MoveMemory(destination, source, size);
        }
        catch
        {
            throw new Win32Exception(string.Format("[Error Code: {0}] Unable to copy memory to {0} from {1}[Size: {2}]",
                Marshal.GetLastWin32Error(), (*(ulong*) destination).ToString($"X{IntPtr.Size}"),
                (*(ulong*) source).ToString($"X{IntPtr.Size} ({size})")));
        }
    }

    public static MemoryProtectionType ChangeMemoryProtection(IntPtr address, int size,
        MemoryProtectionType newProtect =
            MemoryProtectionType.PAGE_EXECUTE_READWRITE)
    {
        MemoryProtectionType oldProtect;

        if (!Imports.VirtualProtect(address, size, newProtect, out oldProtect))
        {
            throw new Win32Exception(
                $"[Error Code: {Marshal.GetLastWin32Error()}] Unable to change memory protection at 0x{address.ToString($"X{IntPtr.Size}")}[Size: {size}] to {newProtect.ToString("X")}");
        }

        return oldProtect;
    }

    public static MemoryProtectionType ChangeMemoryProtection(SafeMemoryHandle processHandle, IntPtr address, int size,
        MemoryProtectionType newProtect =
            MemoryProtectionType.PAGE_EXECUTE_READWRITE)
    {
        MemoryProtectionType oldProtect;

        if (!Imports.VirtualProtectEx(processHandle, address, size, newProtect, out oldProtect))
        {
            throw new Win32Exception(
                $"[Error Code: {Marshal.GetLastWin32Error()}] Unable to change memory protection of process handle 0x{processHandle.DangerousGetHandle().ToString("X")} at 0x{address.ToString($"X{IntPtr.Size}")}[Size: {size}] to {newProtect.ToString("X")}");
        }

        return oldProtect;
    }

    public static MemoryBasicInformation Query(IntPtr address, int size)
    {
        if (Imports.VirtualQuery(address, out var memInfo, size) == 0)
        {
            throw new Win32Exception(
                $"[Error Code: {Marshal.GetLastWin32Error()}] Unable to retrieve memory information from 0x{address.ToString($"X{IntPtr.Size}")}[Size: {size}]");
        }

        return memInfo;
    }

    public static MemoryBasicInformation Query(SafeMemoryHandle processHandle, IntPtr address, int size)
    {
        if (Imports.VirtualQueryEx(processHandle, address, out var memInfo, size) == 0)
        {
            throw new Win32Exception(
                $"[Error Code: {Marshal.GetLastWin32Error()}] Unable to retrieve memory information of process handle 0x{processHandle.DangerousGetHandle().ToString("X")} from 0x{address.ToString($"X{IntPtr.Size}")}[Size: {size}]");
        }

        return memInfo;
    }
}
