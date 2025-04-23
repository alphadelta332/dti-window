using System.ComponentModel;

namespace DTIWindow.Models
{
    public class AircraftManager
    {
        private static AircraftManager? _instance;
        public static AircraftManager Instance => _instance ??= new AircraftManager();

        public BindingList<Aircraft> AircraftList { get; private set; }

        private AircraftManager()
        {
            AircraftList = new BindingList<Aircraft>();
        }

        public Aircraft GetOrCreateAircraft(string callsign)
        {
            var aircraft = AircraftList.FirstOrDefault(a => a.Callsign == callsign);
            if (aircraft == null)
            {
                aircraft = new Aircraft("DefaultName", callsign);
                AircraftList.Add(aircraft);
            }
            return aircraft;
        }
    }
}
