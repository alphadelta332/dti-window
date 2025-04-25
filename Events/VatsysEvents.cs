using System.Diagnostics;
using System.Reflection;
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

                // Check if the selected track corresponds to a parent aircraft
                if (track != null)
                {
                    var parentAircraft = track != null 
                        ? AircraftManager.AircraftList.FirstOrDefault(a => a.Callsign == track.GetPilot().Callsign) 
                        : null;
                    if (parentAircraft != null)
                    {
                        // Update the designated aircraft in the AircraftViewer
                        if (Window != null && !Window.IsDisposed)
                        {
                            parentAircraft?.SetDesignatedAircraft(parentAircraft);
                        }
                    }
                    else
                    {
                        // Clear the designated aircraft if no match is found
                        if (Window != null && !Window.IsDisposed)
                        {
                            parentAircraft?.SetDesignatedAircraft(null);
                        }
                    }
                }
                else
                {
                    // Clear the designated aircraft if no track is selected
                    var parentAircraft = track?.GetPilot() != null 
                        ? AircraftManager.AircraftList.FirstOrDefault(a => a.Callsign == track.GetPilot().Callsign) 
                        : null;
                    if (Window != null && !Window.IsDisposed)
                    {
                        parentAircraft?.SetDesignatedAircraft(null);
                    }
                }

                // Update the previously selected track
                PreviousSelectedTrack = track;
            }
            catch (Exception)
            {
                // Handle exceptions silently in release mode
            }
        }

        // Opens the DTI Window form
        public void OpenForm(object? sender = null, EventArgs? e = null)
        {
            try
            {
                if (Window == null || Window.IsDisposed)
                {
                    // Use the shared AircraftList from AircraftManager
                    var aircraftList = AircraftManager.AircraftList;
                    var trafficPairings = Pairings.GetTrafficPairings();
                    Window = new Window(aircraftList, trafficPairings);
                }

                Window.Show(Form.ActiveForm);
            }
            catch (Exception ex)
            {
                Debug.WriteLine($"Error opening form: {ex.Message}");
            }
        }
        public void Initialize()
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
                MethodInfo onTracksChangedMethod = typeof(Window).GetMethod("OnTracksChanged", BindingFlags.Instance | BindingFlags.NonPublic);
                if (onTracksChangedMethod == null)
                {
                    return;
                }

                // Get the event handler type (EventHandler<TracksChangedEventArgs>)
                Type? eventHandlerType = tracksChangedEvent.EventHandlerType;
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
                // Handle exceptions silently in release mode
            }
        }
        public void OnTracksChanged(object sender, object e)
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
                }

                // Refresh the aircraft display
                var aircraftList = AircraftManager.AircraftList;
                var windowInstance = new Window(aircraftList, new Dictionary<Aircraft, List<Aircraft>>());
                windowInstance.PopulateAircraftDisplay();
            }
            catch (Exception)
            {
                // Handle exceptions silently in release mode
            }
        }
    }
}