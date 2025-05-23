using System.ComponentModel.Composition;
using System.Diagnostics;
using System.Runtime.InteropServices;
using System.Net.Http;
using DTIWindow.Events;
using vatsys;
using vatsys.Plugin;
using System.Reflection;
using System.Text.Json;
using System.Data.Common;

namespace DTIWindow.Integration
{
    [Export(typeof(IPlugin))]
    public class DTIWindow : BaseForm, IPlugin
    {
        private readonly CustomToolStripMenuItem _opener; // Menu button for opening the DTI Window
        private delegate IntPtr LowLevelKeyboardProc(int nCode, IntPtr wParam, IntPtr lParam);
        private static LowLevelKeyboardProc _proc = KeyEvents.HookCallback; // Delegate for the keyboard hook callback

        [DllImport("user32.dll", CharSet = CharSet.Auto, SetLastError = true)]
        private static extern IntPtr SetWindowsHookEx(int idHook, LowLevelKeyboardProc lpfn, IntPtr hMod, uint dwThreadId);

        [DllImport("user32.dll", CharSet = CharSet.Auto, SetLastError = true)]
        [return: MarshalAs(UnmanagedType.Bool)]
        private static extern bool UnhookWindowsHookEx(IntPtr hhk);

        [DllImport("user32.dll", CharSet = CharSet.Auto, SetLastError = true)]
        public static extern IntPtr CallNextHookEx(IntPtr hhk, int nCode, IntPtr wParam, IntPtr lParam);

        [DllImport("kernel32.dll", CharSet = CharSet.Auto, SetLastError = true)]
        private static extern IntPtr GetModuleHandle(string lpModuleName);

        // Constructor for the DTIWindow plugin
        public DTIWindow()
        {
            // Perform the version check
            Task.Run(async () => await CheckForUpdatesAsync());

            // Initialise the menu bar button for the plugin
            _opener = new(CustomToolStripMenuItemWindowType.Main, CustomToolStripMenuItemCategory.Windows, new ToolStripMenuItem("Traffic Info"));
            var Events = new VatsysEvents();
            _opener.Item.Click += Events.OpenForm; // Attach event handler to open the form

            MMI.AddCustomMenuItem(_opener); // Add the menu item to the vatSys menu

            MMI.SelectedTrackChanged += Events.TrackSelected; // Attach event handler for track selection changes

            // Create keybind listeners when the main ASD is created
            MMI.InvokeOnGUI(() =>
            {
                var mainForm = Application.OpenForms["Mainform"];
                if (mainForm != null)
                {
                    var keyEvents = new KeyEvents();
                    mainForm.KeyUp += keyEvents.KeyUp;
                    mainForm.KeyDown += keyEvents.KeyDown;
                    mainForm.LostFocus += (s, e) =>
                    {
                        KeyEvents.KeybindPressed = false;
                    };
                }
            });

            StartGlobalHook(); // Start the global keyboard hook
        }

        // Required for plugin to function (no implementation needed here)
        void IPlugin.OnFDRUpdate(FDP2.FDR updated)
        {
            return;
        }

        void IPlugin.OnRadarTrackUpdate(RDP.RadarTrack updated)
        {
            return;
        }

        public void StartGlobalHook()
        {
            if (KeyEvents._hookID == IntPtr.Zero)
            {
                KeyEvents._hookID = SetHook(_proc);
            }
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
            using (Process curProcess = Process.GetCurrentProcess())
            using (ProcessModule curModule = curProcess.MainModule)
            {
                return SetWindowsHookEx(KeyEvents.WH_KEYBOARD_LL, proc, GetModuleHandle(curModule.ModuleName), 0);
            }
        }

        public async Task CheckForUpdatesAsync()
        {
            try
            {
                // URL to fetch the latest version (e.g., a JSON file hosted on GitHub Pages)
                string versionUrl = "https://raw.githubusercontent.com/alphadelta332/dti-window/main/Version.json";

                using (HttpClient client = new HttpClient())
                {
                    // Fetch the JSON content from the URL
                    string json = await client.GetStringAsync(versionUrl);

                    // Deserialize the JSON into a VersionData object
                    var latestVersionData = JsonSerializer.Deserialize<VersionData>(json);

                    if (latestVersionData == null)
                    {
                        return;
                    }

                    // Construct the latest version from the JSON data
                    Version latestVersion = new Version(latestVersionData.Major, latestVersionData.Minor, latestVersionData.Build);

                    // Get the current version from AssemblyInfo
                    Version currentVersion = Assembly.GetExecutingAssembly().GetName().Version;

                    // Compare versions
                    if (currentVersion < latestVersion)
                    {
                        // Throw an error if the current version is outdated
                        var events = new VatsysEvents();
                        events.ThrowError("Traffic Info Plugin", $"Your plugin version ({currentVersion}) is outdated. Please update to the latest version ({latestVersion}) via GitHub or Plugin Manager.");
                    }
                }
            }
            catch (DbException)
            {
            }
        }
    }

    // Class to represent the JSON structure
    public class VersionData
    {
        public int Major { get; set; }
        public int Minor { get; set; }
        public int Build { get; set; }
    }
}