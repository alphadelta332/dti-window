using System.ComponentModel;

namespace DTIWindow.Models
{
    public class AircraftManager
    {
        private static AircraftManager? _instance;
        public static AircraftManager Instance => _instance ??= new AircraftManager();
        public BindingList<Aircraft> aircraftList = new BindingList<Aircraft>();
        public static BindingList<Aircraft> AircraftList => Instance.aircraftList;

        public Aircraft GetOrCreateAircraft(string callsign)
        {
            Aircraft? aircraft = AircraftList.FirstOrDefault(a => a.Callsign == callsign);
            if (aircraft == null)
            {
                aircraft = new Aircraft(callsign);
                AircraftList.Add(aircraft);
            }
            return aircraft;
        }
    }
}
