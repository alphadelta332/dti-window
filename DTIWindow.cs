using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Linq;
using System.Threading;
using System.Windows.Forms;
using System.Runtime.InteropServices;
using System.Runtime.Versioning;
using vatsys.Plugin;
using vatsys;
using System.ComponentModel.Composition;
using System.Windows.Input;
using System.Windows.Interop;
using System.Diagnostics;

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
        if (!Children.Any(c => c.Callsign == child.Callsign))
        {
            Children.Add(child);
        }
    }

    public bool HasReferences()
    {
        return Children.Count > 0;
    }
}

[Export(typeof(IPlugin))]
public class DTIWindow : IPlugin
{
    private static AircraftViewer? aircraftViewer;
    private static int nextAircraftNumber = 1;

    private bool KeybindPressed; // Is the keyboard button currently pressed down?
    private bool ListenersDefined; // Have we created the keybind listeners?

    private Track? PreviousSelectedTrack;

    private AircraftViewer? Window; // DTI Window Form
    
    private readonly CustomToolStripMenuItem _opener; // Button within vatSys menu for plugin

    private BindingList<Aircraft> AircraftList = new();
    private Dictionary<Aircraft, List<Aircraft>> AircraftPairings = new();

    public string Name => "DTI Window";

    public DTIWindow()
    {
        // Initialises menu bar button.
        _opener = new(CustomToolStripMenuItemWindowType.Main, CustomToolStripMenuItemCategory.Windows, new ToolStripMenuItem("DTI Window"));
        _opener.Item.Click += OpenForm;

        MMI.AddCustomMenuItem(_opener);

        MMI.SelectedTrackChanged += TrackSelected;

        // Create our key listeners when the main ASD is created.
        MMI.InvokeOnGUI(() =>
        {
            var mainForm = Application.OpenForms["Mainform"];
            mainForm.KeyUp += KeyUp;
            mainForm.KeyDown += KeyDown;
        });
        
    }

    private void KeyUp(object sender, KeyEventArgs e)
    {
        if (e.KeyCode == Keys.F7)
        {
            KeybindPressed = false;
        }
    }

    private void KeyDown(object sender, KeyEventArgs e)
    {
        if (e.KeyCode == Keys.F7)
        {
            KeybindPressed = true;
        }
    }

    // Called when a strip or radar target is clicked on.
    private void TrackSelected(object sender, EventArgs e)
    {
        var track = MMI.SelectedTrack;

        // If we previously clicked on an aircraft,
        // We are holding F7 and
        // We aren't de-designating an aircraft
        // Pass this to AircraftViewer and re-designate the old aircraft.
        if (PreviousSelectedTrack != null && track != PreviousSelectedTrack && track != null && KeybindPressed)
        {
            MMI.SelectedTrack = PreviousSelectedTrack;
            Debug.Print($"{PreviousSelectedTrack.GetPilot().Callsign} Passed traffic about {track.GetPilot().Callsign}");

            // Ensure form has been created and is visible.
            OpenForm();

            var parentAircraft = Window.GetOrCreateAircraft(PreviousSelectedTrack.GetPilot().Callsign);
            var childAircraft = Window.GetOrCreateAircraft(track.GetPilot().Callsign);

            Window.CreateTrafficPairing(parentAircraft, childAircraft);

            return;
        }

        PreviousSelectedTrack = track;
    }

    private void OpenForm(object? sender = null, EventArgs? e = null)
    {
        if (Window == null || Window.IsDisposed)
        {
            // Create new window if it doesn't exist or has been closed.
            Window = new AircraftViewer(AircraftList, AircraftPairings);
        }

        Window.Show(Form.ActiveForm);
    }

    /*
    [STAThread]
    public static void Main()
    {
        AllocConsole();

        BindingList<Aircraft> aircraftList = new BindingList<Aircraft>();
        Dictionary<Aircraft, List<Aircraft>> trafficPairings = new Dictionary<Aircraft, List<Aircraft>>();

        Thread viewerThread = new Thread(() =>
        {
            Application.EnableVisualStyles();
            Application.SetCompatibleTextRenderingDefault(false);
            aircraftViewer = new AircraftViewer(aircraftList, trafficPairings);
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

        // Add children relationships
        firstAircraft.AddChild(new ChildAircraft("Child", secondAircraft.Callsign, "Unpassed"));
        secondAircraft.AddChild(new ChildAircraft("Child", firstAircraft.Callsign, "Unpassed"));

        // Add to traffic pairings
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

        // Update UI
        aircraftViewer?.Invoke((MethodInvoker)(() => aircraftViewer.PopulateAircraftDisplay()));
    }

    private static Aircraft GetOrCreateAircraft(string callsign, BindingList<Aircraft> aircraftList)
    {
        var existingAircraft = aircraftList.FirstOrDefault(a => a.Callsign.Equals(callsign, StringComparison.OrdinalIgnoreCase));
        if (existingAircraft != null)
        {
            Console.WriteLine($"Found existing aircraft with callsign {callsign}. Using Aircraft: {existingAircraft.Name}");
            return existingAircraft;
        }

        int newId = nextAircraftNumber;
        string newName = $"Aircraft{newId}";
        Console.WriteLine($"Creating new aircraft with ID: {newId}, Name: {newName}, Callsign: {callsign}");

        var newAircraft = new Aircraft(newName, callsign);
        aircraftList.Add(newAircraft);
        nextAircraftNumber++;

        return newAircraft;
    }

    private static void DisplayAircraft(BindingList<Aircraft> aircraftList)
    {
        if (aircraftList.Count == 0)
        {
            Console.WriteLine("No aircraft available.");
        }
        else
        {
            Console.WriteLine("Aircraft List:");
            foreach (var aircraft in aircraftList)
            {
                Console.WriteLine($"Name: {aircraft.Name}, Callsign: {aircraft.Callsign}");
                foreach (var child in aircraft.Children)
                {
                    Console.WriteLine($"  Child Callsign: {child.Callsign}, Status: {child.Status}");
                }
            }
        }
    }

    private static void DisplayTrafficPairings(Dictionary<Aircraft, List<Aircraft>> trafficPairings)
    {
        if (trafficPairings.Count == 0)
        {
            Console.WriteLine("No traffic pairings available.");
        }
        else
        {
            Console.WriteLine("Traffic Pairings:");
            foreach (var pairing in trafficPairings)
            {
                Console.WriteLine($"{pairing.Key.Callsign} has pairings with:");
                foreach (var aircraft in pairing.Value)
                {
                    Console.WriteLine($"  - {aircraft.Callsign}");
                }
            }
        }
    }
    */

    // Required for plugin to function.
    void IPlugin.OnFDRUpdate(FDP2.FDR updated)
    {
        return;
    }

    void IPlugin.OnRadarTrackUpdate(RDP.RadarTrack updated)
    {
        return;
    }
}
