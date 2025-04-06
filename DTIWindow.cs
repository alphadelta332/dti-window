using System.ComponentModel;
using System.ComponentModel.Composition;
using System.Diagnostics;
using System.Runtime.InteropServices;

using vatsys;
using vatsys.Plugin;

namespace DtiWindow;

/// <summary>
/// Main plugin class for the DTI Window.
/// </summary>
[Export(typeof(IPlugin))]
public class DTIWindow : IPlugin, IDisposable
{
    private static readonly NativeMethods.LowLevelKeyboardProc _proc = HookCallback; // Delegate for the keyboard hook callback

    private static IntPtr _hookID = IntPtr.Zero; // Handle for the global keyboard hook

    private static bool KeybindPressed; // Tracks if the F7 key is currently pressed

    private readonly CustomToolStripMenuItem _opener; // Menu button for opening the DTI Window

    private readonly BindingList<Aircraft> _AircraftList = new(); // List of all parent aircraft
    private readonly Dictionary<Aircraft, List<Aircraft>> _AircraftPairings = new(); // Dictionary of traffic pairings between aircraft

    private Track? _PreviousSelectedTrack; // Stores the previously selected radar track

    private AircraftViewer? _Window; // Reference to the DTI Window form

    private CancellationTokenSource? _keybindTimeout;

    /// <summary>
    /// Initializes a new instance of the <see cref="DTIWindow"/> class.
    /// </summary>
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

    /// <inheritdoc/>
    public string Name => "DTI Window"; // Plugin name

    /// <inheritdoc/>
    public void Dispose()
    {
        Dispose(true);
        GC.SuppressFinalize(this);
    }

    // Required for plugin to function (no implementation needed here)
    /// <inheritdoc/>
    void IPlugin.OnFDRUpdate(FDP2.FDR updated)
    {
        return;
    }

    /// <inheritdoc/>
    void IPlugin.OnRadarTrackUpdate(RDP.RadarTrack updated)
    {
        return;
    }

    /// <summary>
    /// Disposes the resources used by the DTI Window plugin.
    /// </summary>
    /// <param name="isDisposing">If being called by the Dispose method.</param>
    protected virtual void Dispose(bool isDisposing)
    {
        if (isDisposing)
        {
            // Unhook the global keyboard hook
            StopGlobalHook();

            // Remove the menu item from the vatSys menu
            MMI.RemoveCustomMenuItem(_opener);

            // Detach event handlers
            MMI.SelectedTrackChanged -= TrackSelected;

            // Dispose of the DTI Window if it exists
            _Window?.Dispose();

            _keybindTimeout?.Dispose();
        }
    }

    // Hook callback for global keyboard events
    private static IntPtr HookCallback(int nCode, IntPtr wParam, IntPtr lParam)
    {
        if (nCode >= 0)
        {
            var vkCode = Marshal.ReadInt32(lParam); // Get the virtual key code

            if (wParam == (IntPtr)NativeMethods.WM_KEYDOWN && vkCode == (int)Keys.F7)
            {
                KeybindPressed = true; // Set KeybindPressed to true
            }
            else if (wParam == (IntPtr)NativeMethods.WM_KEYUP && vkCode == (int)Keys.F7)
            {
                KeybindPressed = false; // Set KeybindPressed to false
            }
        }

        return NativeMethods.CallNextHookEx(_hookID, nCode, wParam, lParam); // Pass the event to the next hook in the chain
    }

    private static IntPtr SetHook(NativeMethods.LowLevelKeyboardProc proc)
    {
        using (var curProcess = Process.GetCurrentProcess())
        using (var curModule = curProcess.MainModule)
        {
            return NativeMethods.SetWindowsHookEx(NativeMethods.WH_KEYBOARD_LL, proc, NativeMethods.GetModuleHandle(curModule.ModuleName), 0);
        }
    }

    private static void ResetKeybindPressed()
    {
        KeybindPressed = false;
    }

    private static void StartGlobalHook()
    {
        if (_hookID == IntPtr.Zero)
        {
            _hookID = SetHook(_proc);
        }
    }

    private static void StopGlobalHook()
    {
        if (_hookID != IntPtr.Zero)
        {
            NativeMethods.UnhookWindowsHookEx(_hookID);
            _hookID = IntPtr.Zero;
        }
    }

    // Event handler for when a key is released
    private void KeyUp(object sender, KeyEventArgs e)
    {
        if (e.KeyCode == Keys.F7)
        {
            KeybindPressed = false; // Set KeybindPressed to false when F7 is released

            // Cancel the timeout
            _keybindTimeout?.Cancel();
        }
    }

    // Event handler for when a key is pressed
    private async void KeyDown(object sender, KeyEventArgs e)
    {
        if (e.KeyCode == Keys.F7)
        {
            KeybindPressed = true; // Set KeybindPressed to true when F7 is pressed

            // Cancel any existing timeout
            _keybindTimeout?.Cancel();

            // Start a new timeout
            _keybindTimeout = new CancellationTokenSource();
            var token = _keybindTimeout.Token;
            try
            {
                await Task.Delay(5000, token);
            }
            catch (OperationCanceledException)
            {
                // Handle the cancellation
            }
            finally
            {
                KeybindPressed = false; // Reset KeybindPressed after the timeout
            }
        }
    }

    // Event handler for when a radar track is selected
    private void TrackSelected(object sender, EventArgs e)
    {
        try
        {
            var track = MMI.SelectedTrack; // Get the currently selected track

            if (_PreviousSelectedTrack != null && track != _PreviousSelectedTrack && track != null && KeybindPressed)
            {
                MMI.SelectedTrack = _PreviousSelectedTrack; // Re-select the previous track

                // Ensure the AircraftViewer form is created and visible
                OpenForm();

                if (_Window == null)
                {
                    return;
                }

                // Get or create the parent and child aircraft
                var parentAircraft = _Window.GetOrCreateAircraft(_PreviousSelectedTrack.GetPilot().Callsign);
                var childAircraft = _Window.GetOrCreateAircraft(track.GetPilot().Callsign);

                // Create a traffic pairing between the parent and child aircraft
                _Window.CreateTrafficPairing(parentAircraft, childAircraft);

                ResetKeybindPressed(); // Reset KeybindPressed after creating a traffic pairing
                return;
            }

            // Check if the selected track corresponds to a parent aircraft
            if (track != null)
            {
                var parentAircraft = _AircraftList.FirstOrDefault(a => a.Callsign == track.GetPilot().Callsign);
                if (parentAircraft != null)
                {
                    // Update the designated aircraft in the AircraftViewer
                    if (_Window != null && !_Window.IsDisposed)
                    {
                        _Window.SetDesignatedAircraft(parentAircraft);
                    }
                }
                else
                {
                    // Clear the designated aircraft if no match is found
                    if (_Window != null && !_Window.IsDisposed)
                    {
                        _Window.SetDesignatedAircraft(null);
                    }
                }
            }
            else
            {
                // Clear the designated aircraft if no track is selected
                if (_Window != null && !_Window.IsDisposed)
                {
                    _Window.SetDesignatedAircraft(null);
                }
            }

            // Update the previously selected track
            _PreviousSelectedTrack = track;
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
            if (_Window == null || _Window.IsDisposed)
            {
                // Create a new AircraftViewer window if it doesn't exist or has been closed
                _Window = new AircraftViewer(_AircraftList, _AircraftPairings);
            }

            _Window.Show(Form.ActiveForm); // Show the AircraftViewer window
        }
        catch (Exception)
        {
            // Handle exceptions silently in release mode
        }
    }
}
