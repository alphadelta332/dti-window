using System.ComponentModel.Composition;
using System.Diagnostics;
using System.Runtime.InteropServices;
using vatsys;
using vatsys.Plugin;

namespace DTIWindow.MMI
{
    [Export(typeof(IPlugin))]
    public class DTIWindowPluginClass : Form, IPlugin
    {
        private readonly CustomToolStripMenuItem _opener; // Menu button for opening the DTI Window
        private delegate IntPtr LowLevelKeyboardProc(int nCode, IntPtr wParam, IntPtr lParam);
        private static LowLevelKeyboardProc _proc = HookCallback; // Delegate for the keyboard hook callback
        private static IntPtr HookCallback(int nCode, IntPtr wParam, IntPtr lParam)
        {
            if (nCode >= 0)
            {
                // Process the keyboard input here if needed
            }
            return CallNextHookEx(IntPtr.Zero, nCode, wParam, lParam);
        }

        [DllImport("user32.dll", CharSet = CharSet.Auto, SetLastError = true)]
        private static extern IntPtr SetWindowsHookEx(int idHook, LowLevelKeyboardProc lpfn, IntPtr hMod, uint dwThreadId);

        [DllImport("user32.dll", CharSet = CharSet.Auto, SetLastError = true)]
        [return: MarshalAs(UnmanagedType.Bool)]
        private static extern bool UnhookWindowsHookEx(IntPtr hhk);

        [DllImport("user32.dll", CharSet = CharSet.Auto, SetLastError = true)]
        public static extern IntPtr CallNextHookEx(IntPtr hhk, int nCode, IntPtr wParam, IntPtr lParam);

        [DllImport("kernel32.dll", CharSet = CharSet.Auto, SetLastError = true)]
        private static extern IntPtr GetModuleHandle(string lpModuleName);

        public new string Name => "DTI Window"; // Plugin name

        // Constructor for the DTIWindow plugin
        public DTIWindowPluginClass()
        {
            // Initialize the menu bar button for the plugin
            _opener = new(CustomToolStripMenuItemWindowType.Main, CustomToolStripMenuItemCategory.Windows, new ToolStripMenuItem("Traffic Info"));
            var Events = new DTIWindow.MMI.Events();
            _opener.Item.Click += Events.OpenForm; // Attach event handler to open the form

            vatsys.MMI.AddCustomMenuItem(_opener); // Add the menu item to the vatSys menu

            vatsys.MMI.SelectedTrackChanged += Events.TrackSelected; // Attach event handler for track selection changes

            // Create keybind listeners when the main ASD is created
            vatsys.MMI.InvokeOnGUI(() =>
            {
                var mainForm = Application.OpenForms["Mainform"];
                if (mainForm != null)
                {
                    mainForm.KeyUp += Events.KeyUp;
                    mainForm.KeyDown += Events.KeyDown;
                    mainForm.LostFocus += (s, e) =>
                    {
                        Events.KeybindPressed = false;
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
            if (DTIWindow.MMI.Events._hookID == IntPtr.Zero)
            {
                DTIWindow.MMI.Events._hookID = SetHook(_proc);
            }
        }

        public void StopGlobalHook()
        {
            if (DTIWindow.MMI.Events._hookID != IntPtr.Zero)
            {
                UnhookWindowsHookEx(DTIWindow.MMI.Events._hookID);
                DTIWindow.MMI.Events._hookID = IntPtr.Zero;
            }
        }

        private static IntPtr SetHook(LowLevelKeyboardProc proc)
        {
            using (Process curProcess = Process.GetCurrentProcess())
            using (ProcessModule curModule = curProcess.MainModule)
            {
                return SetWindowsHookEx(DTIWindow.MMI.Events.WH_KEYBOARD_LL, proc, GetModuleHandle(curModule.ModuleName), 0);
            }
        }

        public static void ResetKeybindPressed()
        {
            DTIWindow.MMI.Events.KeybindPressed = false;
        }
    }
}