using System.ComponentModel.Composition;
using System.Runtime.InteropServices;

using vatsys;
using vatsys.Plugin;

namespace DtiWindow;

/// <summary>
/// Main plugin class for the DTI Window.
/// </summary>
[Export(typeof(IPlugin))]
public class DTIWindow : Form, IPlugin
{
    private static readonly NativeMethods.LowLevelKeyboardProc Proc = HookCallback;

    private static volatile bool _keybindPressed;
    private static IntPtr _hookID = IntPtr.Zero;

    private readonly CustomToolStripMenuItem _opener;

    private Track? _previousSelectedTrack;
    private AircraftViewer? _window;
    private CancellationTokenSource? _keybindTimeout;
    private AircraftManager _aircraftManager = new();

    /// <summary>
    /// Initializes a new instance of the <see cref="DTIWindow"/> class.
    /// </summary>
    [ImportingConstructor]
    public DTIWindow()
    {
        _opener = new CustomToolStripMenuItem(
            CustomToolStripMenuItemWindowType.Main,
            CustomToolStripMenuItemCategory.Windows,
            new ToolStripMenuItem("Traffic Info"));
        _opener.Item.Click += OpenForm;
        MMI.AddCustomMenuItem(_opener);

        MMI.SelectedTrackChanged += TrackSelected;

        MMI.InvokeOnGUI(() =>
        {
            if (Application.OpenForms["Mainform"] is { } mainForm)
            {
                mainForm.KeyUp += OnKeyUp;
                mainForm.KeyDown += OnKeyDown;
                mainForm.LostFocus += (_, _) => _keybindPressed = false;
            }
        });

        StartGlobalHook();
    }

    /// <inheritdoc/>
    public new string Name => "DTI Window";

    /// <summary>
    /// Resets the keybind pressed state to false.
    /// This is used to clear the state when the keybind timeout expires or loses focus.
    /// </summary>
    public static void ResetKeybindPressed() => _keybindPressed = false;

    /// <summary>
    /// Starts the global keyboard hook to listen for key presses.
    /// This sets up a low-level Windows keyboard hook using P/Invoke.
    /// </summary>
    public static void StartGlobalHook()
    {
        if (_hookID == IntPtr.Zero)
        {
            _hookID = NativeMethods.SetHook(Proc);
        }
    }

    /// <summary>
    /// Stops and removes the global keyboard hook if it is currently active.
    /// This ensures that the low-level Windows keyboard hook is unregistered to prevent memory leaks or conflicts.
    /// </summary>
    public static void StopGlobalHook()
    {
        if (_hookID != IntPtr.Zero)
        {
            NativeMethods.UnhookWindowsHookEx(_hookID);
            _hookID = IntPtr.Zero;
        }
    }

    /// <inheritdoc/>
    /// <summary>
    /// This method is triggered when an FDR (Flight Data Record) update occurs.
    /// Currently, this plugin does not process FDR updates, so the method is unused.
    /// </summary>
    /// <param name="updated">The updated flight data record.</param>
    public void OnFDRUpdate(FDP2.FDR updated)
    {
    }

    /// <inheritdoc/>
    /// <summary>
    /// This method is triggered when a radar track update occurs.
    /// Currently, this plugin does not process radar updates, so the method is unused.
    /// </summary>
    /// <param name="updated">The updated radar track data.</param>
    public void OnRadarTrackUpdate(RDP.RadarTrack updated)
    {
    }

    /// <summary>
    /// Releases unmanaged and managed resources.
    /// </summary>
    /// <param name="disposing">True if called from <see cref="Dispose"/>; otherwise, false.</param>
    protected override void Dispose(bool disposing)
    {
        if (disposing)
        {
            StopGlobalHook();
            _keybindTimeout?.Dispose();
            _window?.Dispose();
            _aircraftManager?.Dispose();
        }

        base.Dispose(disposing);
    }

    private static IntPtr HookCallback(int nCode, IntPtr wParam, IntPtr lParam)
    {
        if (nCode >= 0)
        {
            var vkCode = Marshal.ReadInt32(lParam);
            _keybindPressed = (wParam == (IntPtr)NativeMethods.WM_KEYDOWN && vkCode == (int)Keys.F7)
                || ((wParam != (IntPtr)NativeMethods.WM_KEYUP || vkCode != (int)Keys.F7) && _keybindPressed);
        }

        return NativeMethods.CallNextHookEx(_hookID, nCode, wParam, lParam);
    }

    private void OnKeyUp(object? sender, KeyEventArgs e)
    {
        if (e.KeyCode == Keys.F7)
        {
            _keybindPressed = false;
            _keybindTimeout?.Cancel();
        }
    }

    private async void OnKeyDown(object? sender, KeyEventArgs e)
    {
        if (e.KeyCode == Keys.F7)
        {
            _keybindPressed = true;
            _keybindTimeout?.Cancel();

            _keybindTimeout = new CancellationTokenSource();
            var token = _keybindTimeout.Token;

            try
            {
                await Task.Delay(5000, token);
                if (!token.IsCancellationRequested)
                {
                    MMI.InvokeOnGUI(() => _keybindPressed = false);
                }
            }
            catch (TaskCanceledException)
            {
                // Expected when cancelled, no action needed.
            }
        }
    }

    private void TrackSelected(object? sender, EventArgs e)
    {
        try
        {
            var track = MMI.SelectedTrack;

            if (_previousSelectedTrack != null && track != _previousSelectedTrack && track != null && _keybindPressed)
            {
                MMI.SelectedTrack = _previousSelectedTrack;
                OpenForm();
                if (_window == null)
                {
                    return;
                }

                _aircraftManager.CreateTrafficPairing(_previousSelectedTrack.GetPilot().Callsign, track.GetPilot().Callsign);
                ResetKeybindPressed();
                return;
            }

            if (track != null)
            {
                _aircraftManager.SetDesignatedAircraft(track.GetPilot().Callsign);
            }
            else
            {
                _aircraftManager.SetDesignatedAircraft(null);
            }

            _previousSelectedTrack = track;
        }
        catch
        {
            // Handle exceptions silently in release mode
        }
    }

    private void OpenForm(object? sender = null, EventArgs? e = null)
    {
        try
        {
            if (_window?.IsDisposed != false)
            {
                _window = new AircraftViewer(_aircraftManager);
            }

            MMI.InvokeOnGUI(() => _window.Show(this));
        }
        catch
        {
            // Handle exceptions silently in release mode
        }
    }
}
