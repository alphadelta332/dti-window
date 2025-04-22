using System.Collections.Generic;

namespace DTIWindowPlugin.Models
{
    // Represents an aircraft in the system
    public class Aircraft
    {
        public string Callsign { get; set; } // Unique identifier for the aircraft
        public string Name { get; set; } // Name of the aircraft
        public List<Aircraft> Children { get; set; } // List of child aircraft associated with this aircraft
        public HashSet<string> PassedChildren { get; set; } // Tracks which child aircraft have been marked as "passed"

        // Constructor to initialize an aircraft with a name and callsign
        public Aircraft(string name, string callsign)
        {
            Name = name;
            Callsign = callsign;
            Children = new List<Aircraft>(); // Initialize the list of child aircraft
            PassedChildren = new HashSet<string>(); // Initialize the set of passed children
        }

        // Adds a child aircraft to the list if it doesn't already exist
        public void AddChild(Aircraft child)
        {
            if (!Children.Exists(c => c.Callsign == child.Callsign))
            {
                Children.Add(child);
            }
        }

        // Marks a child aircraft as "passed" by its callsign
        public void MarkChildAsPassed(string callsign)
        {
            PassedChildren.Add(callsign);
        }

        // Checks if a specific child aircraft has been marked as "passed"
        public bool HasChildPassed(string callsign)
        {
            return PassedChildren.Contains(callsign);
        }
    }
}
