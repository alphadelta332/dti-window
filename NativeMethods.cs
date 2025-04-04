using System.Diagnostics;
using System.Runtime.InteropServices;
using System.Security;

namespace DtiWindow;

/// <summary>
/// Provides native methods for interacting with the Windows API, including keyboard hooks.
/// </summary>
/// <remarks>
/// This class is optimized for performance and security by using <see cref="SuppressUnmanagedCodeSecurityAttribute"/>
/// to reduce interop overhead.
/// </remarks>
[SuppressUnmanagedCodeSecurity]
internal static class NativeMethods
{
    /// <summary>
    /// Low-level keyboard hook identifier.
    /// </summary>
    public const int WH_KEYBOARD_LL = 13;

    /// <summary>
    /// Windows message identifier for a key-down event.
    /// </summary>
    public const int WM_KEYDOWN = 0x0100;

    /// <summary>
    /// Windows message identifier for a key-up event.
    /// </summary>
    public const int WM_KEYUP = 0x0101;

    /// <summary>
    /// Delegate for processing low-level keyboard hook events.
    /// </summary>
    /// <param name="nCode">Hook code, used to determine whether the event should be processed.</param>
    /// <param name="wParam">The identifier of the keyboard message.</param>
    /// <param name="lParam">A pointer to a <c>KBDLLHOOKSTRUCT</c> containing event data.</param>
    /// <returns>A pointer to the next hook procedure in the chain.</returns>
    public delegate IntPtr LowLevelKeyboardProc(int nCode, IntPtr wParam, IntPtr lParam);

    /// <summary>
    /// Sets a Windows hook to monitor low-level keyboard input events.
    /// </summary>
    /// <param name="idHook">The type of hook procedure to install (e.g., <see cref="WH_KEYBOARD_LL"/>).</param>
    /// <param name="lpfn">The hook callback function.</param>
    /// <param name="hMod">Handle to the DLL containing the hook procedure, or <see cref="IntPtr.Zero"/> if it's in the current process.</param>
    /// <param name="dwThreadId">The thread ID to associate with the hook, or <c>0</c> for all threads.</param>
    /// <returns>A handle to the installed hook, or <see cref="IntPtr.Zero"/> on failure.</returns>
    [DllImport("user32.dll", CharSet = CharSet.Auto, SetLastError = true, ExactSpelling = false)]
    public static extern IntPtr SetWindowsHookEx(int idHook, LowLevelKeyboardProc lpfn, IntPtr hMod, uint dwThreadId);

    /// <summary>
    /// Removes an installed Windows hook.
    /// </summary>
    /// <param name="hhk">Handle to the hook to be removed.</param>
    /// <returns><c>true</c> if the hook was successfully removed; otherwise, <c>false</c>.</returns>
    [DllImport("user32.dll", CharSet = CharSet.Auto, SetLastError = true, ExactSpelling = false)]
    [return: MarshalAs(UnmanagedType.Bool)]
    public static extern bool UnhookWindowsHookEx(IntPtr hhk);

    /// <summary>
    /// Passes the hook information to the next hook procedure in the chain.
    /// </summary>
    /// <param name="hhk">Handle to the current hook.</param>
    /// <param name="nCode">The hook code.</param>
    /// <param name="wParam">The identifier of the keyboard message.</param>
    /// <param name="lParam">A pointer to a <c>KBDLLHOOKSTRUCT</c> containing event data.</param>
    /// <returns>A pointer to the next hook procedure in the chain.</returns>
    [DllImport("user32.dll", CharSet = CharSet.Auto, SetLastError = true, ExactSpelling = false)]
    public static extern IntPtr CallNextHookEx(IntPtr hhk, int nCode, IntPtr wParam, IntPtr lParam);

    /// <summary>
    /// Retrieves a module handle for the specified module name.
    /// </summary>
    /// <param name="lpModuleName">The name of the module, or <c>null</c> for the calling process.</param>
    /// <returns>A handle to the specified module, or <see cref="IntPtr.Zero"/> on failure.</returns>
    [DllImport("kernel32.dll", CharSet = CharSet.Unicode, SetLastError = true, ExactSpelling = false)]
    public static extern IntPtr GetModuleHandle(string lpModuleName);

    /// <summary>
    /// Installs a global low-level keyboard hook.
    /// </summary>
    /// <param name="proc">The callback function to handle keyboard events.</param>
    /// <returns>A handle to the installed hook, or <see cref="IntPtr.Zero"/> if the hook failed to install.</returns>
    /// <remarks>
    /// The hook will remain active until explicitly removed using <see cref="UnhookWindowsHookEx"/>.
    /// </remarks>
    public static IntPtr SetHook(LowLevelKeyboardProc proc)
    {
        var moduleName = Process.GetCurrentProcess().MainModule?.ModuleName ?? "kernel32.dll";
        return SetWindowsHookEx(WH_KEYBOARD_LL, proc, GetModuleHandle(moduleName), 0);
    }
}
