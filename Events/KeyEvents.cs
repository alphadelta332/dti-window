using System.Runtime.InteropServices;
using System.Text.Json;
using vatsys;

namespace DTIWindow.Events
{
    public partial class KeyEvents : BaseForm
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

                if (wParam == (IntPtr)WM_KEYDOWN && vkCode == (int)KeyEventsHelper.GetKeybind())
                {
                    KeybindPressed = true; // Set KeybindPressed to true
                }
                else if (wParam == (IntPtr)WM_KEYUP && vkCode == (int)KeyEventsHelper.GetKeybind())
                {
                    KeybindPressed = false; // Set KeybindPressed to false
                }
            }

            return Integration.DTIWindow.CallNextHookEx(_hookID, nCode, wParam, lParam); // Pass the event to the next hook in the chain
        }

        // Event handler for when a key is released
        public new void KeyUp(object sender, KeyEventArgs e)
        {
            if (e.KeyCode == KeyEventsHelper.GetKeybind())
            {
                KeybindPressed = false; // Set KeybindPressed to false when F7 is released

                // Cancel the timeout
                keybindTimeout?.Cancel();
            }
        }

        // Event handler for when a key is pressed
        public new void KeyDown(object sender, KeyEventArgs e)
        {
            if (e.KeyCode == KeyEventsHelper.GetKeybind())
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

    public static class KeyEventsHelper
    {
        [DllImport("user32.dll")]
        private static extern short GetAsyncKeyState(int vKey);

        // Reads the physical key state directly from hardware — reliable even when
        // the WH_KEYBOARD_LL hook message hasn't been processed yet (e.g. within WndProc).
        public static bool IsKeybindPhysicallyHeld() =>
            (GetAsyncKeyState((int)GetKeybind()) & 0x8000) != 0;

        private static readonly string _settingsPath = Path.Combine(
            Environment.GetFolderPath(Environment.SpecialFolder.ApplicationData),
            "vatsys-dti-window",
            "settings.json"
        );

        private static Keys currentKeybind = LoadSavedKeybind();

        public static void SetKeybind(Keys key)
        {
            currentKeybind = key;
            SaveKeybind(key);
        }

        public static Keys GetKeybind() => currentKeybind;

        private static Keys LoadSavedKeybind()
        {
            try
            {
                if (File.Exists(_settingsPath))
                {
                    var data = JsonSerializer.Deserialize<PluginSettings>(File.ReadAllText(_settingsPath));
                    if (data?.Keybind != null && Enum.TryParse<Keys>(data.Keybind, out var key))
                        return key;
                }
            }
            catch { }
            return Keys.F7;
        }

        private static void SaveKeybind(Keys key)
        {
            try
            {
                Directory.CreateDirectory(Path.GetDirectoryName(_settingsPath));
                File.WriteAllText(_settingsPath, JsonSerializer.Serialize(new PluginSettings { Keybind = key.ToString() }));
            }
            catch { }
        }

        private class PluginSettings
        {
            public string? Keybind { get; set; }
        }
    }
}