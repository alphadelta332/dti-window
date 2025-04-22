namespace DTIWindow.Aircraft
{
    public class AircraftManager
    {
        private static readonly AircraftManager _instance = new AircraftManager();

        private readonly List<Aircraft> _aircraftList = new List<Aircraft>();

        private AircraftManager() { }

        public static AircraftManager Instance => _instance;

        public List<Aircraft> AircraftList => _aircraftList;

        public void AddAircraft(Aircraft aircraft)
        {
            if (!_aircraftList.Contains(aircraft))
            {
                _aircraftList.Add(aircraft);
            }
        }

        public Aircraft GetAircraftByCallsign(string callsign)
        {
            return _aircraftList.FirstOrDefault(a => a.Callsign == callsign);
        }

        public void Clear()
        {
            _aircraftList.Clear();
        }
    }
}
