using System.Collections.Concurrent; // For thread-safe collections
using System.ComponentModel;
using System.Reflection;
using vatsys;

namespace DtiWindow;

/// <summary>
/// Represents the AircraftViewer form, which displays and manages aircraft and their traffic pairings.
/// </summary>
public class AircraftViewer : BaseForm
{
    private static volatile int nextAircraftNumber = 1; // Counter for generating unique aircraft names
    private readonly Panel _aircraftPanel; // UI panel to display the list of aircraft
    private readonly BindingList<Aircraft> _aircraftList; // List of aircraft in the system
    private readonly Dictionary<Aircraft, List<Aircraft>> _trafficPairings; // Stores traffic pairings between aircraft
    private readonly Font _terminusFont = new("Terminus (TTF)", 12F, System.Drawing.FontStyle.Regular); // Font for UI labels
    private Aircraft? _designatedAircraft; // Currently designated aircraft

    /// <summary>
    /// Initializes a new instance of the <see cref="AircraftViewer"/> class.
    /// </summary>
    /// <param name="aircraftList">The aircraft list.</param>
    /// <param name="trafficPairings">A list of traffic pairings.</param>
    public AircraftViewer(BindingList<Aircraft> aircraftList, Dictionary<Aircraft, List<Aircraft>> trafficPairings)
    {
        _aircraftList = aircraftList; // Initialize the aircraft list
        _trafficPairings = trafficPairings; // Initialize the traffic pairings dictionary

        // Register an event to refresh the UI when the aircraft list changes
        _aircraftList.ListChanged += AircraftList_ListChanged;

        // Set form properties
        Text = "Traffic Info";
        Width = 200;
        Height = 350;
        BackColor = Colours.GetColour(Colours.Identities.WindowBackground);

        MiddleClickClose = false; // Disable middle-click close

        // Create the main panel for displaying aircraft
        _aircraftPanel = new Panel
        {
            Dock = DockStyle.Fill,
            AutoScroll = true
        };

        // Populate the aircraft list initially
        PopulateAircraftDisplay();

        // Add the panel to the form
        Controls.Add(_aircraftPanel);

        // Initialize the TracksChanged event subscription
        Initialize();
    }

    /// <summary>
    /// Sets the designated aircraft.
    /// </summary>
    /// <param name="aircraft">The aircraft to designate.</param>
    public void SetDesignatedAircraft(Aircraft? aircraft)
    {
        _designatedAircraft = aircraft;

        // Refresh the UI to reflect the change
        PopulateAircraftDisplay();
    }

    /// <summary>
    /// Ensures an aircraft exists in the list; creates it if needed.
    /// </summary>
    /// <param name="callsign">The callsign to add.</param>
    /// <returns>The created or retrieved aircraft.</returns>
    public Aircraft GetOrCreateAircraft(string callsign)
    {
        // Find the aircraft by callsign
        Aircraft? aircraft = _aircraftList.FirstOrDefault(a => a.Callsign == callsign);

        if (aircraft == null)
        {
            // If the aircraft doesn't exist, create it
            aircraft = new Aircraft($"Aircraft{nextAircraftNumber++}", callsign);
            _aircraftList.Add(aircraft);
        }

        return aircraft;
    }

    /// <summary>
    /// Creates a traffic pairing between two aircraft.
    /// </summary>
    /// <param name="firstAircraft">The first aircraft.</param>
    /// <param name="secondAircraft">The second aircraft.</param>
    public void CreateTrafficPairing(Aircraft firstAircraft, Aircraft secondAircraft)
    {
        if (firstAircraft == secondAircraft)
        {
            return; // Prevent self-pairing
        }

        // Add children to reflect the traffic pairing
        firstAircraft.AddChild(new ChildAircraft("Child", secondAircraft.Callsign, AircraftStatus.Unpassed));
        secondAircraft.AddChild(new ChildAircraft("Child", firstAircraft.Callsign, AircraftStatus.Unpassed));

        // Update the traffic pairings dictionary
        if (!_trafficPairings.TryGetValue(firstAircraft, out var firstAircraftList))
        {
            firstAircraftList = new List<Aircraft>();
            _trafficPairings[firstAircraft] = firstAircraftList;
        }

        if (!firstAircraftList.Contains(secondAircraft))
        {
            firstAircraftList.Add(secondAircraft);
        }

        if (!_trafficPairings.TryGetValue(secondAircraft, out var secondAircraftList))
        {
            secondAircraftList = new List<Aircraft>();
            _trafficPairings[secondAircraft] = secondAircraftList;
        }

        if (!secondAircraftList.Contains(firstAircraft))
        {
            secondAircraftList.Add(firstAircraft);
        }

        // Refresh the UI to reflect the pairing
        PopulateAircraftDisplay();
    }

    /// <inheritdoc/>
    protected override void Dispose(bool disposing)
    {
        if (disposing)
        {
            // Unsubscribe from the ListChanged event to prevent memory leaks
            _aircraftList.ListChanged -= AircraftList_ListChanged;
            _terminusFont.Dispose(); // Dispose of the font resource
            _aircraftPanel?.Dispose();
        }

        base.Dispose(disposing);
    }

    /// <inheritdoc/>
    protected override void OnPreviewClientMouseUp(BaseMouseEventArgs e)
    {
        if (e.Button == MouseButtons.Middle)
        {
            // Get the mouse position relative to the aircraftPanel
            var mousePosition = _aircraftPanel.PointToClient(Cursor.Position);

            // Iterate through the child controls of the aircraftPanel
            foreach (Control control in _aircraftPanel.Controls)
            {
                if (control is Label childLabel && control.Bounds.Contains(mousePosition))
                {
                    // Find the parent and child objects associated with the label
                    foreach (var parent in _aircraftList)
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

    // Map HMI states to Colours.Identities
    private static Colours.Identities MapHMIStateToIdentity(string hmiState)
    {
        return hmiState switch
        {
            "Jurisdiction" => Colours.Identities.Jurisdiction,       // Active state maps to Jurisdiction
            "HandoverOut" => Colours.Identities.Jurisdiction,       // Active state maps to Jurisdiction
            "Announced" => Colours.Identities.Announced,        // Inactive state maps to Announced
            "HandoverIn" => Colours.Identities.Announced,        // Inactive state maps to Announced
            "Preactive" => Colours.Identities.Preactive,       // Suspended state maps to Preactive
            "PostJurisdiction" => Colours.Identities.PostJurisdiction,       // Suspended state maps to Preactive
            "NonJurisdiction" => Colours.Identities.NonJurisdiction,       // Suspended state maps to Preactive
            "GhostJurisdiction" => Colours.Identities.GhostJurisdiction,       // Suspended state maps to Preactive
            _ => Colours.Identities.Default // Default color if no match
        };
    }

    // Method to get the HMI state and corresponding color
    private static (string hmiState, Color color) GetHMIStateAndColor(string callsign)
    {
        try
        {
            // Retrieve the HMI state using the existing method
            var hmiState = GetHMIState(callsign);

            if (string.IsNullOrEmpty(hmiState) || hmiState == "Unknown State")
            {
                return ("Unknown State", Color.Gray); // Return a default color for unknown states
            }

            // Map the HMI state to a Colours.Identities value
            var identity = MapHMIStateToIdentity(hmiState);

            // Retrieve the color for the identity
            var color = Colours.GetColour(identity);

            return (hmiState, color);
        }
        catch (Exception)
        {
            return ("Error", Color.Red); // Return a default error color
        }
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

    /// <summary>
    /// Populates the aircraft display with UI elements.
    /// </summary>
    private void PopulateAircraftDisplay()
    {
        try
        {
            _aircraftPanel.Controls.Clear(); // Clear all previous UI elements
            var yOffset = 10; // Y-positioning for UI elements

            // Display traffic pairings for each aircraft
            foreach (var aircraft in _aircraftList)
            {
                // Retrieve the HMI state and color for the aircraft
                var (hmiState, color) = GetHMIStateAndColor(aircraft.Callsign);

                // Create a label for the parent aircraft
                var parentLabel = new Label
                {
                    Text = aircraft.Callsign,
                    Font = _terminusFont,
                    ForeColor = color, // Set the label color based on the HMI state
                    Location = new Point(30, yOffset),
                    AutoSize = true
                };
                _aircraftPanel.Controls.Add(parentLabel);

                // Create a panel to indicate designation status with a white square
                var boxPanel = new Panel
                {
                    Size = new Size(16, 16),
                    Location = new Point(parentLabel.Location.X - 20, parentLabel.Location.Y),
                    BorderStyle = BorderStyle.None
                };

                // Draw the white square and fill it if the aircraft is designated
                boxPanel.Paint += (s, e) =>
                {
                    e.Graphics.SmoothingMode = System.Drawing.Drawing2D.SmoothingMode.AntiAlias;
                    using (var pen = new Pen(Color.White, 3))
                    {
                        e.Graphics.DrawRectangle(pen, new Rectangle(0, 0, boxPanel.Width - 1, boxPanel.Height - 1));
                    }

                    if (_designatedAircraft != null && _designatedAircraft.Callsign == aircraft.Callsign)
                    {
                        using (Brush brush = new SolidBrush(Color.White))
                        {
                            e.Graphics.FillRectangle(brush, new Rectangle(1, 1, boxPanel.Width - 2, boxPanel.Height - 2));
                        }
                    }
                };

                _aircraftPanel.Controls.Add(boxPanel);

                yOffset += 25;

                // Display child aircraft under the parent
                foreach (var child in aircraft.Children)
                {
                    var childLabel = new Label
                    {
                        Text = child.Callsign,
                        Font = _terminusFont,
                        ForeColor = child.Status == AircraftStatus.Passed ? Color.FromArgb(0, 0, 188) : Color.FromArgb(255, 255, 255),
                        Location = new Point(100, yOffset),
                        AutoSize = true
                    };

                    // Set event handler for mouse actions on child labels
                    childLabel.MouseDown += (sender, e) => ChildLabel_MouseDown(sender, e, aircraft, child);

                    _aircraftPanel.Controls.Add(childLabel);
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

    // Handles clicks on child aircraft labels
    private void ChildLabel_MouseDown(object? sender, MouseEventArgs e, Aircraft parent, ChildAircraft child)
    {
        try
        {
            if (e.Button == MouseButtons.Left)
            {
                // Toggle the status between "Passed" and "Unpassed"
                child.Status = child.Status == AircraftStatus.Passed ? AircraftStatus.Unpassed : AircraftStatus.Passed;
            }
            else if (e.Button == MouseButtons.Right)
            {
                // Set the status to "Unpassed"
                child.Status = AircraftStatus.Unpassed;
            }
            else if (e.Button == MouseButtons.Middle)
            {
                // Remove the child from the parent's children list
                parent.Children.Remove(child);

                // If the parent has no more children, remove the parent from the aircraft list
                if (parent.Children.Count == 0)
                {
                    _aircraftList.Remove(parent);
                }
            }

            // Refresh the UI to reflect the change
            PopulateAircraftDisplay();
        }
        catch (Exception)
        {
            // Handle exceptions silently in release mode
        }
    }

    private void HandleMiddleClick(Aircraft parent, ChildAircraft child)
    {
        // Remove the child from the parent's children list
        parent.Children.Remove(child);

        // If the parent has no more children, remove the parent from the aircraft list
        if (parent.Children.Count == 0)
        {
            _aircraftList.Remove(parent);
        }

        // Refresh the UI to reflect the change
        PopulateAircraftDisplay();
    }

    private void Initialize()
    {
        try
        {
            // Get the TracksChanged event using reflection
            var tracksChangedEvent = typeof(MMI).GetEvent("TracksChanged", BindingFlags.Static | BindingFlags.NonPublic);
            if (tracksChangedEvent == null)
            {
                return;
            }

            // Get the backing field for the TracksChanged event
            var eventField = typeof(MMI).GetField("TracksChanged", BindingFlags.Static | BindingFlags.NonPublic);
            if (eventField == null)
            {
                return;
            }

            // Get the current value of the event (the delegate)
            var currentDelegate = eventField.GetValue(null) as Delegate;

            // Create a delegate for the OnTracksChanged method
            var onTracksChangedMethod = typeof(AircraftViewer).GetMethod(nameof(OnTracksChanged), BindingFlags.Instance | BindingFlags.NonPublic);
            if (onTracksChangedMethod == null)
            {
                return;
            }

            // Get the event handler type (EventHandler<TracksChangedEventArgs>)
            var eventHandlerType = tracksChangedEvent.EventHandlerType;
            if (eventHandlerType == null)
            {
                return;
            }

            // Create a delegate of the correct type for the event handler
            var newDelegate = Delegate.CreateDelegate(eventHandlerType, this, onTracksChangedMethod);

            // Combine the new delegate with the existing delegate
            var combinedDelegate = Delegate.Combine(currentDelegate, newDelegate);

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
                var removed = removedProperty != null && (bool)removedProperty.GetValue(e);
            }

            // Refresh the aircraft display
            PopulateAircraftDisplay();
        }
        catch (Exception)
        {
            // Handle exceptions silently in release mode
        }
    }
}
