using System.ComponentModel;

namespace DtiWindow;

/// <summary>
/// Represents a parent aircraft in the system.
/// </summary>
public record Aircraft(string Name, string Callsign)
{
    /// <summary>
    /// Gets the list of the child aircraft.
    /// </summary>
    public BindingList<ChildAircraft> Children { get; } = [];

    /// <summary>
    /// Adds a child aircraft to the list if it doesn't already exist.
    /// </summary>
    /// <param name="child">The child to add.</param>
    public void AddChild(ChildAircraft child)
    {
        if (!Children.Any(c => c.Callsign == child.Callsign))
        {
            Children.Add(child);
        }
    }

    /// <summary>
    /// Checks if the aircraft has any child references.
    /// </summary>
    /// <returns>The reference.</returns>
    public bool HasReferences()
    {
        return Children.Count > 0;
    }
}
