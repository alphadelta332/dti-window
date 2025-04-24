using System.ComponentModel;
using UIColours = DTIWindow.UI.Colours;

namespace DTIWindow.Models
{
// Represents a child aircraft in the system
    public class ChildAircraft
    {
        public string Name { get; set; } // Name of the child aircraft
        public string Callsign { get; set; } // Callsign of the child aircraft
        public string Status { get; set; } // Status of the child aircraft (e.g., "Passed", "Unpassed")
        private Label? activeChildLabel = null; // Tracks the currently active child label
        private BindingList<Aircraft> aircraftList = new BindingList<Aircraft>(); // List of aircraft in the system

        public ChildAircraft(string name, string callsign, string status)
        {
            // Ensure no null values are passed
            Name = name ?? throw new ArgumentNullException(nameof(name), "Child aircraft name cannot be null.");
            Callsign = callsign ?? throw new ArgumentNullException(nameof(callsign), "Child aircraft callsign cannot be null.");
            Status = status ?? throw new ArgumentNullException(nameof(status), "Child aircraft status cannot be null.");
        }
        public void ChildLabel_MouseDown(object? sender, MouseEventArgs e, Aircraft parent, ChildAircraft child)
        {
            if (sender is Label childLabel)
            {
                // Highlight the background while the mouse button is held
                childLabel.BackColor = UIColours.GetColour(UIColours.Identities.ChildLabelBackgroundClick); // Use the window title text color

                // Change the text color to white
                childLabel.ForeColor = UIColours.GetColour(UIColours.Identities.ChildLabelTextClick);

                // Track the active child label
                activeChildLabel = childLabel;

                // Capture mouse input
                childLabel.Capture = true;
            }
        }

        // Handles mouse up on child aircraft labels
        public void ChildLabel_MouseUp(object? sender, MouseEventArgs e, Aircraft parent, ChildAircraft child)
        {
            if (sender is Label childLabel)
            {
                // Release mouse input
                childLabel.Capture = false;

                // Reset the background color when the mouse button is released
                childLabel.BackColor = UIColours.GetColour(UIColours.Identities.ChildLabelBackground);

                // Perform the action based on the mouse button released
                try
                {
                    if (e.Button == MouseButtons.Left)
                    {
                        // Set the status to "Passed"
                        child.Status = "Passed";
                    }
                    else if (e.Button == MouseButtons.Right)
                    {
                        // Set the status to "Unpassed"
                        child.Status = "Unpassed";
                    }
                    else if (e.Button == MouseButtons.Middle)
                    {
                        // Remove the child from the parent's children list
                        parent.Children.Remove(child);

                        // If the parent has no more children, remove the parent from the aircraft list
                        if (parent.Children.Count == 0)
                        {
                            aircraftList.Remove(parent);
                        }
                    }

                    // Refresh the UI to reflect the change
                    var windowInstance = new DTIWindow.UI.Window(aircraftList, new Dictionary<Aircraft, List<Aircraft>>());
                    windowInstance.PopulateAircraftDisplay();
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
    }
}