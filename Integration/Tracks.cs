using System.Collections.Concurrent;
using System.Reflection;
using vatsys;

namespace DTIWindow.Integration
{
    public static class Tracks
    {
        /// Returns the currently selected (designated) track from vatSys.
        public static Track? GetDesignatedTrack()
        {
            return MMI.SelectedTrack;
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
