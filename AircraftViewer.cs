using System;
using System.ComponentModel;
using System.Drawing;
using System.Drawing.Text;
using System.Runtime.Versioning;
using System.Windows.Forms;
using System.IO;
using System.Linq;
using vatsys;

public class AircraftViewer : BaseForm
{
    private Panel aircraftPanel; // UI panel to display aircraft list
    private BindingList<Aircraft> aircraftList; // List of aircraft in the system
    private Dictionary<Aircraft, List<Aircraft>> trafficPairings; // Stores aircraft traffic pairings
    private Font terminusFont = new Font("Terminus (TTF)", 12F, System.Drawing.FontStyle.Regular);
    private Aircraft? designatedAircraft = null; // Currently designated aircraft
    private string? hoveredCallsign = null; // Callsign of aircraft currently hovered over
    private static int nextAircraftNumber = 1; // Used to generate unique aircraft names
    private string? selectedCallsign = null; // Currently selected callsign for the box

    public AircraftViewer(BindingList<Aircraft> aircraftList, Dictionary<Aircraft, List<Aircraft>> trafficPairings)
    {
        this.aircraftList = aircraftList;
        this.trafficPairings = trafficPairings;

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

            // Draw a rectangle around the selected callsign's label
            if (selectedCallsign != null && selectedCallsign == callsign)
            {
                fixedAircraftLabel.Paint += (s, e) =>
                {
                    e.Graphics.SmoothingMode = System.Drawing.Drawing2D.SmoothingMode.AntiAlias;
                    using (Pen pen = new Pen(Color.White, 2))
                    {
                        e.Graphics.DrawRectangle(pen, new Rectangle(0, 0, fixedAircraftLabel.Width - 1, fixedAircraftLabel.Height - 1));
                    }
                };
                fixedAircraftLabel.Invalidate(); // Force the label to repaint
            }

            yOffset += 25; // Move the next label down
        }

        // Add a separator
        Panel separator = new Panel
        {
            Size = new Size(aircraftPanel.Width, 2),
            Location = new Point(0, yOffset),
            BackColor = Color.Gray
        };
        aircraftPanel.Controls.Add(separator);
        yOffset += 10;

        // Display traffic pairings
        foreach (var aircraft in aircraftList)
        {
            Label parentLabel = new Label
            {
                Text = aircraft.Callsign,
                Font = terminusFont,
                ForeColor = Color.FromArgb(200, 255, 200),
                Location = new Point(30, yOffset),
                AutoSize = true
            };
            aircraftPanel.Controls.Add(parentLabel);

            // Panel for the white square to indicate designation status
            Panel boxPanel = new Panel
            {
                Size = new Size(16, 16),
                Location = new Point(parentLabel.Location.X - 20, parentLabel.Location.Y),
                BorderStyle = BorderStyle.None
            };

            boxPanel.Paint += (s, e) =>
            {
                e.Graphics.SmoothingMode = System.Drawing.Drawing2D.SmoothingMode.AntiAlias;
                using (Pen pen = new Pen(Color.White, 4))
                {
                    e.Graphics.DrawRectangle(pen, new Rectangle(0, 0, boxPanel.Width - 1, boxPanel.Height - 1));
                }

                // Fill the box white if the aircraft is designated
                if (designatedAircraft != null && designatedAircraft.Callsign == aircraft.Callsign)
                {
                    using (Brush brush = new SolidBrush(Color.White))
                    {
                        e.Graphics.FillRectangle(brush, new Rectangle(1, 1, boxPanel.Width - 2, boxPanel.Height - 2));
                    }
                }
            };

            aircraftPanel.Controls.Add(boxPanel);

            yOffset += 25;

            foreach (var child in aircraft.Children)
            {
                Label childLabel = new Label
                {
                    Text = child.Callsign,
                    Font = terminusFont,
                    ForeColor = child.Status == "Passed" ? Color.FromArgb(0, 0, 188) : Color.FromArgb(255, 255, 255),
                    Location = new Point(80, yOffset),
                    AutoSize = true
                };

                childLabel.MouseDown += (sender, e) => ChildLabel_MouseDown(sender, e, aircraft, child);

                aircraftPanel.Controls.Add(childLabel);
                yOffset += 20;
            }

            yOffset += 10;
        }
    }

    // Handles clicks on fixed aircraft labels (sets selection for the box)
    private void FixedAircraftLabel_MouseDown(object? sender, MouseEventArgs e, string callsign)
    {
        if (sender is Label fixedAircraftLabel && e.Button == MouseButtons.Left)
        {
            // Set the selected callsign for the box
            selectedCallsign = callsign;
            PopulateAircraftDisplay(); // Refresh UI to reflect the selection
        }
    }

    // Handles keyboard input (e.g., F7 to create traffic pairing)
    private void AircraftViewer_KeyDown(object? sender, KeyEventArgs e)
    {
        // Ensure we have both a designated and hovered aircraft before creating a pairing
        if (e.KeyCode == Keys.F7 && selectedCallsign != null && hoveredCallsign != null)
        {
            // Ensure the hovered aircraft exists in the list
            Aircraft hoveredAircraft = GetOrCreateAircraft(hoveredCallsign);

            // Create the designated aircraft if it doesn't already exist
            designatedAircraft = GetOrCreateAircraft(selectedCallsign);

            // Create a traffic pairing
            CreateTrafficPairing(designatedAircraft, hoveredAircraft);

            // Refresh UI to reflect the pairing
            PopulateAircraftDisplay();
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

    // Handles clicks on child aircraft labels
    private void ChildLabel_MouseDown(object? sender, MouseEventArgs e, Aircraft parent, ChildAircraft child)
    {
        if (e.Button == MouseButtons.Left)
        {
            // Toggle the status between "Passed" and "Unpassed"
            child.Status = child.Status == "Passed" ? "Unpassed" : "Passed";
        }
        else if (e.Button == MouseButtons.Right)
        {
            // Set the status to "Unpassed"
            child.Status = "Unpassed";
        }
        else if (e.Button == MouseButtons.Middle)
        {
            // Remove the child from the parent's children list
            parent.Children.Remove(child);

            // If the parent has no more children, remove the parent from the aircraft list
            if (parent.Children.Count == 0)
            {
                aircraftList.Remove(parent);
            }
        }

        PopulateAircraftDisplay(); // Refresh UI to reflect the change
    }
}
