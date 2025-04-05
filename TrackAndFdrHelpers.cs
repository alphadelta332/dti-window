using System.Collections.Concurrent; // For thread-safe collections
using System.Linq.Expressions;
using System.Reflection;

namespace DtiWindow;

/// <summary>
/// Provides helper methods to interact with internal vatSys track and flight data records (FDR).
/// Utilizes reflection, expression trees, and caching for efficient data retrieval.
/// </summary>
public class TrackAndFdrHelpers : IDisposable
{
    // Caching reflected members
    private static readonly Lazy<FieldInfo> _aircraftTracksField = new(
        () => typeof(vatsys.MMI).GetField("AircraftTracks", BindingFlags.Static | BindingFlags.NonPublic),
        LazyThreadSafetyMode.PublicationOnly);

    private static readonly Lazy<EventInfo> _tracksChangedEvent = new(
        () => typeof(vatsys.MMI).GetEvent("TracksChanged", BindingFlags.Static | BindingFlags.NonPublic),
        LazyThreadSafetyMode.PublicationOnly);

    // Expression tree compiled delegates
    private static readonly Lazy<Func<vatsys.Track, string>> _getHmiStateFunc = new(CompileHmiStateExpression, LazyThreadSafetyMode.PublicationOnly);
    private static readonly Lazy<Func<vatsys.Track, string>> _getFdrStateFunc = new(CompileFdrStateExpression, LazyThreadSafetyMode.PublicationOnly);
    private static readonly Lazy<Func<object, bool>> _tracksChangedArgsValidator = new(CreateTracksChangedArgsValidator, LazyThreadSafetyMode.PublicationOnly);

    // TracksChanged event delegate storage
    private Delegate? _tracksChangedHandler;

    /// <summary>
    /// Initializes a new instance of the <see cref="TrackAndFdrHelpers"/> class.
    /// </summary>
    public TrackAndFdrHelpers()
    {
        SubscribeToTracksChanged();
    }

    /// <summary>
    /// TracksChanged event for external subscribers.
    /// </summary>
    public event EventHandler<EventArgs>? TracksChanged;

    /// <summary>
    /// Retrieves the internal AircraftTracks dictionary using reflection.
    /// </summary>
    /// <returns>A concurrent dictionary containing aircraft track data, or null if unavailable.</returns>
    public static ConcurrentDictionary<object, vatsys.Track> GetAircraftTracks() =>
        _aircraftTracksField.Value?.GetValue(null) as ConcurrentDictionary<object, vatsys.Track> ?? [];

    /// <summary>
    /// Retrieves the HMI state of an aircraft track by callsign.
    /// </summary>
    /// <param name="callsign">The callsign of the aircraft.</param>
    /// <returns>The HMI state as a string, or an error message if retrieval fails.</returns>
    public static string GetHmiState(string callsign)
    {
        try
        {
            var aircraftTracks = GetAircraftTracks();
            if (aircraftTracks == null)
            {
                return "AircraftTracks is null";
            }

            var matchingTrack = aircraftTracks.Values.FirstOrDefault(track => track.GetFDR()?.Callsign == callsign);
            if (matchingTrack == null)
            {
                return "No matching track found";
            }

            return _getHmiStateFunc.Value(matchingTrack);
        }
        catch (Exception ex)
        {
            Console.WriteLine($"Error retrieving HMI state: {ex}");
            return "Error retrieving HMI state";
        }
    }

    /// <summary>
    /// Retrieves the FDR state of an aircraft track by callsign.
    /// </summary>
    /// <param name="callsign">The callsign of the aircraft.</param>
    /// <returns>The FDR state as a string, or an error message if retrieval fails.</returns>
    public static string GetFdrState(string callsign)
    {
        try
        {
            var aircraftTracks = GetAircraftTracks();
            if (aircraftTracks == null)
            {
                return "AircraftTracks is null";
            }

            var matchingTrack = aircraftTracks.Values.FirstOrDefault(track => track.GetFDR()?.Callsign == callsign);
            if (matchingTrack == null)
            {
                return "No matching track found";
            }

            return _getFdrStateFunc.Value(matchingTrack);
        }
        catch (Exception ex)
        {
            Console.WriteLine($"Error retrieving FDR state: {ex}");
            return "Error retrieving FDR state";
        }
    }

    /// <summary>
    /// Get HMI state and color based on the callsign.
    /// </summary>
    /// <param name="callsign">The callsign of the aircraft to get the Hmi state from.</param>
    /// <returns>The hmi state and vatsys color.</returns>
    public static (string hmiState, Color color) GetHmiStateAndColor(string callsign)
    {
        try
        {
            var hmiState = GetHmiState(callsign);
            if (string.IsNullOrEmpty(hmiState) || hmiState == "Unknown State")
            {
                return ("Unknown State", Color.Gray);
            }

            var identity = MapHmiStateToIdentity(hmiState);
            var color = vatsys.Colours.GetColour(identity);

            return (hmiState, color);
        }
        catch (Exception ex)
        {
            Console.WriteLine($"Error in GetHmiStateAndColor: {ex}");
            return ("Error", Color.Red);
        }
    }

    /// <inheritdoc/>
    public void Dispose()
    {
        Dispose(true);
        GC.SuppressFinalize(this);
    }

    /// <summary>
    /// Disposes of the resources used by the TrackAndFdrHelpers class.
    /// </summary>
    /// <param name="isDisposing">If the dispose is being called by the Dispose method or finalizer.</param>
    protected virtual void Dispose(bool isDisposing)
    {
        if (isDisposing)
        {
            UnsubscribeFromTracksChanged();
        }
    }

    /// <summary>
    /// Creates an expression tree to validate if the event args are of type TracksChangedEventArgs.
    /// </summary>
    /// <returns>A compiled delegate to validate the event args.</returns>
    private static Func<object, bool> CreateTracksChangedArgsValidator()
    {
        try
        {
            // Get the type of TracksChangedEventArgs dynamically
            var tracksChangedEventArgsType = typeof(vatsys.MMI).Assembly.GetType("vatsys.TracksChangedEventArgs");
            if (tracksChangedEventArgsType == null)
            {
                return _ => false; // Return a delegate that always returns false if the type isn't found
            }

            // Build the expression tree
            var eventArgsParam = Expression.Parameter(typeof(object), "e");

            // Check if 'e' is an instance of TracksChangedEventArgs
            var isInstanceOfType = Expression.TypeIs(eventArgsParam, tracksChangedEventArgsType);

            // Compile the expression
            return Expression.Lambda<Func<object, bool>>(isInstanceOfType, eventArgsParam).Compile();
        }
        catch (Exception ex)
        {
            Console.WriteLine($"Error creating TracksChangedArgsValidator: {ex}");
            return _ => false; // Return a delegate that always returns false on error
        }
    }

    /// <summary>
    /// Subscribes a handler to the vatSys TracksChanged event.
    /// </summary>
    /// <param name="instance">The instance subscribing to the event.</param>
    /// <param name="handler">The event handler method.</param>
    private static void AddTracksChangedHandler(object instance, Action<object, object> handler)
    {
        try
        {
            var eventHandlerType = _tracksChangedEvent.Value?.EventHandlerType;
            if (eventHandlerType == null)
            {
                return;
            }

            var handlerDelegate = Delegate.CreateDelegate(eventHandlerType, instance, handler.Method);
            _tracksChangedEvent.Value?.AddEventHandler(null, handlerDelegate);
        }
        catch (Exception ex)
        {
            Console.WriteLine($"Error adding TracksChanged handler: {ex}");
        }
    }

    // Remove TracksChanged event handler
    private static void RemoveTracksChangedHandler(object instance, Action<object, object> handler)
    {
        try
        {
            var eventHandlerType = _tracksChangedEvent.Value?.EventHandlerType;
            if (eventHandlerType == null)
            {
                return;
            }

            var handlerDelegate = Delegate.CreateDelegate(eventHandlerType, instance, handler.Method);
            _tracksChangedEvent.Value?.RemoveEventHandler(null, handlerDelegate);
        }
        catch (Exception ex)
        {
            Console.WriteLine($"Error removing TracksChanged handler: {ex}");
        }
    }

    // Map HMI states to Colours.Identities
    private static vatsys.Colours.Identities MapHmiStateToIdentity(string hmiState)
    {
        return hmiState switch
        {
            "Jurisdiction" => vatsys.Colours.Identities.Jurisdiction,
            "HandoverOut" => vatsys.Colours.Identities.Jurisdiction,
            "Announced" => vatsys.Colours.Identities.Announced,
            "HandoverIn" => vatsys.Colours.Identities.Announced,
            "Preactive" => vatsys.Colours.Identities.Preactive,
            "PostJurisdiction" => vatsys.Colours.Identities.PostJurisdiction,
            "NonJurisdiction" => vatsys.Colours.Identities.NonJurisdiction,
            "GhostJurisdiction" => vatsys.Colours.Identities.GhostJurisdiction,
            _ => vatsys.Colours.Identities.Default
        };
    }

    // Compile HMI State expression tree
    private static Func<vatsys.Track, string> CompileHmiStateExpression()
    {
        var trackParam = Expression.Parameter(typeof(vatsys.Track), "track");
        var stateProperty = Expression.Field(trackParam, nameof(vatsys.Track.State));
        var toStringCall = Expression.Call(stateProperty, typeof(object).GetMethod(nameof(object.ToString), Type.EmptyTypes));
        return Expression.Lambda<Func<vatsys.Track, string>>(toStringCall, trackParam).Compile();
    }

    // Compile FDR State expression tree
    private static Func<vatsys.Track, string> CompileFdrStateExpression()
    {
        var trackParam = Expression.Parameter(typeof(vatsys.Track), "track");
        var getFdrMethod = typeof(vatsys.Track).GetMethod(nameof(vatsys.Track.GetFDR));
        var fdrCall = Expression.Call(trackParam, getFdrMethod);
        var fdrStateProperty = Expression.Field(fdrCall, "State");
        var toStringCall = Expression.Call(fdrStateProperty, typeof(object).GetMethod(nameof(object.ToString), Type.EmptyTypes));
        return Expression.Lambda<Func<vatsys.Track, string>>(toStringCall, trackParam).Compile();
    }

    /// <summary>
    /// Subscribes the TracksChanged event to the vatSys internal event using reflection.
    /// </summary>
    private void SubscribeToTracksChanged()
    {
        try
        {
            if (_tracksChangedEvent == null)
            {
                return;
            }

            var eventHandlerType = _tracksChangedEvent.Value.EventHandlerType;
            if (eventHandlerType == null)
            {
                return;
            }

            _tracksChangedHandler = Delegate.CreateDelegate(eventHandlerType, this, nameof(OnTracksChanged));

            _tracksChangedEvent.Value.AddEventHandler(null, _tracksChangedHandler);
        }
        catch (Exception ex)
        {
            Console.WriteLine($"Error subscribing to TracksChanged event: {ex}");
        }
    }

    /// <summary>
    /// Unsubscribes the TracksChanged event from the vatSys internal event using reflection.
    /// </summary>
    private void UnsubscribeFromTracksChanged()
    {
        try
        {
            if (_tracksChangedHandler == null)
            {
                return;
            }

            _tracksChangedEvent.Value?.RemoveEventHandler(null, _tracksChangedHandler);
            _tracksChangedHandler = null;
        }
        catch (Exception ex)
        {
            Console.WriteLine($"Error unsubscribing from TracksChanged event: {ex}");
        }
    }

    /// <summary>
    /// Handles the internal TracksChanged event and raises the public TracksChanged event.
    /// </summary>
    private void OnTracksChanged(object sender, object e)
    {
        try
        {
            // Dynamically check if the event args are of type TracksChangedEventArgs
            var tracksChangedEventArgsType = typeof(vatsys.MMI).Assembly.GetType("vatsys.TracksChangedEventArgs");
            if (tracksChangedEventArgsType?.IsInstanceOfType(e) == true)
            {
                // Raise the external TracksChanged event
                TracksChanged?.Invoke(sender, EventArgs.Empty);
            }
        }
        catch (Exception ex)
        {
            Console.WriteLine($"Error handling TracksChanged event: {ex}");
        }
    }
}
