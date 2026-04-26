namespace DTIWindow.Models
{
    public static class Pairings
    {
        public static void CreateTrafficPairing(Aircraft firstAircraft, Aircraft secondAircraft)
        {
            if (firstAircraft == secondAircraft) return;

            firstAircraft.AddChild(new ChildAircraft(secondAircraft.Callsign, PairingStatus.Unpassed));
            secondAircraft.AddChild(new ChildAircraft(firstAircraft.Callsign, PairingStatus.Unpassed));

            if (!firstAircraft.IsDesignated)
                firstAircraft.IsDesignated = true;

            var windowInstance = Application.OpenForms.OfType<UI.Window>().FirstOrDefault();
            windowInstance?.CheckAndSetDesignatedAircraft();
            windowInstance?.PopulateAircraftDisplay();
        }
    }
}
