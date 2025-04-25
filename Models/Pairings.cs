namespace DTIWindow.Models
{
    // Manages traffic pairings between parent and child aircraft
    public class Pairings
    {        
        private static readonly Dictionary<string, List<string>> trafficPairs = new Dictionary<string, List<string>>();
        private static Dictionary<Aircraft, List<Aircraft>> trafficPairings = new Dictionary<Aircraft, List<Aircraft>>(); // Stores traffic pairings between aircraft
        private Aircraft? designatedAircraft = null; // Currently designated aircraft

        // Creates a traffic pairing between two aircraft
        public void CreateTrafficPairing(Aircraft firstAircraft, Aircraft secondAircraft)
        {
            if (firstAircraft == secondAircraft)
            {
                return; // Prevent self-pairing
            }

            // Add children to reflect the traffic pairing
            firstAircraft.AddChild(new ChildAircraft("Child", secondAircraft.Callsign, "Unpassed"));
            secondAircraft.AddChild(new ChildAircraft("Child", firstAircraft.Callsign, "Unpassed"));

            // Update the traffic pairings dictionary
            if (!trafficPairings.ContainsKey(firstAircraft))
            {
                trafficPairings[firstAircraft] = new List<Aircraft>();
            }
            if (!trafficPairings[firstAircraft].Contains(secondAircraft))
            {
                trafficPairings[firstAircraft].Add(secondAircraft);
            }

            if (!trafficPairings.ContainsKey(secondAircraft))
            {
                trafficPairings[secondAircraft] = new List<Aircraft>();
            }
            if (!trafficPairings[secondAircraft].Contains(firstAircraft))
            {
                trafficPairings[secondAircraft].Add(firstAircraft);
            }

            // Ensure the designated aircraft is set (if not already set)
            if (designatedAircraft == null)
            {
                designatedAircraft = firstAircraft; // Default to the first aircraft in the pairing
            }

            // Refresh the UI after all updates
            var windowInstance = Application.OpenForms.OfType<UI.Window>().FirstOrDefault();
            windowInstance?.PopulateAircraftDisplay();
        }

        // Retrieves the list of child callsigns for a given parent callsign
        public static List<string> GetChildren(string parentCallsign)
        {
            if (trafficPairs.TryGetValue(parentCallsign, out var children))
            {
                return children;
            }

            return new List<string>(); // Return an empty list if the parent has no children
        }

        // Checks if a specific traffic pairing exists between a parent and a child
        public static bool HasPair(string parentCallsign, string childCallsign)
        {
            return trafficPairs.ContainsKey(parentCallsign) && trafficPairs[parentCallsign].Contains(childCallsign);
        }

        // Retrieves all traffic pairings as a dictionary
        public static Dictionary<string, List<string>> GetAllPairs()
        {
            return trafficPairs;
        }

        public static Dictionary<Aircraft, List<Aircraft>> GetTrafficPairings()
        {
            return trafficPairings;
        }
    }
}
