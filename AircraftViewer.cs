using System;
using System.ComponentModel;
using System.Drawing;
using System.Drawing.Text;
using System.Runtime.Versioning;
using System.Windows.Forms;
using System.IO;
using System.Linq;

[SupportedOSPlatform("windows6.1")]
public class AircraftViewer : Form
{
    private Panel aircraftPanel; // UI panel to display aircraft list
    private BindingList<Aircraft> aircraftList; // List of aircraft in the system
    private Dictionary<Aircraft, List<Aircraft>> trafficPairings; // Stores aircraft traffic pairings
    private Font terminusFont; // Font used for UI text
    private Aircraft? designatedAircraft = null; // Currently designated aircraft
    private string? hoveredCallsign = null; // Callsign of aircraft currently hovered over
    private static int nextAircraftNumber = 1; // Used to generate unique aircraft names

    public AircraftViewer(BindingList<Aircraft> aircraftList, Dictionary<Aircraft, List<Aircraft>> trafficPairings)
    {
        this.aircraftList = aircraftList;
        this.trafficPairings = trafficPairings;

        // Load the Terminus font, or use Arial if not found
        string baseDirectory = AppDomain.CurrentDomain.BaseDirectory;
        string fontPath = Path.Combine(baseDirectory, @"..\..\fonts\Terminus.ttf");
        fontPath = Path.GetFullPath(fontPath);

        if (File.Exists(fontPath))
        {
            PrivateFontCollection privateFonts = new PrivateFontCollection();
            privateFonts.AddFontFile(fontPath);
            terminusFont = new Font(privateFonts.Families[0], 12, FontStyle.Regular);
        }
        else
        {
            terminusFont = new Font("Arial", 12);
            MessageBox.Show($"Terminus font not found. Looking in: {fontPath}\nDefault font 'Arial' will be used.");
        }

        // Register event to refresh UI when aircraft list changes
        this.aircraftList.ListChanged += AircraftList_ListChanged;

        // Set form properties
        this.Text = "DTI Window";
        this.Width = 200;
        this.Height = 350;
        this.BackColor = Color.FromArgb(160, 170, 170);

        // Enable keyboard input for the form
        this.KeyPreview = true;
        this.KeyDown += AircraftViewer_KeyDown;

        // Create the main panel for displaying aircraft
        aircraftPanel = new Panel
        {
            Dock = DockStyle.Fill,
            AutoScroll = true
        };

        PopulateAircraftDisplay(); // Populate the aircraft list initially
        this.Controls.Add(aircraftPanel);
    }

    // Refreshes the UI when the aircraft list changes
    private void AircraftList_ListChanged(object? sender, ListChangedEventArgs e)
    {
        if (InvokeRequired)
        {
            Invoke(new MethodInvoker(PopulateAircraftDisplay));
        }
        else
        {
            PopulateAircraftDisplay();
        }
    }

    // Populates the aircraft display with UI elements
    public void PopulateAircraftDisplay()
    {
        // Debug message to indicate when the method is triggered
        Console.WriteLine("PopulateAircraftDisplay triggered!");

        aircraftPanel.Controls.Clear(); // Clear previous elements
        int yOffset = 10; // Y-positioning for elements

        // Define static (designatable) aircraft callsigns
        string[] staticAircraft = { "QFA123", "VOZ456", "JST789" };

        // Create UI labels for each static aircraft
        foreach (string callsign in staticAircraft)
        {
            Label fixedAircraftLabel = new Label
            {
                Text = callsign,
                Font = terminusFont,
                ForeColor = Color.FromArgb(200, 255, 200),
                Location = new Point(30, yOffset),
                AutoSize = true
            };

            // Set event handlers for selection and hovering
            fixedAircraftLabel.MouseDown += (sender, e) => FixedAircraftLabel_MouseDown(sender, e, callsign);
            fixedAircraftLabel.MouseEnter += (sender, e) => hoveredCallsign = callsign;
            fixedAircraftLabel.MouseLeave += (sender, e) => hoveredCallsign = null;

            aircraftPanel.Controls.Add(fixedAircraftLabel);
            yOffset += 25; // Move the next label down
        }
    }

    // Handles clicks on fixed aircraft labels (sets designation)
    private void FixedAircraftLabel_MouseDown(object? sender, MouseEventArgs e, string callsign)
    {
        if (sender is Label fixedAircraftLabel && e.Button == MouseButtons.Left)
        {
            // Get or create the aircraft and set it as designated
            Aircraft designated = GetOrCreateAircraft(callsign);
            designatedAircraft = designated;
            PopulateAircraftDisplay(); // Refresh UI to reflect selection
        }
    }

    // Handles keyboard input (e.g., F7 to create traffic pairing)
    private void AircraftViewer_KeyDown(object? sender, KeyEventArgs e)
    {
        // Ensure we have both a designated and hovered aircraft before creating a pairing
        if (e.KeyCode == Keys.F7 && designatedAircraft != null && hoveredCallsign != null)
        {
            // Ensure the hovered aircraft exists in the list
            Aircraft hoveredAircraft = GetOrCreateAircraft(hoveredCallsign);

            // Create a traffic pairing
            CreateTrafficPairing(designatedAircraft, hoveredAircraft);
        }
    }

    // Ensures an aircraft exists in the list; creates it if needed
    private Aircraft GetOrCreateAircraft(string callsign)
    {
        Aircraft? aircraft = aircraftList.FirstOrDefault(a => a.Callsign == callsign);

        if (aircraft == null)
        {
            // If aircraft doesn't exist, create it
            aircraft = new Aircraft($"Aircraft{nextAircraftNumber++}", callsign);
            aircraftList.Add(aircraft);
            Console.WriteLine($"Aircraft {callsign} created.");
        }
        else
        {
            Console.WriteLine($"Using existing aircraft: {callsign}");
        }
        return aircraft;
    }

    // Creates a traffic pairing between two aircraft
    private void CreateTrafficPairing(Aircraft firstAircraft, Aircraft secondAircraft)
    {
        if (firstAircraft == secondAircraft) return; // Prevent self-pairing

        // Add children to reflect the traffic pairing
        firstAircraft.AddChild(new ChildAircraft("Child", secondAircraft.Callsign, "Unpassed"));
        secondAircraft.AddChild(new ChildAircraft("Child", firstAircraft.Callsign, "Unpassed"));

        // Update traffic pairings dictionary
        if (!trafficPairings.ContainsKey(firstAircraft))
            trafficPairings[firstAircraft] = new List<Aircraft>();
        if (!trafficPairings[firstAircraft].Contains(secondAircraft))
            trafficPairings[firstAircraft].Add(secondAircraft);

        if (!trafficPairings.ContainsKey(secondAircraft))
            trafficPairings[secondAircraft] = new List<Aircraft>();
        if (!trafficPairings[secondAircraft].Contains(firstAircraft))
            trafficPairings[secondAircraft].Add(firstAircraft);

        Console.WriteLine($"Traffic pairing created between {firstAircraft.Callsign} and {secondAircraft.Callsign}");
        PopulateAircraftDisplay(); // Refresh UI to reflect the pairing
    }
}
