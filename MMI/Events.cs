
using System.ComponentModel;
using System.Runtime.InteropServices;
using DTIWindow.Aircraft;
using vatsys;

namespace DTIWindow.MMI
{
    public class Events : Form
    {
        public static bool KeybindPressed; // Tracks if the F7 key is currently pressed
        public Track? PreviousSelectedTrack; // Stores the previously selected radar track
        public AircraftViewer? Window; // Reference to the DTI Window form
        public CancellationTokenSource? keybindTimeout;
        public const int WH_KEYBOARD_LL = 13; // Low-level keyboard hook constant
        public const int WM_KEYDOWN = 0x0100; // Windows message for key down
        public const int WM_KEYUP = 0x0101; // Windows message for key up
        public static IntPtr _hookID = IntPtr.Zero; // Handle for the global keyboard hook
        public delegate IntPtr LowLevelKeyboardProc(int nCode, IntPtr wParam, IntPtr lParam); // Delegate for the keyboard hook callback
        public static LowLevelKeyboardProc _proc = HookCallback; // Delegate for the keyboard hook callback
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

        // Event handler for when a radar track is selected
        public void TrackSelected(object sender, EventArgs e)
        {
            try
            {
                var track = vatsys.MMI.SelectedTrack; // Get the currently selected track

                if (PreviousSelectedTrack != null && track != PreviousSelectedTrack && track != null && KeybindPressed)
                {
                    vatsys.MMI.SelectedTrack = PreviousSelectedTrack; // Re-select the previous track

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
                    var parentAircraft = AircraftManager.Instance.AircraftList.FirstOrDefault(a => a.Callsign == track.GetPilot().Callsign);
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
        public void OpenForm(object? sender = null, EventArgs? e = null)
        {
            try
            {
                if (Window == null || Window.IsDisposed)
                {
                    // Create a new AircraftViewer window if it doesn't exist or has been closed
                    Window = new AircraftViewer(new BindingList<DTIWindow.Aircraft.Aircraft>(AircraftManager.Instance.AircraftList), DTIWindow.Aircraft.Pairings.AircraftPairings);
                }

                Window.Show(Form.ActiveForm); // Show the AircraftViewer window
            }
            catch (Exception)
            {
                // Handle exceptions silently in release mode
            }
        }
        public static void ResetKeybindPressed()
        {
            KeybindPressed = false;
        }
    }
}