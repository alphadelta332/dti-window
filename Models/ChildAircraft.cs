namespace DTIWindow.Models
{
    public enum PairingStatus { Unpassed, Passed }

    public class ChildAircraft
    {
        public string Callsign { get; set; }
        public PairingStatus Status { get; set; }

        public ChildAircraft(string callsign, PairingStatus status)
        {
            Callsign = callsign ?? throw new ArgumentNullException(nameof(callsign));
            Status = status;
        }
    }
}
