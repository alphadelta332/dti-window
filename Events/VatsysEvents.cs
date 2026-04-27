using System.Reflection;
using DTIWindow.Integration;
using DTIWindow.Models;
using DTIWindow.UI;
using vatsys;

namespace DTIWindow.Events
{
    public class VatsysEvents
    {
        public Track? PreviousSelectedTrack;
        private Window? _window;

        public void TrackSelected(object sender, EventArgs e)
        {
            try
            {
                var track = MMI.SelectedTrack;

                bool keybindHeld = KeyEvents.KeybindPressed || KeyEventsHelper.IsKeybindPhysicallyHeld();

                if (PreviousSelectedTrack != null && track != PreviousSelectedTrack && track != null && keybindHeld)
                {
                    MMI.SelectedTrack = PreviousSelectedTrack;

                    OpenForm();

                    if (_window == null)
                        return;

                    var parentAircraft = AircraftManager.Instance.GetOrCreateAircraft(PreviousSelectedTrack.GetPilot().Callsign);
                    var childAircraft = AircraftManager.Instance.GetOrCreateAircraft(track.GetPilot().Callsign);

                    Pairings.CreateTrafficPairing(parentAircraft, childAircraft);

                    KeyEvents.ResetKeybindPressed();
                    return;
                }

                MouseEvents.ClearPendingPairing();

                foreach (var aircraft in AircraftManager.AircraftList)
                    aircraft.IsDesignated = false;

                foreach (var aircraft in AircraftManager.AircraftList)
                    aircraft.SetDesignatedAircraft(triggeredByDesignateWithWindow: false);

                PreviousSelectedTrack = track;

                Application.OpenForms.OfType<Window>().FirstOrDefault()?.PopulateAircraftDisplay();
            }
            catch (Exception ex)
            {
                ErrorReporter.ThrowError("Traffic Info", ex.Message);
            }
        }

        public void OpenForm(object? sender = null, EventArgs? e = null)
        {
            try
            {
                if (_window == null || _window.IsDisposed)
                {
                    var aircraftList = AircraftManager.AircraftList;
                    _window = new Window(aircraftList);
                }

                if (!_window.Visible)
                    _window.Show(Form.ActiveForm);
                else
                    _window.BringToFront();
            }
            catch (Exception ex)
            {
                ErrorReporter.ThrowError("Traffic Info", ex.Message);
            }
        }

        public void InitialiseTracksChanged()
        {
            try
            {
                EventInfo tracksChangedEvent = typeof(MMI).GetEvent("TracksChanged", BindingFlags.Static | BindingFlags.NonPublic);
                if (tracksChangedEvent == null)
                    return;

                FieldInfo? eventField = typeof(MMI).GetField("TracksChanged", BindingFlags.Static | BindingFlags.NonPublic);
                if (eventField == null)
                    return;

                Delegate? currentDelegate = eventField.GetValue(null) as Delegate;

                MethodInfo? onTracksChangedMethod = typeof(VatsysEvents).GetMethod(nameof(OnTracksChanged), BindingFlags.Instance | BindingFlags.Public | BindingFlags.NonPublic);
                if (onTracksChangedMethod == null)
                    return;

                Type? eventHandlerType = typeof(EventHandler<>).MakeGenericType(typeof(MMI).Assembly.GetType("vatsys.TracksChangedEventArgs"));
                if (eventHandlerType == null)
                    return;

                Delegate newDelegate = Delegate.CreateDelegate(eventHandlerType, this, onTracksChangedMethod);
                Delegate? combinedDelegate = Delegate.Combine(currentDelegate, newDelegate);
                eventField.SetValue(null, combinedDelegate);
            }
            catch (Exception ex)
            {
                ErrorReporter.ThrowError("Traffic Info", ex.Message);
            }
        }

        private void OnTracksChanged(object sender, EventArgs e)
        {
            try
            {
                var tracksChangedEventArgsType = typeof(MMI).Assembly.GetType("vatsys.TracksChangedEventArgs");
                if (tracksChangedEventArgsType != null && tracksChangedEventArgsType.IsInstanceOfType(e))
                {
                    var removedProperty = tracksChangedEventArgsType.GetProperty("Removed", BindingFlags.Instance | BindingFlags.NonPublic | BindingFlags.Public);
                    bool removed = removedProperty != null && (bool)removedProperty.GetValue(e);
                    if (removed)
                        return;
                }

                var designatedAircraftCallsign = MMI.SelectedTrack?.GetPilot()?.Callsign;
                if (string.IsNullOrEmpty(designatedAircraftCallsign))
                    return;

                foreach (var aircraft in AircraftManager.AircraftList)
                    aircraft.IsDesignated = false;

                var designatedAircraft = AircraftManager.AircraftList.FirstOrDefault(a => a.Callsign == designatedAircraftCallsign);
                if (designatedAircraft == null)
                    return;

                designatedAircraft.SetDesignatedAircraft(triggeredByDesignateWithWindow: false);

                Application.OpenForms.OfType<Window>().FirstOrDefault()?.PopulateAircraftDisplay();
            }
            catch (Exception ex)
            {
                ErrorReporter.ThrowError("Traffic Info", ex.Message);
            }
        }
    }
}
