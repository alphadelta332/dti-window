using vatsys;

namespace DTIWindow.Models
{
    // Manages traffic pairings between parent and child aircraft
    public class Pairings : BaseForm
    {
        private static Dictionary<Aircraft, List<Aircraft>> trafficPairings = new Dictionary<Aircraft, List<Aircraft>>(); // Stores traffic pairings between aircraft

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
            if (firstAircraft.designatedAircraft == null)
            {
                firstAircraft.designatedAircraft = firstAircraft; // Default to the first aircraft in the pairing
            }

            // Refresh the UI after all updates
            var windowInstance = Application.OpenForms.OfType<UI.Window>().FirstOrDefault();
            windowInstance?.PopulateAircraftDisplay();
            windowInstance?.CheckAndSetDesignatedAircraft();
        }

        public static Dictionary<Aircraft, List<Aircraft>> GetTrafficPairings()
        {
            return trafficPairings;
        }
    }
}