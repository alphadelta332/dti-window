using System.ComponentModel;
using System.Diagnostics;
using DTIWindow.Models;
using DTIWindow.UI;
using vatsys;
using UIColours = DTIWindow.UI.Colours;

namespace DTIWindow.Events
{
    public class MouseEvents : BaseForm
    {
        private bool middleclickclose = false; // Prevent middle-click from closing the form
        public static Label? activeChildLabel = null; // Tracks the currently active child label
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
            Debug.WriteLine($"ChildLabel_MouseUp called for child: {child.Callsign}, button: {e.Button}");
            
            if (sender is Label childLabel)
            {
                // Release mouse input
                childLabel.Capture = false;

                // Reset the background color when the mouse button is released
                childLabel.BackColor = UIColours.GetColour(UIColours.Identities.ChildLabelBackground);

                // Perform the action based on the mouse button released
                Debug.WriteLine($"ChildLabel_MouseUp called for child: {child.Callsign}, button: {e.Button}");

                // Release mouse input
                childLabel.Capture = false;

                // Reset the background color when the mouse button is released
                childLabel.BackColor = UIColours.GetColour(UIColours.Identities.ChildLabelBackground);

                try
                {
                    if (e.Button == MouseButtons.Left)
                    {
                        // Set the status to "Passed"
                        child.Status = "Passed";
                    }
                    else if (e.Button == MouseButtons.Right)
                    {
                        Debug.WriteLine($"Right-click detected for child: {child.Callsign}");
                        child.Status = "Unpassed";
                    }
                    else if (e.Button == MouseButtons.Middle)
                    {
                        Debug.WriteLine($"Middle-click detected for child: {child.Callsign}");

                        // Remove the child from the parent's children list
                        parent.Children.Remove(child);
                        Debug.WriteLine($"Removed child: {child.Callsign} from parent: {parent.Callsign}");

                        // If the parent has no more children, remove the parent from the aircraft list
                        if (parent.Children.Count == 0)
                        {
                            AircraftManager.AircraftList.Remove(parent);
                            Debug.WriteLine($"Removed parent: {parent.Callsign} from AircraftManager.AircraftList");
                        }
                    }

                    // Refresh the existing Window instance
                    var windowInstance = Application.OpenForms.OfType<UI.Window>().FirstOrDefault();
                    if (windowInstance != null)
                    {
                        Debug.WriteLine("Refreshing the UI");
                        windowInstance.PopulateAircraftDisplay();
                    }
                    else
                    {
                        Debug.WriteLine("Window instance not found");
                    }
                }
                catch (Exception ex)
                {
                    Debug.WriteLine($"Error in ChildLabel_MouseUp: {ex.Message}");
                }
                finally
                {
                    activeChildLabel = null;
                }
            }
        }
        
        protected override void OnPreviewClientMouseUp(BaseMouseEventArgs e)
        {
            Debug.WriteLine($"OnPreviewClientMouseUp called with button: {e.Button}");

            if (e.Button == MouseButtons.Middle)
            {
                // Prevent the default behavior in BaseForm
                e.Handled = true;
                
                // Get the mouse position relative to the aircraftPanel
                var windowInstance = Application.OpenForms.OfType<Window>().FirstOrDefault();
                if (windowInstance != null)
                {
                    var WindowInstance = Application.OpenForms.OfType<Window>().FirstOrDefault();
                    if (WindowInstance != null)
                    {
                        Point mousePosition = WindowInstance.aircraftPanel.PointToClient(Cursor.Position);
                    }
                    else
                    {
                        Debug.WriteLine("Window instance not found");
                    }
                }
                else
                {
                    Debug.WriteLine("Window instance not found");
                }
                Debug.WriteLine($"Mouse position: {MousePosition}");

                // Iterate through the child controls of the aircraftPanel
                if (windowInstance?.aircraftPanel != null)
                {
                    foreach (Control control in windowInstance.aircraftPanel.Controls)
                    {
                        if (control is Label childLabel && control.Bounds.Contains(MousePosition))
                        {
                            Debug.WriteLine($"Middle-click detected on label with text: {childLabel.Text}");

                            // Find the parent and child objects associated with the label
                            foreach (var parent in AircraftManager.AircraftList)
                            {
                                var child = parent.Children.FirstOrDefault(c => c.Callsign == childLabel.Text);
                                if (child != null)
                                {
                                    Debug.WriteLine($"Found parent '{parent.Callsign}' and child '{child.Callsign}'");
                                    HandleMiddleClick(parent, child);
                                    break;
                                }
                            }

                            // Prevent the default behavior by not calling the base method for middle-click
                            return;
                        }
                    }
                }

                Debug.WriteLine("Middle-click did not hit any child label");
                return; // Prevent the default behavior if no label is found
            }

            base.OnPreviewClientMouseUp(e); // Call the base method for other mouse buttons
        }

        private void HandleMiddleClick(Aircraft parent, ChildAircraft child)
        {
            try
            {
                Debug.WriteLine($"HandleMiddleClick called for parent: {parent.Callsign}, child: {child.Callsign}");

                // Remove the child from the parent's children list
                parent.Children.Remove(child);
                Debug.WriteLine($"Removed child: {child.Callsign} from parent: {parent.Callsign}");

                // If the parent has no more children, remove the parent from the aircraft list
                if (parent.Children.Count == 0)
                {
                    AircraftManager.AircraftList.Remove(parent);
                    Debug.WriteLine($"Removed parent: {parent.Callsign} from AircraftManager.AircraftList");
                }

                // Refresh the UI to reflect the change
                var windowInstance = Application.OpenForms.OfType<UI.Window>().FirstOrDefault();
                if (windowInstance != null)
                {
                    Debug.WriteLine("Refreshing the UI");
                    windowInstance.PopulateAircraftDisplay();
                }
                else
                {
                    Debug.WriteLine("Window instance not found");
                }
            }
            catch (Exception ex)
            {
                Debug.WriteLine($"Error in HandleMiddleClick: {ex.Message}");
            }
        }

        protected override void WndProc(ref Message m)
        {
            Debug.WriteLine($"WndProc called with message: {m.Msg}");
            try
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
                        Debug.WriteLine("Middle mouse button down detected");

                        // Capture mouse input
                        this.Capture = true;

                        // Get the mouse position relative to the aircraftPanel
                        var windowInstance = Application.OpenForms.OfType<Window>().FirstOrDefault();
                        if (windowInstance != null)
                        {
                            Point mousePosition = windowInstance.aircraftPanel.PointToClient(Cursor.Position);
                        }
                        else
                        {
                            Debug.WriteLine("Window instance not found");
                        }

                        // Check if the mouse is over a child label
                        if (windowInstance?.aircraftPanel != null)
                        {
                            foreach (Control control in windowInstance.aircraftPanel.Controls)
                            {
                                if (control is Label childLabel && childLabel.Bounds.Contains(MousePosition))
                                {
                                    Debug.WriteLine($"Middle-click detected on label with text: {childLabel.Text}");

                                    // Highlight the label background
                                    childLabel.BackColor = UIColours.GetColour(UIColours.Identities.ChildLabelBackgroundClick);

                                    // Change the text color to white
                                    childLabel.ForeColor = UIColours.GetColour(UIColours.Identities.ChildLabelTextClick);

                                    // Track the active child label
                                    ChildAircraft.activeChildLabel = childLabel;
                                    Debug.WriteLine($"Active child label set to: {childLabel.Text}");

                                    return; // Prevent further processing
                                }
                            }
                        }

                        // If no label is found, clear the active child label
                        ChildAircraft.activeChildLabel = null;
                        Debug.WriteLine("No active child label found");
                    }
                }

                // Check for WM_MBUTTONUP
                if (m.Msg == WM_MBUTTONUP)
                {
                    Debug.WriteLine("Middle mouse button up detected");

                    // Release mouse input
                    this.Capture = false;

                    // Use the active child label if it exists
                    if (ChildAircraft.activeChildLabel != null)
                    {
                        Debug.WriteLine($"Active child label: {ChildAircraft.activeChildLabel.Text}");

                        // Reset the label background
                        ChildAircraft.activeChildLabel.BackColor = UIColours.GetColour(UIColours.Identities.ChildLabelBackground);

                        // Reset the text color to its original state
                        var associatedChild = AircraftManager.AircraftList
                            .SelectMany(parent => parent.Children)
                            .FirstOrDefault(c => c.Callsign == ChildAircraft.activeChildLabel.Text);

                        if (associatedChild != null)
                        {
                            ChildAircraft.activeChildLabel.ForeColor = associatedChild.Status == "Passed"
                                ? UIColours.GetColour(UIColours.Identities.ChildLabelPassedText)
                                : UIColours.GetColour(UIColours.Identities.ChildLabelUnpassedText);

                            // Perform the action
                            foreach (var parent in AircraftManager.AircraftList)
                            {
                                var child = parent.Children.FirstOrDefault(c => c.Callsign == ChildAircraft.activeChildLabel.Text);
                                if (child != null)
                                {
                                    HandleMiddleClick(parent, child);
                                    break;
                                }
                            }
                        }

                        // Clear the active child label
                        ChildAircraft.activeChildLabel = null;

                        return; // Prevent further processing
                    }
                }

                // Call the base method for other messages
                base.WndProc(ref m);
            }
            catch (Exception ex)
            {
                // Log the exception to the debug console
                Debug.WriteLine($"Unhandled exception in WndProc: {ex.Message}");
                Debug.WriteLine($"Stack Trace: {ex.StackTrace}");
            }
        }
        protected override void OnClosing(CancelEventArgs e)
        {
            Debug.WriteLine("MouseEvents form is closing");
            base.OnClosing(e);
        }
    }
}