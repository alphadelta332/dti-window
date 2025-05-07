using System.ComponentModel;

namespace DTIWindow.Models
{
    public class AircraftManager
    {
        private static AircraftManager? _instance;
        public static AircraftManager Instance => _instance ??= new AircraftManager();
        public BindingList<Aircraft> aircraftList = new BindingList<Aircraft>(); // Initialise here
        public static BindingList<Aircraft> AircraftList => Instance.aircraftList; // Static property
        private static int nextAircraftNumber = 1; // Counter for generating unique aircraft names

        public Aircraft GetOrCreateAircraft(string callsign)
        {

            // Find the aircraft by callsign
            Aircraft? aircraft = AircraftList.FirstOrDefault(a => a.Callsign == callsign);

            if (aircraft == null)
            {
                // If the aircraft doesn't exist, create it
                aircraft = new Aircraft($"Aircraft{nextAircraftNumber++}", callsign);
                AircraftList.Add(aircraft); // Add to the shared list
            }

            return aircraft;
        }
    }
}
