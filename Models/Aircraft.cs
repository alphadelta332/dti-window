using System.ComponentModel;
using vatsys;

namespace DTIWindow.Models
{
    public class Aircraft
    {
        public string Callsign { get; set; }
        public BindingList<ChildAircraft> Children { get; set; } = new BindingList<ChildAircraft>();
        public bool IsDesignated { get; set; } = false;

        public Aircraft(string callsign)
        {
            Callsign = callsign ?? throw new ArgumentNullException(nameof(callsign));
        }

        public void AddChild(ChildAircraft child)
        {
            if (!Children.Any(c => c.Callsign == child.Callsign))
                Children.Add(child);
        }

        public void SetDesignatedAircraft(bool triggeredByDesignateWithWindow = false)
        {
            if (triggeredByDesignateWithWindow)
            {
                IsDesignated = true;
                return;
            }

            var designatedCallsign = MMI.SelectedTrack?.GetPilot()?.Callsign;
            if (!string.IsNullOrEmpty(designatedCallsign) && Callsign == designatedCallsign)
                IsDesignated = true;
        }
    }
}
