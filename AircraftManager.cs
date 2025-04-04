using System.ComponentModel;

namespace DtiWindow;

/// <summary>
/// Manages the aircraft and their pairings.
/// </summary>
public class AircraftManager : IDisposable
{
    private static volatile int _nextAircraftNumber = 1;

    private Aircraft? _designatedAircraft;
    private bool _disposedValue;

    /// <summary>
    /// Event triggered when the aircraft list changes.
    /// </summary>
    public event EventHandler? AircraftListChanged;

    /// <summary>
    /// Event triggered when the designated aircraft changes.
    /// </summary>
    public event EventHandler? DesignatedAircraftChanged;

    /// <summary>
    /// Gets the list of aircraft.
    /// </summary>
    public BindingList<Aircraft> AircraftList { get; } = [];

    /// <summary>
    /// Gets a dictionary containing a pair of aircraft, to a list of other aircraft.
    /// </summary>
    public Dictionary<Aircraft, List<Aircraft>> TrafficPairings { get; } = [];

    /// <summary>
    /// Gets a list of aircraft pairings.
    /// </summary>
    public Dictionary<Aircraft, List<Aircraft>> AircraftPairings { get; } = [];

    /// <summary>
    /// Gets the track and FDR helpers.
    /// </summary>
    public TrackAndFdrHelpers TrackFdrHelper { get; } = new();

    /// <summary>
    /// Gets the list of all child aircraft.
    /// </summary>
    /// <param name="callsign">The callsign to get the aircraft for.</param>
    /// <returns>The reated aircraft instance.</returns>
    public Aircraft GetOrCreateAircraft(string callsign)
    {
        var aircraft = AircraftList.FirstOrDefault(a => a.Callsign == callsign);
        if (aircraft == null)
        {
            var nextId = Interlocked.Increment(ref _nextAircraftNumber);
            aircraft = new Aircraft($"Aircraft{nextId}", callsign);
            AircraftList.Add(aircraft);
            AircraftListChanged?.Invoke(this, EventArgs.Empty);
        }

        return aircraft;
    }

    /// <summary>
    /// Creates a pairing between two aircraft.
    /// </summary>
    /// <param name="parentCallsign">The first aircraft to create a pairing of.</param>
    /// <param name="childCallsign">The second aircraft to create a pairing of.</param>
    public void CreateTrafficPairing(string parentCallsign, string childCallsign)
    {
        if (parentCallsign == childCallsign)
        {
            return;
        }

        var parentAircraft = GetOrCreateAircraft(parentCallsign);
        var childAircraft = GetOrCreateAircraft(childCallsign);

        if (parentAircraft.Children.All(c => c.Callsign != childCallsign))
        {
            parentAircraft.AddChild(new ChildAircraft("Child", childCallsign, AircraftStatus.Unpassed));
        }

        if (childAircraft.Children.All(c => c.Callsign != parentCallsign))
        {
            childAircraft.AddChild(new ChildAircraft("Child", parentCallsign, AircraftStatus.Unpassed));
        }

        // Update pairings dictionary
        if (!AircraftPairings.TryGetValue(parentAircraft, out var parentPairings))
        {
            parentPairings = new List<Aircraft>();
            AircraftPairings[parentAircraft] = parentPairings;
        }

        if (!parentPairings.Contains(childAircraft))
        {
            parentPairings.Add(childAircraft);
        }

        if (!AircraftPairings.TryGetValue(childAircraft, out var childPairings))
        {
            childPairings = new List<Aircraft>();
            AircraftPairings[childAircraft] = childPairings;
        }

        if (!childPairings.Contains(parentAircraft))
        {
            childPairings.Add(parentAircraft);
        }
    }

    /// <summary>
    /// Creates a pairing between two aircraft.
    /// </summary>
    /// <param name="first">The first aircraft to create a pairing of.</param>
    /// <param name="second">The second aircraft to create a pairing of.</param>
    public void CreateTrafficPairing(Aircraft first, Aircraft second)
    {
        if (first == second)
        {
            return;
        }

        first.AddChild(new ChildAircraft("Child", second.Callsign, AircraftStatus.Unpassed));
        second.AddChild(new ChildAircraft("Child", first.Callsign, AircraftStatus.Unpassed));

        if (!TrafficPairings.TryGetValue(first, out var firstValue))
        {
            firstValue = [];
            TrafficPairings[first] = firstValue;
        }

        if (!firstValue.Contains(second))
        {
            firstValue.Add(second);
        }

        if (!TrafficPairings.TryGetValue(second, out var secondValue))
        {
            secondValue = [];
            TrafficPairings[second] = secondValue;
        }

        if (!secondValue.Contains(first))
        {
            secondValue.Add(first);
        }

        AircraftListChanged?.Invoke(this, EventArgs.Empty);
    }

    /// <summary>
    /// Sets the designated aircraft.
    /// </summary>
    /// <param name="callsign">The aircraft callsign.</param>
    public void SetDesignatedAircraft(string? callsign)
    {
        if (string.IsNullOrEmpty(callsign))
        {
            _designatedAircraft = null;
            DesignatedAircraftChanged?.Invoke(this, EventArgs.Empty);
            return;
        }

        _designatedAircraft = AircraftList.FirstOrDefault(a => a.Callsign == callsign);

        if (_designatedAircraft is null)
        {
            Console.WriteLine($"Aircraft with callsign {callsign} not found.");
        }

        DesignatedAircraftChanged?.Invoke(this, EventArgs.Empty);
    }

    /// <summary>
    /// Gets the designated aircraft.
    /// </summary>
    /// <returns>The designated callsign.</returns>
    public Aircraft? GetDesignatedAircraft() => _designatedAircraft;

    /// <summary>
    /// Removes a aircraft from the list and its pairings.
    /// </summary>
    /// <param name="aircraft">The aircraft to remove.</param>
    public void RemoveAircraft(Aircraft aircraft)
    {
        AircraftList.Remove(aircraft);
        TrafficPairings.Remove(aircraft);
        foreach (var pair in TrafficPairings.Values)
        {
            pair.Remove(aircraft);
        }

        AircraftListChanged?.Invoke(this, EventArgs.Empty);
    }

    /// <inheritdoc/>
    public void Dispose()
    {
        // Do not change this code. Put cleanup code in 'Dispose(bool disposing)' method
        Dispose(disposing: true);
        GC.SuppressFinalize(this);
    }

    /// <summary>
    /// Releases unmanaged and managed resources.
    /// </summary>
    /// <param name="disposing">If the Dispose method is calling this method or the false if the finalizer is.</param>
    protected virtual void Dispose(bool disposing)
    {
        if (!_disposedValue)
        {
            if (disposing)
            {
                TrackFdrHelper.Dispose();
            }

            _disposedValue = true;
        }
    }
}
