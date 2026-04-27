using System.Collections;
using System.Reflection;
using DTIWindow.Integration;
using DTIWindow.Models;
using DTIWindow.UI;
using vatsys;
using UIColours = DTIWindow.UI.Colours;

namespace DTIWindow.Events
{
    public static class MouseEvents
    {
        public static Label? activeChildLabel = null;
        private static bool _keybindHeldAtMouseDown = false;
        private static Aircraft? _pendingPairingAircraft = null;

        public static void ChildLabel_MouseDown(object? sender, MouseEventArgs e, Aircraft parent, ChildAircraft child)
        {
            if (sender is Label childLabel)
            {
                childLabel.BackColor = UIColours.GetColour(UIColours.Identities.ChildLabelBackgroundClick);
                childLabel.ForeColor = UIColours.GetColour(UIColours.Identities.ChildLabelTextClick);
                activeChildLabel = childLabel;
                childLabel.Capture = true;
            }
        }

        public static void ChildLabel_MouseUp(object? sender, MouseEventArgs e, Aircraft parent, ChildAircraft child)
        {
            if (sender is Label childLabel)
            {
                childLabel.Capture = false;
                childLabel.BackColor = UIColours.GetColour(UIColours.Identities.ChildLabelBackground);

                try
                {
                    if (e.Button == MouseButtons.Left)
                        child.Status = PairingStatus.Passed;
                    else if (e.Button == MouseButtons.Right)
                        child.Status = PairingStatus.Unpassed;
                    else if (e.Button == MouseButtons.Middle)
                    {
                        parent.Children.Remove(child);
                        if (parent.Children.Count == 0)
                            AircraftManager.AircraftList.Remove(parent);
                    }

                    Application.OpenForms.OfType<Window>().FirstOrDefault()?.PopulateAircraftDisplay();
                }
                catch (Exception ex)
                {
                    ErrorReporter.ThrowError("Traffic Info", ex.Message);
                }
                finally
                {
                    activeChildLabel = null;
                }
            }
        }

        public static void ClearPendingPairing()
        {
            _pendingPairingAircraft = null;
            _keybindHeldAtMouseDown = false;
        }

        public static void DesignateWithWindow(object? sender, MouseEventArgs e, Aircraft aircraft)
        {
            if (e.Button != MouseButtons.Left) return;

            try
            {
                var selectedTrackProperty = typeof(MMI).GetProperty("SelectedTrack", BindingFlags.Static | BindingFlags.NonPublic | BindingFlags.Public);
                var currentSelectedTrack = selectedTrackProperty?.GetValue(null) as Track;

                if (currentSelectedTrack?.GetPilot()?.Callsign == aircraft.Callsign)
                {
                    _pendingPairingAircraft = null;
                    selectedTrackProperty?.SetValue(null, null);
                    FireSelectedTrackChanged();
                    return;
                }

                if (_keybindHeldAtMouseDown)
                {
                    _keybindHeldAtMouseDown = false;

                    var parentAircraft = _pendingPairingAircraft
                        ?? (currentSelectedTrack?.GetPilot()?.Callsign is string cs
                            ? AircraftManager.Instance.GetOrCreateAircraft(cs)
                            : null);

                    if (parentAircraft != null && parentAircraft.Callsign != aircraft.Callsign)
                    {
                        var childAircraft = AircraftManager.Instance.GetOrCreateAircraft(aircraft.Callsign);
                        Pairings.CreateTrafficPairing(parentAircraft, childAircraft);
                        KeyEvents.ResetKeybindPressed();
                        _pendingPairingAircraft = null;
                        return;
                    }

                    KeyEvents.ResetKeybindPressed();
                    _pendingPairingAircraft = null;
                }

                aircraft.SetDesignatedAircraft(triggeredByDesignateWithWindow: true);

                var aircraftTracksField = typeof(MMI).GetField("AircraftTracks", BindingFlags.Static | BindingFlags.NonPublic);
                var aircraftTracks = aircraftTracksField?.GetValue(null) as IDictionary;
                var track = aircraftTracks?.Values.Cast<Track>().FirstOrDefault(t => t.GetPilot()?.Callsign == aircraft.Callsign);

                if (track != null)
                {
                    selectedTrackProperty?.SetValue(null, track);
                    _pendingPairingAircraft = aircraft;
                    FireSelectedTrackChanged();
                }
            }
            catch (Exception ex)
            {
                ErrorReporter.ThrowError("Traffic Info", ex.Message);
            }
        }

        public static void DesignationBox_MouseDown(object? sender, MouseEventArgs e, Aircraft aircraft, ref bool isMouseDown, Panel boxPanel)
        {
            if (e.Button == MouseButtons.Left)
            {
                isMouseDown = true;
                _keybindHeldAtMouseDown = KeyEvents.KeybindPressed;
                boxPanel.Invalidate();
            }
        }

        public static void DesignationBox_MouseUp(object? sender, MouseEventArgs e, Aircraft aircraft, ref bool isMouseDown, Panel boxPanel)
        {
            if (e.Button == MouseButtons.Left)
            {
                isMouseDown = false;
                boxPanel.Invalidate();
                DesignateWithWindow(sender, e, aircraft);
            }
        }

        private static void FireSelectedTrackChanged()
        {
            var eventField = typeof(MMI).GetField("SelectedTrackChanged", BindingFlags.Static | BindingFlags.NonPublic);
            if (eventField?.GetValue(null) is EventHandler handler)
                handler.Invoke(null, EventArgs.Empty);
        }
    }
}
