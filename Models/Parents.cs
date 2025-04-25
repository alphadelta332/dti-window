using System.ComponentModel;

namespace DTIWindow.Models
{
    public class Aircraft
    {
        public string Name { get; set; } // Name of the aircraft
        public string Callsign { get; set; } // Callsign of the aircraft
        public BindingList<ChildAircraft> Children { get; set; } = new BindingList<ChildAircraft>(); // List of child aircraft associated with this aircraft
        private Aircraft? designatedAircraft = null; // Currently designated aircraft

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

        // Checks if the aircraft has any child references
        public bool HasReferences()
        {
            return Children.Count > 0;
        }
        public void SetDesignatedAircraft(Aircraft? aircraft)
        {
            designatedAircraft = aircraft;

            // Refresh the existing Window instance
            var windowInstance = Application.OpenForms
                .OfType<Form>()
                .FirstOrDefault(form => form.Name == "Window");
            windowInstance?.GetType().GetMethod("PopulateAircraftDisplay")?.Invoke(windowInstance, null);
        }
    }
}