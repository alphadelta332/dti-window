using System.ComponentModel;
using System.Windows.Forms;

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
    }
}