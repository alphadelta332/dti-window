using System.Runtime.InteropServices;
using DTIWindow.MMI;

namespace DTIWindow.Events
{
    public class KeyEvents : Form
    {
        public const int WH_KEYBOARD_LL = 13; // Low-level keyboard hook constant
        public const int WM_KEYDOWN = 0x0100; // Windows message for key down
        public const int WM_KEYUP = 0x0101; // Windows message for key up
        public static IntPtr _hookID = IntPtr.Zero; // Handle for the global keyboard hook
        public CancellationTokenSource? keybindTimeout;
        public static bool KeybindPressed; // Tracks if the F7 key is currently pressed
        public static IntPtr HookCallback(int nCode, IntPtr wParam, IntPtr lParam)
        {
            if (nCode >= 0)
            {
                int vkCode = Marshal.ReadInt32(lParam); // Get the virtual key code

                if (wParam == (IntPtr)WM_KEYDOWN && vkCode == (int)Keys.F7)
                {
                    KeybindPressed = true; // Set KeybindPressed to true
                }
                else if (wParam == (IntPtr)WM_KEYUP && vkCode == (int)Keys.F7)
                {
                    KeybindPressed = false; // Set KeybindPressed to false
                }
            }

            return DTIWindowPluginClass.CallNextHookEx(_hookID, nCode, wParam, lParam); // Pass the event to the next hook in the chain
        }

        // Event handler for when a key is released
        public new void KeyUp(object sender, KeyEventArgs e)
        {
            if (e.KeyCode == Keys.F7)
            {
                KeybindPressed = false; // Set KeybindPressed to false when F7 is released

                // Cancel the timeout
                keybindTimeout?.Cancel();
            }
        }

        // Event handler for when a key is pressed
        public new void KeyDown(object sender, KeyEventArgs e)
        {
            if (e.KeyCode == Keys.F7)
            {
                KeybindPressed = true; // Set KeybindPressed to true when F7 is pressed

                // Cancel any existing timeout
                keybindTimeout?.Cancel();

                // Start a new timeout
                keybindTimeout = new CancellationTokenSource();
                var token = keybindTimeout.Token;
                Task.Delay(5000, token).ContinueWith(t =>
                {
                    if (!t.IsCanceled)
                    {
                        KeybindPressed = false;
                    }
                });
            }
        }
        public static void ResetKeybindPressed()
        {
            KeybindPressed = false;
        }
    }
}