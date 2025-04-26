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
            if (sender is Label childLabel)
            {
                // Release mouse input
                childLabel.Capture = false;

                // Reset the background color when the mouse button is released
                childLabel.BackColor = UIColours.GetColour(UIColours.Identities.ChildLabelBackground);

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
                        child.Status = "Unpassed";
                    }
                    else if (e.Button == MouseButtons.Middle)
                    {

                        // Remove the child from the parent's children list
                        parent.Children.Remove(child);

                        // If the parent has no more children, remove the parent from the aircraft list
                        if (parent.Children.Count == 0)
                        {
                            AircraftManager.AircraftList.Remove(parent);
                        }
                    }

                    // Refresh the existing Window instance
                    var windowInstance = Application.OpenForms.OfType<UI.Window>().FirstOrDefault();
                    if (windowInstance != null)
                    {
                        windowInstance.PopulateAircraftDisplay();
                    }
                }
                catch (Exception)
                {
                }
                finally
                {
                    activeChildLabel = null;
                }
            }
        }
        
        protected override void OnPreviewClientMouseUp(BaseMouseEventArgs e)
        {
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
                }

                // Iterate through the child controls of the aircraftPanel
                if (windowInstance?.aircraftPanel != null)
                {
                    foreach (Control control in windowInstance.aircraftPanel.Controls)
                    {
                        if (control is Label childLabel && control.Bounds.Contains(MousePosition))
                        {

                            // Find the parent and child objects associated with the label
                            foreach (var parent in AircraftManager.AircraftList)
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
                }
                return; // Prevent the default behavior if no label is found
            }

            base.OnPreviewClientMouseUp(e); // Call the base method for other mouse buttons
        }

        private void HandleMiddleClick(Aircraft parent, ChildAircraft child)
        {
            try
            {
                // Remove the child from the parent's children list
                parent.Children.Remove(child);

                // If the parent has no more children, remove the parent from the aircraft list
                if (parent.Children.Count == 0)
                {
                    AircraftManager.AircraftList.Remove(parent);
                }

                // Refresh the UI to reflect the change
                var windowInstance = Application.OpenForms.OfType<UI.Window>().FirstOrDefault();
                if (windowInstance != null)
                {
                    windowInstance.PopulateAircraftDisplay();
                }
            }
            catch (Exception ex)
            {
                Debug.WriteLine($"Exception: {ex.Message}\n{ex.StackTrace}");
            }
        }

        protected override void WndProc(ref Message m)
        {
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

                        // Capture mouse input
                        this.Capture = true;

                        // Get the mouse position relative to the aircraftPanel
                        var windowInstance = Application.OpenForms.OfType<Window>().FirstOrDefault();
                        if (windowInstance != null)
                        {
                            Point mousePosition = windowInstance.aircraftPanel.PointToClient(Cursor.Position);
                        }

                        // Check if the mouse is over a child label
                        if (windowInstance?.aircraftPanel != null)
                        {
                            foreach (Control control in windowInstance.aircraftPanel.Controls)
                            {
                                if (control is Label childLabel && childLabel.Bounds.Contains(MousePosition))
                                {

                                    // Highlight the label background
                                    childLabel.BackColor = UIColours.GetColour(UIColours.Identities.ChildLabelBackgroundClick);

                                    // Change the text color to white
                                    childLabel.ForeColor = UIColours.GetColour(UIColours.Identities.ChildLabelTextClick);

                                    // Track the active child label
                                    ChildAircraft.activeChildLabel = childLabel;

                                    return; // Prevent further processing
                                }
                            }
                        }

                        // If no label is found, clear the active child label
                        ChildAircraft.activeChildLabel = null;
                    }
                }

                // Check for WM_MBUTTONUP
                if (m.Msg == WM_MBUTTONUP)
                {
                    // Release mouse input
                    this.Capture = false;

                    // Use the active child label if it exists
                    if (ChildAircraft.activeChildLabel != null)
                    {
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
    Debug.WriteLine($"Exception: {ex.Message}\n{ex.StackTrace}");
}
        }
        protected override void OnClosing(CancelEventArgs e)
        {
            base.OnClosing(e);
        }
    }
}