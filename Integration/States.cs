using System.Collections.Concurrent;
using System.Reflection;
using vatsys;

namespace DTIWindow.Integration
{
    public static class States
    {
        public static string GetHMIState(string callsign)
        {
            try
            {
                var aircraftTracksField = typeof(MMI).GetField("AircraftTracks", BindingFlags.Static | BindingFlags.NonPublic);
                if (aircraftTracksField == null)
                    return "AircraftTracks field not found";

                var aircraftTracks = aircraftTracksField.GetValue(null) as ConcurrentDictionary<object, Track>;
                if (aircraftTracks == null)
                    return "AircraftTracks is null";

                var matchingTrack = aircraftTracks.Values.FirstOrDefault(track => track.GetFDR()?.Callsign == callsign);
                if (matchingTrack == null)
                    return "No matching track found";

                return matchingTrack.State.ToString();
            }
            catch (Exception)
            {
                return "Error retrieving HMI state";
            }
        }
    }
}
