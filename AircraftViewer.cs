using System.Collections.Concurrent;
using System.ComponentModel;
using System.Reflection;
using DTIWindow.Models;
using vatsys;
using UIColours = DTIWindow.UI.Colours;
public class AircraftViewer : BaseForm
{
    private Panel aircraftPanel = new Panel(); // UI panel to display the list of aircraft
    private BindingList<Aircraft> aircraftList = new BindingList<Aircraft>(); // List of aircraft in the system
    private Label? activeChildLabel = null; // Tracks the currently active child label
    private BindingList<Aircraft> aircraft;
    private Dictionary<Aircraft, List<Aircraft>> aircraftPairings;

    public AircraftViewer(BindingList<Aircraft> aircraft, Dictionary<Aircraft, List<Aircraft>> aircraftPairings)
    {
        this.aircraft = aircraft;
        this.aircraftPairings = aircraftPairings;
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
        var windowInstance = new DTIWindow.UI.Window(aircraftList, new Dictionary<Aircraft, List<Aircraft>>());
        windowInstance.PopulateAircraftDisplay();
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
            var windowInstance = new DTIWindow.UI.Window(aircraftList, new Dictionary<Aircraft, List<Aircraft>>());
            windowInstance.PopulateAircraftDisplay();
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
                    .SelectMany<Aircraft, ChildAircraft>(parent => parent.Children)
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
                            var windowInstance = new DTIWindow.UI.Window(aircraftList, new Dictionary<Aircraft, List<Aircraft>>());
                            windowInstance.PopulateAircraftDisplay();
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
