using System.ComponentModel;
using System.Windows.Forms;
using DTIWindow.UI;
using UIColours = DTIWindow.UI.Colours;

namespace DTIWindow.Events
{
    public static class ChildrenEvents
    {
        // Handles middle-click to remove a child
        public static void HandleMiddleClick(Aircraft parent, ChildAircraft child, BindingList<Aircraft> aircraftList)
        {
            parent.Children.Remove(child);

            // If the parent has no more children, remove the parent from the aircraft list
            if (parent.Children.Count == 0)
            {
                aircraftList.Remove(parent);
            }
        }

        // Handles left-click to set the child's status to "Passed"
        public static void HandleLeftClick(ChildAircraft child)
        {
            child.Status = "Passed";
        }

        // Handles right-click to set the child's status to "Unpassed"
        public static void HandleRightClick(ChildAircraft child)
        {
            child.Status = "Unpassed";
        }

        // Handles mouse down on child aircraft labels
        public static void HandleMouseDown(Label childLabel, ref Label? activeChildLabel)
        {
            // Highlight the background while the mouse button is held
            childLabel.BackColor = UIColours.GetColour(UIColours.Identities.ChildLabelBackgroundClick);

            // Change the text color to white
            childLabel.ForeColor = UIColours.GetColour(UIColours.Identities.ChildLabelTextClick);

            // Track the active child label
            activeChildLabel = childLabel;

            // Capture mouse input
            childLabel.Capture = true;
        }

        // Handles mouse up on child aircraft labels
        public static void HandleMouseUp(Label childLabel, MouseEventArgs e, Aircraft parent, ChildAircraft child, BindingList<Aircraft> aircraftList, Action refreshUI)
        {
            // Release mouse input
            childLabel.Capture = false;

            // Reset the background color when the mouse button is released
            childLabel.BackColor = UIColours.GetColour(UIColours.Identities.ChildLabelBackground);

            // Perform the action based on the mouse button released
            if (e.Button == MouseButtons.Left)
            {
                HandleLeftClick(child);
            }
            else if (e.Button == MouseButtons.Right)
            {
                HandleRightClick(child);
            }
            else if (e.Button == MouseButtons.Middle)
            {
                HandleMiddleClick(parent, child, aircraftList);
            }

            // Refresh the UI to reflect the change
            refreshUI();
        }
    }
}