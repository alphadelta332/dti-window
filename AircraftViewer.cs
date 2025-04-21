using System.Collections.Concurrent;
using System.ComponentModel;
using System.Reflection;
using DTIWindow.UI;
using DTIWindow.Events;
using vatsys;
using UIColours = DTIWindow.UI.Colours;
public class AircraftViewer : BaseForm
{
    private Panel aircraftPanel; // UI panel to display the list of aircraft
    private BindingList<Aircraft> aircraftList; // List of aircraft in the system
    private Dictionary<Aircraft, List<Aircraft>> trafficPairings; // Stores traffic pairings between aircraft
    private Font terminusFont = new Font("Terminus (TTF)", 12F, System.Drawing.FontStyle.Regular); // Font for UI labels
    private Aircraft? designatedAircraft = null; // Currently designated aircraft
    private static int nextAircraftNumber = 1; // Counter for generating unique aircraft names
    private Label? activeChildLabel = null; // Tracks the currently active child label

    // Constructor for the AircraftViewer form
    public AircraftViewer(BindingList<Aircraft> aircraftList, Dictionary<Aircraft, List<Aircraft>> trafficPairings)
    {
        this.aircraftList = aircraftList; // Initialize the aircraft list
        this.trafficPairings = trafficPairings; // Initialize the traffic pairings dictionary

        // Register an event to refresh the UI when the aircraft list changes
        this.aircraftList.ListChanged += AircraftList_ListChanged;

        // Set form properties
        this.Text = "Traffic Info";
        this.Width = 200;
        this.Height = 350;
        this.BackColor = UIColours.GetColour(UIColours.Identities.WindowBackground); // Updated

        // Create the main panel for displaying aircraft
        aircraftPanel = new Panel
        {
            Dock = DockStyle.Fill,
            AutoScroll = true
        };

        // Populate the aircraft list initially
        PopulateAircraftDisplay();

        // Add the panel to the form
        this.Controls.Add(aircraftPanel);

        // Initialize the TracksChanged event subscription
        Initialize();
    }

    // Event handler to refresh the UI when the aircraft list changes
    private void AircraftList_ListChanged(object? sender, ListChangedEventArgs e)
    {
        if (InvokeRequired)
        {
            // If called from a different thread, invoke the UI update on the main thread
            Invoke(new MethodInvoker(PopulateAircraftDisplay));
        }
        else
        {
            // Update the UI directly
            PopulateAircraftDisplay();
        }
    }

    // Populates the aircraft display with UI elements
    public void PopulateAircraftDisplay()
    {
        try
        {
            aircraftPanel.Controls.Clear(); // Clear all previous UI elements
            int yOffset = 10; // Y-positioning for UI elements

            // Display traffic pairings for each aircraft
            foreach (var aircraft in aircraftList)
            {
                // Retrieve the HMI state and color for the aircraft
                var (hmiState, color) = GetHMIStateAndColor(aircraft.Callsign);

                // Create a label for the parent aircraft
                Label parentLabel = new Label
                {
                    Text = aircraft.Callsign,
                    Font = terminusFont,
                    ForeColor = color, // Updated
                    Location = new Point(30, yOffset),
                    AutoSize = true
                };
                aircraftPanel.Controls.Add(parentLabel);

                // Create a panel to indicate designation status with a white square
                Panel boxPanel = new Panel
                {
                    Size = new Size(16, 16),
                    Location = new Point(parentLabel.Location.X - 20, parentLabel.Location.Y),
                    BorderStyle = BorderStyle.None
                };

                // Draw the white square and fill it if the aircraft is designated
                boxPanel.Paint += (s, e) =>
                {
                    e.Graphics.SmoothingMode = System.Drawing.Drawing2D.SmoothingMode.AntiAlias;
                    using (Pen pen = new Pen(UIColours.GetColour(UIColours.Identities.DesignationBox), 3)) // Updated
                    {
                        e.Graphics.DrawRectangle(pen, new Rectangle(0, 0, boxPanel.Width - 1, boxPanel.Height - 1));
                    }

                    if (designatedAircraft != null && designatedAircraft.Callsign == aircraft.Callsign)
                    {
                        using (Brush brush = new SolidBrush(UIColours.GetColour(UIColours.Identities.DesignationBox))) // Updated
                        {
                            e.Graphics.FillRectangle(brush, new Rectangle(1, 1, boxPanel.Width - 2, boxPanel.Height - 2));
                        }
                    }
                };

                aircraftPanel.Controls.Add(boxPanel);

                yOffset += 25;

                // Display child aircraft under the parent
                foreach (var child in aircraft.Children)
                {
                    Label childLabel = new Label
                    {
                        Text = child.Callsign,
                        Font = terminusFont,
                        ForeColor = child.Status == "Passed"
                            ? UIColours.GetColour(UIColours.Identities.ChildLabelPassedText) // Updated
                            : UIColours.GetColour(UIColours.Identities.ChildLabelUnpassedText), // Updated
                        Location = new Point(100, yOffset),
                        AutoSize = true,
                        BackColor = UIColours.GetColour(UIColours.Identities.ChildLabelBackground) // Updated
                    };

                    // Set event handlers for mouse actions on child labels
                    childLabel.MouseDown += (sender, e) => ChildLabel_MouseDown(sender, e, aircraft, child);
                    childLabel.MouseUp += (sender, e) => ChildLabel_MouseUp(sender, e, aircraft, child);

                    aircraftPanel.Controls.Add(childLabel);
                    yOffset += 20;
                }

                yOffset += 10;
            }
        }
        catch (Exception)
        {
            // Handle exceptions silently in release mode
        }
    }

    // Handles mouse down on child aircraft labels
    private void ChildLabel_MouseDown(object? sender, MouseEventArgs e, Aircraft parent, ChildAircraft child)
    {
        if (sender is Label childLabel)
        {
            // Highlight the background while the mouse button is held
            childLabel.BackColor = UIColours.GetColour(UIColours.Identities.ChildLabelBackgroundClick); // Updated

            // Change the text color to white
            childLabel.ForeColor = UIColours.GetColour(UIColours.Identities.ChildLabelTextClick); // Updated

            // Track the active child label
            activeChildLabel = childLabel;

            // Capture mouse input
            childLabel.Capture = true;
        }
    }

    // Handles mouse up on child aircraft labels
    private void ChildLabel_MouseUp(object? sender, MouseEventArgs e, Aircraft parent, ChildAircraft child)
    {
        if (sender is Label childLabel)
        {
            // Release mouse input
            childLabel.Capture = false;

            // Reset the background color when the mouse button is released
            childLabel.BackColor = UIColours.GetColour(UIColours.Identities.ChildLabelBackground); // Updated

            // Perform the action based on the mouse button released
            try
            {
                if (e.Button == MouseButtons.Left)
                {
                    ChildrenEvents.HandleLeftClick(child);
                }
                else if (e.Button == MouseButtons.Right)
                {
                    ChildrenEvents.HandleRightClick(child);
                }
                else if (e.Button == MouseButtons.Middle)
                {
                    ChildrenEvents.HandleMiddleClick(parent, child, aircraftList);
                }

                // Refresh the UI to reflect the change
                PopulateAircraftDisplay();
            }
            catch (Exception)
            {
                // Handle exceptions silently in release mode
            }
            finally
            {
                activeChildLabel = null;
            }
        }
    }

    // Ensures an aircraft exists in the list; creates it if needed
    public Aircraft GetOrCreateAircraft(string callsign)
    {
        // Find the aircraft by callsign
        Aircraft? aircraft = aircraftList.FirstOrDefault(a => a.Callsign == callsign);

        if (aircraft == null)
        {
            // If the aircraft doesn't exist, create it
            aircraft = new Aircraft($"Aircraft{nextAircraftNumber++}", callsign);
            aircraftList.Add(aircraft);
        }

        return aircraft;
    }

    // Creates a traffic pairing between two aircraft
    public void CreateTrafficPairing(Aircraft firstAircraft, Aircraft secondAircraft)
    {
        if (firstAircraft == secondAircraft)
        {
            return; // Prevent self-pairing
        }

        // Add children to reflect the traffic pairing
        firstAircraft.AddChild(new ChildAircraft("Child", secondAircraft.Callsign, "Unpassed"));
        secondAircraft.AddChild(new ChildAircraft("Child", firstAircraft.Callsign, "Unpassed"));

        // Update the traffic pairings dictionary
        if (!trafficPairings.ContainsKey(firstAircraft))
        {
            trafficPairings[firstAircraft] = new List<Aircraft>();
        }
        if (!trafficPairings[firstAircraft].Contains(secondAircraft))
        {
            trafficPairings[firstAircraft].Add(secondAircraft);
        }

        if (!trafficPairings.ContainsKey(secondAircraft))
        {
            trafficPairings[secondAircraft] = new List<Aircraft>();
        }
        if (!trafficPairings[secondAircraft].Contains(firstAircraft))
        {
            trafficPairings[secondAircraft].Add(firstAircraft);
        }

        // Ensure the designated aircraft is set (if not already set)
        if (designatedAircraft == null)
        {
            designatedAircraft = firstAircraft; // Default to the first aircraft in the pairing
        }

        // Refresh the UI to reflect the pairing and designation
        PopulateAircraftDisplay();
    }

    protected override void OnPreviewClientMouseUp(BaseMouseEventArgs e)
    {
        if (e.Button == MouseButtons.Middle)
        {
            // Get the mouse position relative to the aircraftPanel
            Point mousePosition = aircraftPanel.PointToClient(Cursor.Position);

            // Iterate through the child controls of the aircraftPanel
            foreach (Control control in aircraftPanel.Controls)
            {
                if (control is Label childLabel && control.Bounds.Contains(mousePosition))
                {
                    // Find the parent and child objects associated with the label
                    foreach (var parent in aircraftList)
                    {
                        var child = parent.Children.FirstOrDefault(c => c.Callsign == childLabel.Text);
                        if (child != null)
                        {
                            HandleMiddleClick(parent, child);
                            break;
                        }
                    }

                    // Prevent the default behavior by not calling the base method for middle-click
                    return;
                }
            }

            return; // Prevent the default behavior if no label is found
        }

        base.OnPreviewClientMouseUp(e); // Call the base method for other mouse buttons
    }

    private void HandleMiddleClick(Aircraft parent, ChildAircraft child)
    {
        // Remove the child from the parent's children list
        parent.Children.Remove(child);

        // If the parent has no more children, remove the parent from the aircraft list
        if (parent.Children.Count == 0)
        {
            aircraftList.Remove(parent);
        }

        // Refresh the UI to reflect the change
        PopulateAircraftDisplay();
    }

    public void SetDesignatedAircraft(Aircraft? aircraft)
    {
        designatedAircraft = aircraft;

        // Refresh the UI to reflect the change
        PopulateAircraftDisplay();
    }

    private static string GetFDRState(string callsign)
    {
        try
        {
            // Use reflection to access the AircraftTracks field
            var aircraftTracksField = typeof(MMI).GetField("AircraftTracks", BindingFlags.Static | BindingFlags.NonPublic);
            if (aircraftTracksField == null)
            {
                return "AircraftTracks field not found";
            }

            // Get the value of AircraftTracks and cast it to the correct type
            var aircraftTracks = aircraftTracksField.GetValue(null) as ConcurrentDictionary<object, Track>;
            if (aircraftTracks == null)
            {
                return "AircraftTracks is null";
            }

            // Find the track with the matching callsign
            var matchingTrack = aircraftTracks.Values.FirstOrDefault(track =>
            {
                var fdr = track.GetFDR();
                return fdr?.Callsign == callsign;
            });

            if (matchingTrack == null)
            {
                return "No matching track found";
            }

            // Retrieve the FDR state
            var fdrState = matchingTrack.GetFDR()?.State;
            return fdrState?.ToString() ?? "Unknown State";
        }
        catch (Exception)
        {
            return "Error retrieving FDR state";
        }
    }

    private static string GetHMIState(string callsign)
    {
        try
        {
            // Use reflection to access the AircraftTracks field
            var aircraftTracksField = typeof(MMI).GetField("AircraftTracks", BindingFlags.Static | BindingFlags.NonPublic);
            if (aircraftTracksField == null)
            {
                return "AircraftTracks field not found";
            }

            // Get the value of AircraftTracks and cast it to the correct type
            var aircraftTracks = aircraftTracksField.GetValue(null) as ConcurrentDictionary<object, Track>;
            if (aircraftTracks == null)
            {
                return "AircraftTracks is null";
            }

            // Find the track with the matching callsign
            var matchingTrack = aircraftTracks.Values.FirstOrDefault(track =>
            {
                var fdr = track.GetFDR();
                return fdr?.Callsign == callsign;
            });

            if (matchingTrack == null)
            {
                return "No matching track found";
            }

            // Retrieve the HMI state
            var hmiState = matchingTrack.State;
            return hmiState.ToString();
        }
        catch (Exception)
        {
            return "Error retrieving HMI state";
        }
    }

    // Method to get the HMI state and corresponding color
    public static (string hmiState, Color color) GetHMIStateAndColor(string callsign)
    {
        try
        {
            // Retrieve the HMI state using the existing method
            string hmiState = GetHMIState(callsign);

            // Delegate both the state and color logic to the Colours class
            return UIColours.GetHMIStateAndColor(hmiState);
        }
        catch
        {
            // Let the Colours class handle exceptions and return default values
            return UIColours.GetHMIStateAndColor(string.Empty);
        }
    }

    public void Initialize()
    {
        try
        {
            // Get the TracksChanged event using reflection
            EventInfo tracksChangedEvent = typeof(MMI).GetEvent("TracksChanged", BindingFlags.Static | BindingFlags.NonPublic);
            if (tracksChangedEvent == null)
            {
                return;
            }

            // Get the backing field for the TracksChanged event
            FieldInfo? eventField = typeof(MMI).GetField("TracksChanged", BindingFlags.Static | BindingFlags.NonPublic);
            if (eventField == null)
            {
                return;
            }

            // Get the current value of the event (the delegate)
            Delegate? currentDelegate = eventField.GetValue(null) as Delegate;

            // Create a delegate for the OnTracksChanged method
            MethodInfo onTracksChangedMethod = typeof(AircraftViewer).GetMethod("OnTracksChanged", BindingFlags.Instance | BindingFlags.NonPublic);
            if (onTracksChangedMethod == null)
            {
                return;
            }

            // Get the event handler type (EventHandler<TracksChangedEventArgs>)
            Type? eventHandlerType = tracksChangedEvent.EventHandlerType;
            if (eventHandlerType == null)
            {
                return;
            }

            // Create a delegate of the correct type for the event handler
            Delegate newDelegate = Delegate.CreateDelegate(eventHandlerType, this, onTracksChangedMethod);

            // Combine the new delegate with the existing delegate
            Delegate? combinedDelegate = Delegate.Combine(currentDelegate, newDelegate);

            // Set the combined delegate back to the event field
            eventField.SetValue(null, combinedDelegate);
        }
        catch (Exception)
        {
            // Handle exceptions silently in release mode
        }
    }

    // Event handler method
    private void OnTracksChanged(object sender, object e)
    {
        try
        {
            // Dynamically check if the event args are of type TracksChangedEventArgs
            var tracksChangedEventArgsType = typeof(MMI).Assembly.GetType("vatsys.TracksChangedEventArgs");
            if (tracksChangedEventArgsType != null && tracksChangedEventArgsType.IsInstanceOfType(e))
            {
                // Access the 'Track' property
                var trackProperty = tracksChangedEventArgsType.GetProperty("Track", BindingFlags.Instance | BindingFlags.NonPublic | BindingFlags.Public);
                var track = trackProperty?.GetValue(e);

                // Access the 'Removed' property
                var removedProperty = tracksChangedEventArgsType.GetProperty("Removed", BindingFlags.Instance | BindingFlags.NonPublic | BindingFlags.Public);
                bool removed = removedProperty != null && (bool)removedProperty.GetValue(e);
            }

            // Refresh the aircraft display
            PopulateAircraftDisplay();
        }
        catch (Exception)
        {
            // Handle exceptions silently in release mode
        }
    }

    protected override void WndProc(ref Message m)
    {
        const int WM_PARENTNOTIFY = 0x0210; // Parent notify message
        const int WM_MBUTTONDOWN = 0x0207; // Middle mouse button down
        const int WM_MBUTTONUP = 0x0208;   // Middle mouse button up

        // Check if the message is WM_PARENTNOTIFY
        if (m.Msg == WM_PARENTNOTIFY)
        {
            int lowWord = m.WParam.ToInt32() & 0xFFFF; // Extract the low word of wParam

            if (lowWord == WM_MBUTTONDOWN)
            {
                // Capture mouse input
                this.Capture = true;

                // Get the mouse position relative to the aircraftPanel
                Point mousePosition = aircraftPanel.PointToClient(Cursor.Position);

                // Check if the mouse is over a child label
                foreach (Control control in aircraftPanel.Controls)
                {
                    if (control is Label childLabel && childLabel.Bounds.Contains(mousePosition))
                    {
                        // Highlight the label background
                        childLabel.BackColor = UIColours.GetColour(UIColours.Identities.ChildLabelBackgroundClick);

                        // Change the text color to white
                        childLabel.ForeColor = UIColours.GetColour(UIColours.Identities.ChildLabelTextClick);

                        // Track the active child label
                        activeChildLabel = childLabel;

                        return; // Prevent further processing
                    }
                }

                // If no label is found, clear the active child label
                activeChildLabel = null;
            }
        }

        // Check for WM_MBUTTONUP
        if (m.Msg == WM_MBUTTONUP)
        {
            // Release mouse input
            this.Capture = false;

            // Use the active child label if it exists
            if (activeChildLabel != null)
            {
                // Reset the label background
                activeChildLabel.BackColor = UIColours.GetColour(UIColours.Identities.ChildLabelBackground);

                // Reset the text color to its original state
                var associatedChild = aircraftList
                    .SelectMany(parent => parent.Children)
                    .FirstOrDefault(c => c.Callsign == activeChildLabel.Text);

                if (associatedChild != null)
                {
                    activeChildLabel.ForeColor = associatedChild.Status == "Passed"
                        ? UIColours.GetColour(UIColours.Identities.ChildLabelPassedText)
                        : UIColours.GetColour(UIColours.Identities.ChildLabelUnpassedText);

                    // Perform the action
                    foreach (var parent in aircraftList)
                    {
                        var child = parent.Children.FirstOrDefault(c => c.Callsign == activeChildLabel.Text);
                        if (child != null)
                        {
                            parent.Children.Remove(child);
                            if (parent.Children.Count == 0)
                            {
                                aircraftList.Remove(parent);
                            }
                            PopulateAircraftDisplay();
                            break;
                        }
                    }
                }

                // Clear the active child label
                activeChildLabel = null;

                return; // Prevent further processing
            }
        }

        // Call the base method for other messages
        base.WndProc(ref m);
    }
}
