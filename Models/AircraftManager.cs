using System.ComponentModel;
using System.Diagnostics;

namespace DTIWindow.Models
{
    public class AircraftManager
    {
        private static AircraftManager? _instance;
        public static AircraftManager Instance => _instance ??= new AircraftManager();
        public BindingList<Aircraft> aircraftList = new BindingList<Aircraft>(); // List of aircraft in the system
        public BindingList<Aircraft> AircraftList { get; private set; }
        private static int nextAircraftNumber = 1; // Counter for generating unique aircraft names

        public AircraftManager()
        {
            AircraftList = new BindingList<Aircraft>();
        }

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

            Debug.WriteLine($"Aircraft created or retrieved: {callsign}");

            return aircraft;
        }
    }
}
