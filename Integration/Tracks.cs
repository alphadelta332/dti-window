using vatsys;
using System.Reflection;
using System.Collections.Concurrent;

namespace DTIWindow.Integration
{
    public static class Tracks
    {
        /// Returns the currently selected (designated) track from vatSys.
        public static Track? GetDesignatedTrack()
        {
            return MMI.SelectedTrack;
        }

        /// Returns the callsign of the designated aircraft, or null if none is selected.
        public static string? GetDesignatedCallsign()
        {
            return MMI.SelectedTrack.GetPilot().Callsign;
        }
    }
    public static class States
    {
        /// Retrieves the HMI state for a given aircraft based on its callsign.
        public static string GetHMIState(string callsign)
        {
            try
            {
                // Access the AircraftTracks field using reflection
                var aircraftTracksField = typeof(MMI).GetField("AircraftTracks", BindingFlags.Static | BindingFlags.NonPublic);
                if (aircraftTracksField == null)
                {
                    return "AircraftTracks field not found";
                }

                // Get the AircraftTracks value and cast it to the correct type
                var aircraftTracks = aircraftTracksField.GetValue(null) as ConcurrentDictionary<object, Track>;
                if (aircraftTracks == null)
                {
                    return "AircraftTracks is null";
                }

                // Find the track with the matching callsign
                var matchingTrack = aircraftTracks.Values.FirstOrDefault(track =>
                {
                    var fdr = track.GetFDR();
                    return fdr?.Callsign == callsign;
                });

                if (matchingTrack == null)
                {
                    return "No matching track found";
                }

                // Retrieve and return the HMI state
                var hmiState = matchingTrack.State;
                return hmiState.ToString();
            }
            catch (Exception)
            {
                return "Error retrieving HMI state";
            }
        }
    }
}
