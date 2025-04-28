using System.ComponentModel;
using DTIWindow.Integration;

namespace DTIWindow.Models
{
    public class Aircraft
    {
        public string Name { get; set; } // Name of the aircraft
        public string Callsign { get; set; } // Callsign of the aircraft
        public BindingList<ChildAircraft> Children { get; set; } = new BindingList<ChildAircraft>(); // List of child aircraft associated with this aircraft
        public Aircraft? designatedAircraft { get; set; } = null; // Currently designated aircraft

        public Aircraft(string name, string callsign)
        {
            // Ensure no null values are passed
            Name = name ?? throw new ArgumentNullException(nameof(name), "Aircraft name cannot be null.");
            Callsign = callsign ?? throw new ArgumentNullException(nameof(callsign), "Aircraft callsign cannot be null.");
            Children = new BindingList<ChildAircraft>(); // Initialize the list of children
        }

        // Adds a child aircraft to the list if it doesn't already exist
        public void AddChild(ChildAircraft child)
        {
            // Check if a child with the same callsign already exists
            if (!Children.Any(c => c.Callsign == child.Callsign))
            {
                Children.Add(child);
            }
        }
        public void SetDesignatedAircraft(bool triggeredByDesignateWithWindow = false)
        {
            // If triggered by DesignateWithWindow, set this aircraft as the designated aircraft
            if (triggeredByDesignateWithWindow)
            {
                designatedAircraft = this;
                return;
            }

            // Retrieve the designated aircraft from the Tracks class
            var designatedAircraftCallsign = Tracks.GetDesignatedTrack()?.GetPilot()?.Callsign;

            if (string.IsNullOrEmpty(designatedAircraftCallsign))
            {
                return; // Exit if no valid designated aircraft is found
            }

            // Ensure this aircraft matches the designated callsign
            if (Callsign != designatedAircraftCallsign)
            {
                return;
            }

            // Set this aircraft as the designated aircraft
            designatedAircraft = this;
        }
    }
}