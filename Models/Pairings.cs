using System.Collections.Generic;
using System.Linq;

namespace DTIWindowPlugin.Models
{
    // Manages traffic pairings between parent and child aircraft
    public static class Pairings
    {
        // Dictionary to store traffic pairings (parent callsign -> list of child callsigns)
        private static readonly Dictionary<string, List<string>> trafficPairs = new Dictionary<string, List<string>>();

        // Adds a traffic pairing between a parent and a child aircraft
        public static void AddPair(string parentCallsign, string childCallsign)
        {
            // If the parent does not exist in the dictionary, add it
            if (!trafficPairs.ContainsKey(parentCallsign))
            {
                trafficPairs[parentCallsign] = new List<string>();
            }

            // Add the child callsign to the parent's list if it doesn't already exist
            if (!trafficPairs[parentCallsign].Contains(childCallsign))
            {
                trafficPairs[parentCallsign].Add(childCallsign);
            }
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
    }
}
