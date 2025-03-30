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
    private static AircraftViewer? aircraftViewer; // Reference to the AircraftViewer window
    private static int nextAircraftNumber = 1; // Counter for generating unique aircraft names

    private bool KeybindPressed; // Tracks if the F7 key is currently pressed
    private bool ListenersDefined; // Tracks if keybind listeners have been created

    private Track? PreviousSelectedTrack; // Stores the previously selected radar track

    private AircraftViewer? Window; // Reference to the DTI Window form
    
    private readonly CustomToolStripMenuItem _opener; // Menu button for opening the DTI Window

    private BindingList<Aircraft> AircraftList = new(); // List of all parent aircraft
    private Dictionary<Aircraft, List<Aircraft>> AircraftPairings = new(); // Dictionary of traffic pairings between aircraft

    public new string Name => "DTI Window"; // Plugin name

    // Constructor for the DTIWindow plugin
    public DTIWindow()
    {
        // Initialize the menu bar button for the plugin
        _opener = new(CustomToolStripMenuItemWindowType.Main, CustomToolStripMenuItemCategory.Windows, new ToolStripMenuItem("DTI Window"));
        _opener.Item.Click += OpenForm; // Attach event handler to open the form

        MMI.AddCustomMenuItem(_opener); // Add the menu item to the vatSys menu

        MMI.SelectedTrackChanged += TrackSelected; // Attach event handler for track selection changes

        // Create keybind listeners when the main ASD is created
        MMI.InvokeOnGUI(() =>
        {
            var mainForm = Application.OpenForms["Mainform"];
            mainForm.KeyUp += KeyUp; // Attach KeyUp event handler
            mainForm.KeyDown += KeyDown; // Attach KeyDown event handler
        });
    }

    // Event handler for when a key is released
    private new void KeyUp(object sender, KeyEventArgs e)
    {
        if (e.KeyCode == Keys.F7)
        {
            KeybindPressed = false; // Set KeybindPressed to false when F7 is released
        }
    }

    // Event handler for when a key is pressed
    private new void KeyDown(object sender, KeyEventArgs e)
    {
        if (e.KeyCode == Keys.F7)
        {
            KeybindPressed = true; // Set KeybindPressed to true when F7 is pressed
        }
    }

    // Event handler for when a radar track is selected
    private void TrackSelected(object sender, EventArgs e)
    {
        try
        {
            Debug.WriteLine("========== DEBUG START ==========");
            Debug.WriteLine("TrackSelected method triggered.");

            var track = MMI.SelectedTrack; // Get the currently selected track

            if (track != null)
            {
                Debug.WriteLine($"Selected track: {track.GetPilot().Callsign}");
            }
            else
            {
                Debug.WriteLine("No track selected.");
            }

            // Debug each condition in the if statement
            if (PreviousSelectedTrack != null && track != PreviousSelectedTrack && track != null && KeybindPressed)
            {
                Debug.WriteLine($"Previous track: {PreviousSelectedTrack.GetPilot().Callsign}");
                Debug.WriteLine($"Passing traffic from {PreviousSelectedTrack.GetPilot().Callsign} to {track.GetPilot().Callsign}");

                MMI.SelectedTrack = PreviousSelectedTrack; // Re-select the previous track
                Debug.WriteLine($"{PreviousSelectedTrack.GetPilot().Callsign} Passed traffic about {track.GetPilot().Callsign}");

                // Ensure the AircraftViewer form is created and visible
                OpenForm();

                if (Window == null)
                {
                    Debug.WriteLine("Window is null. Exiting method.");
                    return;
                }

                // Get or create the parent and child aircraft
                var parentAircraft = Window.GetOrCreateAircraft(PreviousSelectedTrack.GetPilot().Callsign);
                var childAircraft = Window.GetOrCreateAircraft(track.GetPilot().Callsign);

                // Create a traffic pairing between the parent and child aircraft
                Debug.WriteLine($"Creating traffic pairing between {parentAircraft.Callsign} and {childAircraft.Callsign}");
                Window.CreateTrafficPairing(parentAircraft, childAircraft);

                Debug.WriteLine("Traffic pairing created successfully.");
                return;
            }
            else
            {
                Debug.WriteLine("Condition not met:");
                Debug.WriteLine($"PreviousSelectedTrack != null: {PreviousSelectedTrack != null}");
                Debug.WriteLine($"track != PreviousSelectedTrack: {track != PreviousSelectedTrack}");
                Debug.WriteLine($"track != null: {track != null}");
                Debug.WriteLine($"KeybindPressed: {KeybindPressed}");
            }

            // Check if the selected track corresponds to a parent aircraft
            if (track != null)
            {
                var parentAircraft = AircraftList.FirstOrDefault(a => a.Callsign == track.GetPilot().Callsign);
                if (parentAircraft != null)
                {
                    Debug.WriteLine($"Parent aircraft found: {parentAircraft.Callsign}");

                    // Update the designated aircraft in the AircraftViewer
                    if (Window != null && !Window.IsDisposed)
                    {
                        Window.SetDesignatedAircraft(parentAircraft);
                    }
                }
                else
                {
                    Debug.WriteLine("Selected track does not correspond to any parent aircraft.");

                    // Clear the designated aircraft if no match is found
                    if (Window != null && !Window.IsDisposed)
                    {
                        Window.SetDesignatedAircraft(null);
                    }
                }
            }
            else
            {
                Debug.WriteLine("No track selected.");

                // Clear the designated aircraft if no track is selected
                if (Window != null && !Window.IsDisposed)
                {
                    Window.SetDesignatedAircraft(null);
                }
            }

            // Update the previously selected track
            PreviousSelectedTrack = track;
            Debug.WriteLine("PreviousSelectedTrack updated.");
        }
        catch (Exception ex)
        {
            // Log any exceptions that occur
            Debug.WriteLine("========== EXCEPTION ==========");
            Debug.WriteLine($"An error occurred: {ex.Message}");
            Debug.WriteLine(ex.StackTrace);
            Debug.WriteLine("========== END EXCEPTION ==========");
        }
        finally
        {
            Debug.WriteLine("========== DEBUG END ==========");
        }
    }

    // Opens the DTI Window form
    private void OpenForm(object? sender = null, EventArgs? e = null)
    {
        try
        {
            Console.WriteLine("========== DEBUG START ==========");
            Console.WriteLine("OpenForm method triggered.");

            if (Window == null || Window.IsDisposed)
            {
                Console.WriteLine("Creating a new AircraftViewer window.");
                // Create a new AircraftViewer window if it doesn't exist or has been closed
                Window = new AircraftViewer(AircraftList, AircraftPairings);
            }
            else
            {
                Console.WriteLine("Reusing existing AircraftViewer window.");
            }

            Window.Show(Form.ActiveForm); // Show the AircraftViewer window
            Console.WriteLine("AircraftViewer window displayed.");
        }
        catch (Exception ex)
        {
            // Log any exceptions that occur
            Console.WriteLine("========== EXCEPTION ==========");
            Console.WriteLine($"An error occurred: {ex.Message}");
            Console.WriteLine(ex.StackTrace);
            Console.WriteLine("========== END EXCEPTION ==========");
        }
        finally
        {
            Console.WriteLine("========== DEBUG END ==========");
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

    private void ChildLabel_MouseDown(object? sender, MouseEventArgs e, Aircraft parent, ChildAircraft child)
    {
        try
        {
            Debug.WriteLine($"MouseDown event triggered. Mouse Button: {e.Button}");

            if (e.Button == MouseButtons.Left)
            {
                child.Status = child.Status == "Passed" ? "Unpassed" : "Passed";
                Debug.WriteLine($"Toggled status of child {child.Callsign} to {child.Status}");
            }
            else if (e.Button == MouseButtons.Right)
            {
                child.Status = "Unpassed";
                Debug.WriteLine($"Set status of child {child.Callsign} to Unpassed");
            }
            else if (e.Button == MouseButtons.Middle)
            {
                Debug.WriteLine("Middle mouse button clicked. Attempting to remove child...");

                parent.Children.Remove(child);
                Debug.WriteLine($"Removed child {child.Callsign} from parent {parent.Callsign}");

                if (parent.Children.Count == 0)
                {
                    AircraftList.Remove(parent);
                    Debug.WriteLine($"Removed parent {parent.Callsign} as it has no more children");
                }

                if (AircraftList.Count == 0)
                {
                    Debug.WriteLine("No parents or children remaining in the system.");
                }
            }

            Debug.WriteLine($"Parent has {parent.Children.Count} children after action.");
        }
        catch (Exception ex)
        {
            Debug.WriteLine($"Exception in ChildLabel_MouseDown: {ex.Message}");
            Debug.WriteLine(ex.StackTrace);
        }
    }

    protected override void OnMouseDown(MouseEventArgs e)
    {
        if (e.Button == MouseButtons.Middle)
        {
            Debug.WriteLine("Middle click intercepted at the form level.");
            // Prevent default behavior
            return;
        }

        base.OnMouseDown(e); // Call the base class implementation of OnMouseDown
    }

    protected override void WndProc(ref Message m)
    {
        const int WM_MBUTTONDOWN = 0x0207; // Windows message for middle mouse button down

        if (m.Msg == WM_MBUTTONDOWN)
        {
            Debug.WriteLine("Middle mouse button clicked. Intercepted in WndProc.");
            // Prevent the default behavior by not calling base.WndProc
            return;
        }

        base.WndProc(ref m);
    }

    protected override void OnFormClosing(FormClosingEventArgs e)
    {
        Debug.WriteLine("Form is attempting to close.");
        e.Cancel = true; // Prevent the form from closing
    }

    private void CloseApplication()
    {
        this.Close();
        this.Dispose();
        Application.Exit();
    }
}
