using System.Runtime.InteropServices;
using System.Text.Json;

namespace DTIWindow.Events
{
    public class KeyEvents
    {
        public const int WH_KEYBOARD_LL = 13;
        public const int WM_KEYDOWN = 0x0100;
        public const int WM_KEYUP = 0x0101;
        public static IntPtr _hookID = IntPtr.Zero;
        public CancellationTokenSource? keybindTimeout;
        public static volatile bool KeybindPressed;

        public static IntPtr HookCallback(int nCode, IntPtr wParam, IntPtr lParam)
        {
            if (nCode >= 0)
            {
                int vkCode = Marshal.ReadInt32(lParam);

                if (wParam == (IntPtr)WM_KEYDOWN && vkCode == (int)KeyEventsHelper.GetKeybind())
                    KeybindPressed = true;
                else if (wParam == (IntPtr)WM_KEYUP && vkCode == (int)KeyEventsHelper.GetKeybind())
                    KeybindPressed = false;
            }

            return Integration.DTIWindow.CallNextHookEx(_hookID, nCode, wParam, lParam);
        }

        public void KeyUp(object sender, KeyEventArgs e)
        {
            if (e.KeyCode == KeyEventsHelper.GetKeybind())
            {
                KeybindPressed = false;
                keybindTimeout?.Cancel();
            }
        }

        public void KeyDown(object sender, KeyEventArgs e)
        {
            if (e.KeyCode == KeyEventsHelper.GetKeybind())
            {
                KeybindPressed = true;
                keybindTimeout?.Cancel();
                keybindTimeout = new CancellationTokenSource();
                var token = keybindTimeout.Token;
                Task.Delay(5000, token).ContinueWith(t =>
                {
                    if (!t.IsCanceled)
                        KeybindPressed = false;
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

        // Reads physical key state directly — reliable even when the WH_KEYBOARD_LL hook
        // message hasn't been processed yet (async delivery means it can lag behind mouse events).
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
