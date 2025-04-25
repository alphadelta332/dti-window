using System.ComponentModel;
using DTIWindow.Models;
using vatsys;
public class AircraftViewer : BaseForm
{
    private BindingList<Aircraft> aircraft;
    private Dictionary<Aircraft, List<Aircraft>> aircraftPairings;

    public AircraftViewer(BindingList<Aircraft> aircraft, Dictionary<Aircraft, List<Aircraft>> aircraftPairings)
    {
        this.aircraft = aircraft;
        this.aircraftPairings = aircraftPairings;
    }
}
