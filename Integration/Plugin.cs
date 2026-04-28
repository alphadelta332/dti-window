using System.ComponentModel.Composition;
using System.Diagnostics;
using System.Runtime.InteropServices;
using System.Net.Http;
using DTIWindow.Events;
using DTIWindow.UI;
using vatsys;
using vatsys.Plugin;
using System.Reflection;
using System.Text.Json;

namespace DTIWindow.Integration
{
    [Export(typeof(IPlugin))]
    public class DTIWindow : IPlugin
    {
        public string Name => "Traffic Info";
        private readonly CustomToolStripMenuItem _opener;
        private delegate IntPtr LowLevelKeyboardProc(int nCode, IntPtr wParam, IntPtr lParam);
        private static LowLevelKeyboardProc _proc = KeyEvents.HookCallback;

        [DllImport("user32.dll", CharSet = CharSet.Auto, SetLastError = true)]
        private static extern IntPtr SetWindowsHookEx(int idHook, LowLevelKeyboardProc lpfn, IntPtr hMod, uint dwThreadId);

        [DllImport("user32.dll", CharSet = CharSet.Auto, SetLastError = true)]
        [return: MarshalAs(UnmanagedType.Bool)]
        private static extern bool UnhookWindowsHookEx(IntPtr hhk);

        [DllImport("user32.dll", CharSet = CharSet.Auto, SetLastError = true)]
        public static extern IntPtr CallNextHookEx(IntPtr hhk, int nCode, IntPtr wParam, IntPtr lParam);

        [DllImport("kernel32.dll", CharSet = CharSet.Auto, SetLastError = true)]
        private static extern IntPtr GetModuleHandle(string? lpModuleName);

        public DTIWindow()
        {
            CrashLogger.Attach();
            Task.Run(async () => await CheckForUpdatesAsync());

            _opener = new(CustomToolStripMenuItemWindowType.Main, CustomToolStripMenuItemCategory.Windows, new ToolStripMenuItem("Traffic Info"));
            var Events = new VatsysEvents();
            _opener.Item.Click += Events.OpenForm;

            MMI.AddCustomMenuItem(_opener);
            MMI.SelectedTrackChanged += Events.TrackSelected;
            Events.InitialiseTracksChanged();

            MMI.InvokeOnGUI(() =>
            {
                var mainForm = Application.OpenForms["Mainform"];
                if (mainForm != null)
                {
                    var keyEvents = new KeyEvents();
                    mainForm.KeyUp += keyEvents.KeyUp;
                    mainForm.KeyDown += keyEvents.KeyDown;
                    mainForm.LostFocus += (s, e) => KeyEvents.KeybindPressed = false;
                    KeyboardSettingsInjector.HookKeyboardSettingsMenu(mainForm);
                }
            });

            StartGlobalHook();
        }

        void IPlugin.OnFDRUpdate(FDP2.FDR updated) { }

        void IPlugin.OnRadarTrackUpdate(RDP.RadarTrack updated) { }

        public void StartGlobalHook()
        {
            if (KeyEvents._hookID == IntPtr.Zero)
                KeyEvents._hookID = SetHook(_proc);
        }

        public void StopGlobalHook()
        {
            if (KeyEvents._hookID != IntPtr.Zero)
            {
                UnhookWindowsHookEx(KeyEvents._hookID);
                KeyEvents._hookID = IntPtr.Zero;
            }
        }

        private static IntPtr SetHook(LowLevelKeyboardProc proc)
        {
            using Process curProcess = Process.GetCurrentProcess();
            var moduleName = curProcess.MainModule?.ModuleName;
            return SetWindowsHookEx(KeyEvents.WH_KEYBOARD_LL, proc, GetModuleHandle(moduleName), 0);
        }

        public async Task CheckForUpdatesAsync()
        {
            try
            {
                string versionUrl = "https://raw.githubusercontent.com/alphadelta332/dti-window/main/Version.json";

                using HttpClient client = new HttpClient();
                string json = await client.GetStringAsync(versionUrl);
                var latestVersionData = JsonSerializer.Deserialize<VersionData>(json);

                if (latestVersionData == null)
                    return;

                Version latestVersion = new Version(latestVersionData.Major, latestVersionData.Minor, latestVersionData.Build);
                Version currentVersion = Assembly.GetExecutingAssembly().GetName().Version;

                if (currentVersion < latestVersion)
                    ErrorReporter.ThrowError("Traffic Info Plugin", $"Your plugin version ({currentVersion}) is outdated. Please update to the latest version ({latestVersion}) via GitHub or vatSys Launcher.");
            }
            catch (Exception) { }
        }
    }

    public class VersionData
    {
        public int Major { get; set; }
        public int Minor { get; set; }
        public int Build { get; set; }
    }
}
