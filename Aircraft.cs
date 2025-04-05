using System.ComponentModel;

namespace DtiWindow;

/// <summary>
/// Represents a parent aircraft in the system.
/// </summary>
/// <param name="Name">The name of the aircraft.</param>
/// <param name="Callsign">The callsign of the aircraft.</param>
public record Aircraft(string Name, string Callsign)
{
    /// <summary>
    /// Gets a read-only collection of child aircraft.
    /// </summary>
    public BindingList<ChildAircraft> Children { get; } = [];

    /// <summary>
    /// Adds a child aircraft if it doesn't already exist.
    /// </summary>
    /// <param name="child">The child to add.</param>
    public void AddChild(ChildAircraft child) => Children.Add(child);

    /// <summary>
    /// Checks if the aircraft has any child references.
    /// </summary>
    /// <returns>True if there are references, false otherwise.</returns>
    public bool HasReferences() => Children.Count > 0;
}
