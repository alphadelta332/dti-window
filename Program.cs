using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Linq;
using System.Threading;
using System.Windows.Forms;
using System.Runtime.InteropServices;

public class ChildAircraft
{
    public string Name { get; set; }
    public string Callsign { get; set; }
    public string Status { get; set; }

    public ChildAircraft(string name, string callsign, string status)
    {
        Name = name ?? throw new ArgumentNullException(nameof(name), "Child aircraft name cannot be null.");
        Callsign = callsign ?? throw new ArgumentNullException(nameof(callsign), "Child aircraft callsign cannot be null.");
        Status = status ?? throw new ArgumentNullException(nameof(status), "Child aircraft status cannot be null.");
    }
}

public class Aircraft
{
    public string Name { get; set; }
    public string Callsign { get; set; }
    public BindingList<ChildAircraft> Children { get; set; }

    public Aircraft(string name, string callsign)
    {
        Name = name ?? throw new ArgumentNullException(nameof(name), "Aircraft name cannot be null.");
        Callsign = callsign ?? throw new ArgumentNullException(nameof(callsign), "Aircraft callsign cannot be null.");
        Children = new BindingList<ChildAircraft>();
    }

    public void AddChild(ChildAircraft child)
    {
        // Check if the child already exists
        if (!Children.Any(c => c.Callsign == child.Callsign))
        {
            Children.Add(child);
        }
    }
}

public class Program
{
    private static AircraftViewer? aircraftViewer;

    [STAThread]
    public static void Main()
    {
        // Attach a console window for debugging
        AllocConsole();

        BindingList<Aircraft> aircraftList = new BindingList<Aircraft>();
        Dictionary<Aircraft, List<Aircraft>> trafficPairings = new Dictionary<Aircraft, List<Aircraft>>();

        Thread viewerThread = new Thread(() =>
        {
            Application.EnableVisualStyles();
            Application.SetCompatibleTextRenderingDefault(false);
            aircraftViewer = new AircraftViewer(aircraftList, trafficPairings);  // Pass trafficPairings to the viewer
            Application.Run(aircraftViewer);
        });

        viewerThread.SetApartmentState(ApartmentState.STA);
        viewerThread.Start();

        RunConsoleApp(aircraftList, trafficPairings);
    }

    [DllImport("kernel32.dll")]
    private static extern bool AllocConsole();

    private static void RunConsoleApp(BindingList<Aircraft> aircraftList, Dictionary<Aircraft, List<Aircraft>> trafficPairings)
    {
        while (true)
        {
            Console.WriteLine("\n1. Create Traffic Pairing");
            Console.WriteLine("2. Display All Aircraft");
            Console.WriteLine("3. View Traffic Pairings");
            Console.WriteLine("4. Exit");
            Console.Write("Choose an option: ");
            string choice = Console.ReadLine() ?? string.Empty;

            if (choice == "1")
            {
                CreateTrafficPairing(aircraftList, trafficPairings);
            }
            else if (choice == "2")
            {
                DisplayAircraft(aircraftList);
            }
            else if (choice == "3")
            {
                DisplayTrafficPairings(trafficPairings);
            }
            else if (choice == "4")
            {
                break;
            }
            else
            {
                Console.WriteLine("Invalid choice. Try again.");
            }
        }
    }

    private static void CreateTrafficPairing(BindingList<Aircraft> aircraftList, Dictionary<Aircraft, List<Aircraft>> trafficPairings)
    {
        Console.Write("Enter First Aircraft Callsign: ");
        string firstCallsign = Console.ReadLine() ?? string.Empty;
        Aircraft firstAircraft = GetOrCreateAircraft(firstCallsign, aircraftList);

        Console.Write("Enter Second Aircraft Callsign: ");
        string secondCallsign = Console.ReadLine() ?? string.Empty;
        Aircraft secondAircraft = GetOrCreateAircraft(secondCallsign, aircraftList);

        // Add each aircraft as a child of the other if not already added
        firstAircraft.AddChild(new ChildAircraft("Child", secondAircraft.Callsign, "Unpassed"));
        secondAircraft.AddChild(new ChildAircraft("Child", firstAircraft.Callsign, "Unpassed"));

        // Update the traffic pairings dictionary, ensuring no duplicates
        if (!trafficPairings.ContainsKey(firstAircraft))
        {
            trafficPairings[firstAircraft] = new List<Aircraft>();
        }

        if (!trafficPairings[firstAircraft].Contains(secondAircraft))
        {
            trafficPairings[firstAircraft].Add(secondAircraft);
        }

        if (!trafficPairings.ContainsKey(secondAircraft))
        {
            trafficPairings[secondAircraft] = new List<Aircraft>();
        }

        if (!trafficPairings[secondAircraft].Contains(firstAircraft))
        {
            trafficPairings[secondAircraft].Add(firstAircraft);
        }

        // Refresh the UI after adding a traffic pairing
        aircraftViewer?.Invoke((MethodInvoker)(() => aircraftViewer.PopulateAircraftDisplay()));
    }

    private static Aircraft GetOrCreateAircraft(string callsign, BindingList<Aircraft> aircraftList)
    {
        // Check if aircraft already exists
        var existingAircraft = aircraftList.FirstOrDefault(a => a.Callsign.Equals(callsign, StringComparison.OrdinalIgnoreCase));
        if (existingAircraft != null)
        {
            return existingAircraft; // Return existing aircraft if found
        }

        // Otherwise, create a new aircraft
        int newId = aircraftList.Count + 1; // Find the next available ID
        string newName = $"Aircraft{newId}";
        Aircraft newAircraft = new Aircraft(newName, callsign);
        aircraftList.Add(newAircraft); // Add to the list of aircraft
        return newAircraft; // Return the new aircraft
    }

    private static void DisplayAircraft(BindingList<Aircraft> aircraftList)
    {
        Console.WriteLine("\nAll Aircraft:");
        foreach (var aircraft in aircraftList)
        {
            Console.WriteLine($"{aircraft.Name} ({aircraft.Callsign})");
            foreach (var child in aircraft.Children)
            {
                Console.WriteLine($"    {child.Name} ({child.Callsign}) - {child.Status}");
            }
        }
    }

    private static void DisplayTrafficPairings(Dictionary<Aircraft, List<Aircraft>> trafficPairings)
    {
        Console.WriteLine("\nTraffic Pairings:");
        if (trafficPairings.Count == 0)
        {
            Console.WriteLine("No pairings exist.");
            return;
        }

        foreach (var pair in trafficPairings)
        {
            Console.WriteLine($"{pair.Key.Callsign} is paired with:");
            foreach (var target in pair.Value)
            {
                Console.WriteLine($"  - {target.Callsign}");
            }
        }
    }
}
