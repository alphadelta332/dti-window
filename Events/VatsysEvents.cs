using System.Reflection;
using DTIWindow.Integration;
using DTIWindow.Models;
using DTIWindow.UI;
using vatsys;

namespace DTIWindow.Events
{
    public class VatsysEvents : BaseForm
    {
        public Track? PreviousSelectedTrack; // Stores the previously selected radar track
        private Window? Window; // Reference to the DTI Window form

        // Event handler for when a radar track is selected
        public void TrackSelected(object sender, EventArgs e)
        {
            try
            {
                var track = MMI.SelectedTrack; // Get the currently selected track

                if (PreviousSelectedTrack != null && track != PreviousSelectedTrack && track != null && KeyEvents.KeybindPressed)
                {
                    MMI.SelectedTrack = PreviousSelectedTrack; // Re-select the previous track

                    // Ensure the AircraftViewer form is created and visible
                    OpenForm();

                    if (Window == null)
                    {
                        return;
                    }

                    // Get or create the parent and child aircraft
                    var parentAircraft = AircraftManager.Instance.GetOrCreateAircraft(PreviousSelectedTrack.GetPilot().Callsign);
                    var childAircraft = AircraftManager.Instance.GetOrCreateAircraft(track.GetPilot().Callsign);

                    // Create a traffic pairing between the parent and child aircraft
                    var pairings = new Pairings();
                    pairings.CreateTrafficPairing(parentAircraft, childAircraft);

                    KeyEvents.ResetKeybindPressed(); // Reset KeybindPressed after creating a traffic pairing
                    return;
                }

                // Clear the designation for all aircraft
                foreach (var aircraft in AircraftManager.AircraftList)
                {
                    aircraft.designatedAircraft = null;
                }

                // Trigger the SetDesignatedAircraft method
                foreach (var aircraft in AircraftManager.AircraftList)
                {
                    aircraft.SetDesignatedAircraft(triggeredByDesignateWithWindow: false);
                }

                // Update the previously selected track
                PreviousSelectedTrack = track;

                // Refresh the UI
                var windowInstance = Application.OpenForms
                    .OfType<Window>()
                    .FirstOrDefault();
                windowInstance?.PopulateAircraftDisplay();
            }
            catch (Exception)
            {
            }
        }

        // Opens the DTI Window form
        public void OpenForm(object? sender = null, EventArgs? e = null)
        {
            try
            {
                // Check if the Window instance is null or disposed
                if (Window == null || Window.IsDisposed)
                {
                    // Use the shared AircraftList from AircraftManager
                    var aircraftList = AircraftManager.AircraftList;
                    var trafficPairings = Pairings.GetTrafficPairings();
                    Window = new Window(aircraftList, trafficPairings);

                    // Subscribe to events again if necessary
                    aircraftList.ListChanged += Window.AircraftList_ListChanged;
                }

                // Show the form if it is not already visible
                if (!Window.Visible)
                {
                    Window.Show(ActiveForm);
                }
                else
                {
                    Window.BringToFront(); // Bring the form to the front if it is already visible
                }
            }
            catch (Exception)
            {
            }
        }
        public void InitialiseTracksChanged()
        {
            try
            {
                // Get the TracksChanged event using reflection
                EventInfo tracksChangedEvent = typeof(MMI).GetEvent("TracksChanged", BindingFlags.Static | BindingFlags.NonPublic);
                if (tracksChangedEvent == null)
                {
                    return;
                }

                // Get the backing field for the TracksChanged event
                FieldInfo? eventField = typeof(MMI).GetField("TracksChanged", BindingFlags.Static | BindingFlags.NonPublic);
                if (eventField == null)
                {
                    return;
                }

                // Get the current value of the event (the delegate)
                Delegate? currentDelegate = eventField.GetValue(null) as Delegate;

                // Create a delegate for the OnTracksChanged method
                MethodInfo onTracksChangedMethod = typeof(VatsysEvents).GetMethod(nameof(OnTracksChanged), BindingFlags.Instance | BindingFlags.Public | BindingFlags.NonPublic);
                if (onTracksChangedMethod == null)
                {
                    return;
                }

                // Get the event handler type (EventHandler<TracksChangedEventArgs>)
                Type? eventHandlerType = typeof(EventHandler<>).MakeGenericType(typeof(MMI).Assembly.GetType("vatsys.TracksChangedEventArgs"));
                if (eventHandlerType == null)
                {
                    return;
                }

                // Create a delegate of the correct type for the event handler
                Delegate newDelegate = Delegate.CreateDelegate(eventHandlerType, this, onTracksChangedMethod);

                // Combine the new delegate with the existing delegate
                Delegate? combinedDelegate = Delegate.Combine(currentDelegate, newDelegate);

                // Set the combined delegate back to the event field
                eventField.SetValue(null, combinedDelegate);
            }
            catch (Exception)
            {
            }
        }
        private void OnTracksChanged(object sender, EventArgs e)
        {
            try
            {
                // Dynamically check if the event args are of type TracksChangedEventArgs
                var tracksChangedEventArgsType = typeof(MMI).Assembly.GetType("vatsys.TracksChangedEventArgs");
                if (tracksChangedEventArgsType != null && tracksChangedEventArgsType.IsInstanceOfType(e))
                {
                    // Access the 'Track' property
                    var trackProperty = tracksChangedEventArgsType.GetProperty("Track", BindingFlags.Instance | BindingFlags.NonPublic | BindingFlags.Public);
                    var track = trackProperty?.GetValue(e);

                    // Access the 'Removed' property
                    var removedProperty = tracksChangedEventArgsType.GetProperty("Removed", BindingFlags.Instance | BindingFlags.NonPublic | BindingFlags.Public);
                    bool removed = removedProperty != null && (bool)removedProperty.GetValue(e);

                    // If the track is removed, skip further processing
                    if (removed)
                    {
                        return;
                    }
                }

                // Retrieve the designated aircraft callsign
                var designatedAircraftCallsign = Tracks.GetDesignatedTrack()?.GetPilot()?.Callsign;

                if (string.IsNullOrEmpty(designatedAircraftCallsign))
                {
                    return;
                }

                // Clear the designation for all aircraft
                foreach (var aircraft in AircraftManager.AircraftList)
                {
                    aircraft.designatedAircraft = null;
                }

                // Find the aircraft corresponding to the designated callsign
                var designatedAircraft = AircraftManager.AircraftList.FirstOrDefault(a => a.Callsign == designatedAircraftCallsign);

                if (designatedAircraft == null)
                {
                    return;
                }

                // Set the new designated aircraft
                designatedAircraft.SetDesignatedAircraft(triggeredByDesignateWithWindow: false);

                // Refresh the aircraft display
                var windowInstance = Application.OpenForms
                    .OfType<Window>()
                    .FirstOrDefault();
                windowInstance?.PopulateAircraftDisplay();
            }
            catch (Exception)
            {
            }
        }
        public void ThrowError(string source, string message)
        {
            try
            {
                // Get the Errors type from the vatsys assembly
                Type? errorsType = typeof(MMI).Assembly.GetType("vatsys.Errors");
                if (errorsType == null)
                {
                    return;
                }

                // Get the Add method of the Errors class
                MethodInfo? addMethod = errorsType.GetMethod("Add", BindingFlags.Static | BindingFlags.Public);
                if (addMethod == null)
                {
                    return;
                }

                // Log the method signature for debugging
                foreach (var parameter in addMethod.GetParameters())
                {
                }

                // Create a new Exception with the provided message
                Exception pluginError = new Exception(message);

                // Invoke the Errors.Add method with the correct parameters
                addMethod.Invoke(null, new object[] { pluginError, source });
            }
            catch (Exception)
            {
            }
        }
    }
}