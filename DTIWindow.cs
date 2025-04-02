using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Linq;
using System.Threading;
using System.Windows.Forms;
using System.Runtime.InteropServices;
using System.Runtime.Versioning;
using vatsys.Plugin;
using vatsys;
using System.ComponentModel.Composition;
using System.Windows.Input;
using System.Windows.Interop;
using System.Threading.Tasks;
using System.Diagnostics;

// Represents a child aircraft in the system
public class ChildAircraft
{
    public string Name { get; set; } // Name of the child aircraft
    public string Callsign { get; set; } // Callsign of the child aircraft
    public string Status { get; set; } // Status of the child aircraft (e.g., "Passed", "Unpassed")

    public ChildAircraft(string name, string callsign, string status)
    {
        // Ensure no null values are passed
        Name = name ?? throw new ArgumentNullException(nameof(name), "Child aircraft name cannot be null.");
        Callsign = callsign ?? throw new ArgumentNullException(nameof(callsign), "Child aircraft callsign cannot be null.");
        Status = status ?? throw new ArgumentNullException(nameof(status), "Child aircraft status cannot be null.");
    }
}

// Represents a parent aircraft in the system
public class Aircraft
{
    public string Name { get; set; } // Name of the aircraft
    public string Callsign { get; set; } // Callsign of the aircraft
    public BindingList<ChildAircraft> Children { get; set; } // List of child aircraft associated with this aircraft

    public Aircraft(string name, string callsign)
    {
        // Ensure no null values are passed
        Name = name ?? throw new ArgumentNullException(nameof(name), "Aircraft name cannot be null.");
        Callsign = callsign ?? throw new ArgumentNullException(nameof(callsign), "Aircraft callsign cannot be null.");
        Children = new BindingList<ChildAircraft>(); // Initialize the list of children
    }

    // Adds a child aircraft to the list if it doesn't already exist
    public void AddChild(ChildAircraft child)
    {
        if (!Children.Any(c => c.Callsign == child.Callsign))
        {
            Children.Add(child);
        }
    }

    // Checks if the aircraft has any child references
    public bool HasReferences()
    {
        return Children.Count > 0;
    }
}

// Main plugin class for the DTI Window
[Export(typeof(IPlugin))]
public class DTIWindow : Form, IPlugin
{
    private static bool KeybindPressed; // Tracks if the F7 key is currently pressed

    private Track? PreviousSelectedTrack; // Stores the previously selected radar track

    private AircraftViewer? Window; // Reference to the DTI Window form
    
    private readonly CustomToolStripMenuItem _opener; // Menu button for opening the DTI Window

    private BindingList<Aircraft> AircraftList = new(); // List of all parent aircraft
    private Dictionary<Aircraft, List<Aircraft>> AircraftPairings = new(); // Dictionary of traffic pairings between aircraft

    private CancellationTokenSource? keybindTimeout;

    private const int WH_KEYBOARD_LL = 13; // Low-level keyboard hook constant
    private const int WM_KEYDOWN = 0x0100; // Windows message for key down
    private const int WM_KEYUP = 0x0101; // Windows message for key up

    private static IntPtr _hookID = IntPtr.Zero; // Handle for the global keyboard hook
    private static LowLevelKeyboardProc _proc = HookCallback; // Delegate for the keyboard hook callback

    // Hook callback for global keyboard events
    private static IntPtr HookCallback(int nCode, IntPtr wParam, IntPtr lParam)
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

        return CallNextHookEx(_hookID, nCode, wParam, lParam); // Pass the event to the next hook in the chain
    }

    private delegate IntPtr LowLevelKeyboardProc(int nCode, IntPtr wParam, IntPtr lParam);

    [DllImport("user32.dll", CharSet = CharSet.Auto, SetLastError = true)]
    private static extern IntPtr SetWindowsHookEx(int idHook, LowLevelKeyboardProc lpfn, IntPtr hMod, uint dwThreadId);

    [DllImport("user32.dll", CharSet = CharSet.Auto, SetLastError = true)]
    [return: MarshalAs(UnmanagedType.Bool)]
    private static extern bool UnhookWindowsHookEx(IntPtr hhk);

    [DllImport("user32.dll", CharSet = CharSet.Auto, SetLastError = true)]
    private static extern IntPtr CallNextHookEx(IntPtr hhk, int nCode, IntPtr wParam, IntPtr lParam);

    [DllImport("kernel32.dll", CharSet = CharSet.Auto, SetLastError = true)]
    private static extern IntPtr GetModuleHandle(string lpModuleName);

    public new string Name => "DTI Window"; // Plugin name

    // Constructor for the DTIWindow plugin
    public DTIWindow()
    {
        // Initialize the menu bar button for the plugin
        _opener = new(CustomToolStripMenuItemWindowType.Main, CustomToolStripMenuItemCategory.Windows, new ToolStripMenuItem("Traffic Info"));
        _opener.Item.Click += OpenForm; // Attach event handler to open the form

        MMI.AddCustomMenuItem(_opener); // Add the menu item to the vatSys menu

        MMI.SelectedTrackChanged += TrackSelected; // Attach event handler for track selection changes

        // Create keybind listeners when the main ASD is created
        MMI.InvokeOnGUI(() =>
        {
            var mainForm = Application.OpenForms["Mainform"];
            if (mainForm != null)
            {
                mainForm.KeyUp += KeyUp;
                mainForm.KeyDown += KeyDown;
                mainForm.LostFocus += (s, e) =>
                {
                    KeybindPressed = false;
                };
            }
        });

        StartGlobalHook(); // Start the global keyboard hook
    }

    // Event handler for when a key is released
    private new void KeyUp(object sender, KeyEventArgs e)
    {
        if (e.KeyCode == Keys.F7)
        {
            KeybindPressed = false; // Set KeybindPressed to false when F7 is released

            // Cancel the timeout
            keybindTimeout?.Cancel();
        }
    }

    // Event handler for when a key is pressed
    private new void KeyDown(object sender, KeyEventArgs e)
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

    // Event handler for when a radar track is selected
    private void TrackSelected(object sender, EventArgs e)
    {
        try
        {
            var track = MMI.SelectedTrack; // Get the currently selected track

            if (PreviousSelectedTrack != null && track != PreviousSelectedTrack && track != null && KeybindPressed)
            {
                MMI.SelectedTrack = PreviousSelectedTrack; // Re-select the previous track

                // Ensure the AircraftViewer form is created and visible
                OpenForm();

                if (Window == null)
                {
                    return;
                }

                // Get or create the parent and child aircraft
                var parentAircraft = Window.GetOrCreateAircraft(PreviousSelectedTrack.GetPilot().Callsign);
                var childAircraft = Window.GetOrCreateAircraft(track.GetPilot().Callsign);

                // Create a traffic pairing between the parent and child aircraft
                Window.CreateTrafficPairing(parentAircraft, childAircraft);

                ResetKeybindPressed(); // Reset KeybindPressed after creating a traffic pairing
                return;
            }

            // Check if the selected track corresponds to a parent aircraft
            if (track != null)
            {
                var parentAircraft = AircraftList.FirstOrDefault(a => a.Callsign == track.GetPilot().Callsign);
                if (parentAircraft != null)
                {
                    // Update the designated aircraft in the AircraftViewer
                    if (Window != null && !Window.IsDisposed)
                    {
                        Window.SetDesignatedAircraft(parentAircraft);
                    }
                }
                else
                {
                    // Clear the designated aircraft if no match is found
                    if (Window != null && !Window.IsDisposed)
                    {
                        Window.SetDesignatedAircraft(null);
                    }
                }
            }
            else
            {
                // Clear the designated aircraft if no track is selected
                if (Window != null && !Window.IsDisposed)
                {
                    Window.SetDesignatedAircraft(null);
                }
            }

            // Update the previously selected track
            PreviousSelectedTrack = track;
        }
        catch (Exception)
        {
            // Handle exceptions silently in release mode
        }
    }

    // Opens the DTI Window form
    private void OpenForm(object? sender = null, EventArgs? e = null)
    {
        try
        {
            if (Window == null || Window.IsDisposed)
            {
                // Create a new AircraftViewer window if it doesn't exist or has been closed
                Window = new AircraftViewer(AircraftList, AircraftPairings);
            }

            Window.Show(Form.ActiveForm); // Show the AircraftViewer window
        }
        catch (Exception)
        {
            // Handle exceptions silently in release mode
        }
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
        if (_hookID == IntPtr.Zero)
        {
            _hookID = SetHook(_proc);
        }
    }

    public void StopGlobalHook()
    {
        if (_hookID != IntPtr.Zero)
        {
            UnhookWindowsHookEx(_hookID);
            _hookID = IntPtr.Zero;
        }
    }

    private static IntPtr SetHook(LowLevelKeyboardProc proc)
    {
        using (Process curProcess = Process.GetCurrentProcess())
        using (ProcessModule curModule = curProcess.MainModule)
        {
            return SetWindowsHookEx(WH_KEYBOARD_LL, proc, GetModuleHandle(curModule.ModuleName), 0);
        }
    }

    public static void ResetKeybindPressed()
    {
        KeybindPressed = false;
    }
}
