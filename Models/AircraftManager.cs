using System.ComponentModel;

namespace DTIWindow.Models
{
    public class AircraftManager
    {
        private static AircraftManager? _instance;
        public static AircraftManager Instance => _instance ??= new AircraftManager();
        public BindingList<Aircraft> aircraftList = new BindingList<Aircraft>(); // Initialize here
        public static BindingList<Aircraft> AircraftList => Instance.aircraftList; // Static property
        private static int nextAircraftNumber = 1; // Counter for generating unique aircraft names
        public static Dictionary<string, ChildAircraft> CallsignToChildMap = new Dictionary<string, ChildAircraft>();


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
        public static void RebuildCallsignToChildMap()
        {
            CallsignToChildMap.Clear();
            foreach (var parent in AircraftList)
            {
                foreach (var child in parent.Children)
                {
                    CallsignToChildMap[child.Callsign] = child;
                }
            }
        }

    }
}
