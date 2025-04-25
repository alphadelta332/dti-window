namespace DTIWindow.Models
{
// Represents a child aircraft in the system
    public class ChildAircraft
    {
        public string Name { get; set; } // Name of the child aircraft
        public string Callsign { get; set; } // Callsign of the child aircraft
        public string Status { get; set; } // Status of the child aircraft (e.g., "Passed", "Unpassed")
        public static Label? activeChildLabel = null; // Tracks the currently active child label

        public ChildAircraft(string name, string callsign, string status)
        {
            // Ensure no null values are passed
            Name = name ?? throw new ArgumentNullException(nameof(name), "Child aircraft name cannot be null.");
            Callsign = callsign ?? throw new ArgumentNullException(nameof(callsign), "Child aircraft callsign cannot be null.");
            Status = status ?? throw new ArgumentNullException(nameof(status), "Child aircraft status cannot be null.");
        }
    }
}