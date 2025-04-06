namespace DtiWindow;

/// <summary>
/// Represents a child aircraft in the system.
/// </summary>
/// <param name="Name">The name of the aircraft.</param>
/// <param name="Callsign">The aircraft callsign.</param>
public record ChildAircraft(string Name, string Callsign)
{
    /// <summary>
    /// Initializes a new instance of the <see cref="ChildAircraft"/> class with the specified name, callsign, and status.
    /// </summary>
    /// <param name="name">The name of the child aircraft.</param>
    /// <param name="callsign">The callsign of the child aircraft.</param>
    /// <param name="status">The status of the child aircraft.</param>
    public ChildAircraft(string name, string callsign, AircraftStatus status)
        : this(name, callsign) => Status = status;

    /// <summary>
    /// Gets or sets the status of the child aircraft.
    /// </summary>
    public AircraftStatus Status { get; set; } // Status of the child aircraft (e.g., "Passed", "Unpassed")
}
