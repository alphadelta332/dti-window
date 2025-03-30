using System;
using System.ComponentModel;
using System.Drawing;
using System.Drawing.Text;
using System.Runtime.Versioning;
using System.Windows.Forms;
using System.IO;
using System.Linq;
using vatsys;
using System.Diagnostics; // Add this to use Debug.WriteLine

// Represents the AircraftViewer form, which displays and manages aircraft and their traffic pairings
public class AircraftViewer : BaseForm
{
    private Panel aircraftPanel; // UI panel to display the list of aircraft
    private BindingList<Aircraft> aircraftList; // List of aircraft in the system
    private Dictionary<Aircraft, List<Aircraft>> trafficPairings; // Stores traffic pairings between aircraft
    private Font terminusFont = new Font("Terminus (TTF)", 12F, System.Drawing.FontStyle.Regular); // Font for UI labels
    private Aircraft? designatedAircraft = null; // Currently designated aircraft
    private string? hoveredCallsign = null; // Callsign of the aircraft currently hovered over
    private static int nextAircraftNumber = 1; // Counter for generating unique aircraft names
    private string? selectedCallsign = null; // Currently selected callsign for the box

    // Constructor for the AircraftViewer form
    public AircraftViewer(BindingList<Aircraft> aircraftList, Dictionary<Aircraft, List<Aircraft>> trafficPairings)
    {
        this.aircraftList = aircraftList; // Initialize the aircraft list
        this.trafficPairings = trafficPairings; // Initialize the traffic pairings dictionary

        // Register an event to refresh the UI when the aircraft list changes
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

        // Populate the aircraft list initially
        PopulateAircraftDisplay();

        // Add the panel to the form
        this.Controls.Add(aircraftPanel);
    }

    // Event handler to refresh the UI when the aircraft list changes
    private void AircraftList_ListChanged(object? sender, ListChangedEventArgs e)
    {
        if (InvokeRequired)
        {
            // If called from a different thread, invoke the UI update on the main thread
            Invoke(new MethodInvoker(PopulateAircraftDisplay));
        }
        else
        {
            // Update the UI directly
            PopulateAircraftDisplay();
        }
    }

    // Populates the aircraft display with UI elements
    public void PopulateAircraftDisplay()
    {
        aircraftPanel.Controls.Clear(); // Clear all previous UI elements
        int yOffset = 10; // Y-positioning for UI elements

        // Display traffic pairings for each aircraft
        foreach (var aircraft in aircraftList)
        {
            // Create a label for the parent aircraft
            Label parentLabel = new Label
            {
                Text = aircraft.Callsign,
                Font = terminusFont,
                ForeColor = Color.FromArgb(200, 255, 200),
                Location = new Point(30, yOffset),
                AutoSize = true
            };
            aircraftPanel.Controls.Add(parentLabel);

            // Create a panel to indicate designation status with a white square
            Panel boxPanel = new Panel
            {
                Size = new Size(16, 16),
                Location = new Point(parentLabel.Location.X - 20, parentLabel.Location.Y),
                BorderStyle = BorderStyle.None
            };

            // Draw the white square and fill it if the aircraft is designated
            boxPanel.Paint += (s, e) =>
            {
                e.Graphics.SmoothingMode = System.Drawing.Drawing2D.SmoothingMode.AntiAlias;
                using (Pen pen = new Pen(Color.White, 4))
                {
                    e.Graphics.DrawRectangle(pen, new Rectangle(0, 0, boxPanel.Width - 1, boxPanel.Height - 1));
                }

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

            // Display child aircraft under the parent
            foreach (var child in aircraft.Children)
            {
                Label childLabel = new Label
                {
                    Text = child.Callsign,
                    Font = terminusFont,
                    ForeColor = child.Status == "Passed" ? Color.FromArgb(0, 0, 188) : Color.FromArgb(255, 255, 255),
                    Location = new Point(100, yOffset),
                    AutoSize = true
                };

                // Set event handler for mouse actions on child labels
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

            // Refresh the UI to reflect the selection
            PopulateAircraftDisplay();
        }
    }

    // Handles keyboard input (e.g., F7 to create traffic pairing)
    private void AircraftViewer_KeyDown(object? sender, KeyEventArgs e)
    {
        // Ensure both a designated and hovered aircraft exist before creating a pairing
        if (e.KeyCode == Keys.F7 && selectedCallsign != null && hoveredCallsign != null)
        {
            // Get or create the hovered aircraft
            Aircraft hoveredAircraft = GetOrCreateAircraft(hoveredCallsign);

            // Get or create the designated aircraft
            designatedAircraft = GetOrCreateAircraft(selectedCallsign);

            // Create a traffic pairing between the two aircraft
            CreateTrafficPairing(designatedAircraft, hoveredAircraft);

            // Refresh the UI to reflect the pairing
            PopulateAircraftDisplay();
        }
    }

    // Ensures an aircraft exists in the list; creates it if needed
    public Aircraft GetOrCreateAircraft(string callsign)
    {
        // Find the aircraft by callsign
        Aircraft? aircraft = aircraftList.FirstOrDefault(a => a.Callsign == callsign);

        if (aircraft == null)
        {
            // If the aircraft doesn't exist, create it
            aircraft = new Aircraft($"Aircraft{nextAircraftNumber++}", callsign);
            aircraftList.Add(aircraft);
            Debug.WriteLine($"Aircraft {callsign} created.");
        }
        else
        {
            Debug.WriteLine($"Using existing aircraft: {callsign}");
        }

        return aircraft;
    }

    // Creates a traffic pairing between two aircraft
    public void CreateTrafficPairing(Aircraft firstAircraft, Aircraft secondAircraft)
    {
        try
        {
            Debug.WriteLine("========== DEBUG START ==========");
            Debug.WriteLine($"CreateTrafficPairing called with: {firstAircraft.Callsign} and {secondAircraft.Callsign}");

            if (firstAircraft == secondAircraft)
            {
                Debug.WriteLine("Attempted to create a pairing with the same aircraft. Aborting.");
                return; // Prevent self-pairing
            }

            // Add children to reflect the traffic pairing
            firstAircraft.AddChild(new ChildAircraft("Child", secondAircraft.Callsign, "Unpassed"));
            secondAircraft.AddChild(new ChildAircraft("Child", firstAircraft.Callsign, "Unpassed"));
            Debug.WriteLine($"Children added: {firstAircraft.Callsign} -> {secondAircraft.Callsign}, {secondAircraft.Callsign} -> {firstAircraft.Callsign}");

            // Update the traffic pairings dictionary
            if (!trafficPairings.ContainsKey(firstAircraft))
            {
                trafficPairings[firstAircraft] = new List<Aircraft>();
                Debug.WriteLine($"Created new traffic pairing list for {firstAircraft.Callsign}");
            }
            if (!trafficPairings[firstAircraft].Contains(secondAircraft))
            {
                trafficPairings[firstAircraft].Add(secondAircraft);
                Debug.WriteLine($"Added {secondAircraft.Callsign} to {firstAircraft.Callsign}'s traffic pairings");
            }

            if (!trafficPairings.ContainsKey(secondAircraft))
            {
                trafficPairings[secondAircraft] = new List<Aircraft>();
                Debug.WriteLine($"Created new traffic pairing list for {secondAircraft.Callsign}");
            }
            if (!trafficPairings[secondAircraft].Contains(firstAircraft))
            {
                trafficPairings[secondAircraft].Add(firstAircraft);
                Debug.WriteLine($"Added {firstAircraft.Callsign} to {secondAircraft.Callsign}'s traffic pairings");
            }

            Debug.WriteLine($"Traffic pairing successfully created between {firstAircraft.Callsign} and {secondAircraft.Callsign}");
        }
        catch (Exception ex)
        {
            Debug.WriteLine("========== EXCEPTION ==========");
            Debug.WriteLine($"An error occurred in CreateTrafficPairing: {ex.Message}");
            Debug.WriteLine(ex.StackTrace);
            Debug.WriteLine("========== END EXCEPTION ==========");
        }
        finally
        {
            Debug.WriteLine("========== DEBUG END ==========");
        }

        // Reset KeybindPressed after successfully creating a traffic pairing
        DTIWindow.ResetKeybindPressed();

        // Refresh the UI to reflect the pairing
        PopulateAircraftDisplay();
    }

    // Handles clicks on child aircraft labels
    private void ChildLabel_MouseDown(object? sender, MouseEventArgs e, Aircraft parent, ChildAircraft child)
    {
        try
        {
            Debug.WriteLine($"MouseDown event triggered. Mouse Button: {e.Button}");

            if (e.Button == MouseButtons.Left)
            {
                // Toggle the status between "Passed" and "Unpassed"
                child.Status = child.Status == "Passed" ? "Unpassed" : "Passed";
                Debug.WriteLine($"Toggled status of child {child.Callsign} to {child.Status}");
            }
            else if (e.Button == MouseButtons.Right)
            {
                // Set the status to "Unpassed"
                child.Status = "Unpassed";
                Debug.WriteLine($"Set status of child {child.Callsign} to Unpassed");
            }
            else if (e.Button == MouseButtons.Middle)
            {
                Debug.WriteLine("Middle mouse button clicked. Attempting to remove child...");

                // Remove the child from the parent's children list
                parent.Children.Remove(child);
                Debug.WriteLine($"Removed child {child.Callsign} from parent {parent.Callsign}");

                // If the parent has no more children, remove the parent from the aircraft list
                if (parent.Children.Count == 0)
                {
                    aircraftList.Remove(parent);
                    Debug.WriteLine($"Removed parent {parent.Callsign} as it has no more children");
                }

                // Check if there are no parents and no children left
                if (aircraftList.Count == 0)
                {
                    Debug.WriteLine("No parents or children remaining in the system.");
                }
            }

            Debug.WriteLine($"Parent has {parent.Children.Count} children after action.");
        }
        catch (Exception ex)
        {
            Debug.WriteLine($"Exception in ChildLabel_MouseDown: {ex.Message}");
            Debug.WriteLine(ex.StackTrace);
        }

        // Refresh the UI to reflect the change
        PopulateAircraftDisplay();
    }

    protected override void OnPreviewClientMouseUp(BaseMouseEventArgs e)
    {
        if (e.Button == MouseButtons.Middle)
        {
            Debug.WriteLine("Middle mouse button clicked. Handling it within the plugin.");

            // Get the mouse position relative to the aircraftPanel
            Point mousePosition = aircraftPanel.PointToClient(Cursor.Position);

            // Iterate through the child controls of the aircraftPanel
            foreach (Control control in aircraftPanel.Controls)
            {
                if (control is Label childLabel && control.Bounds.Contains(mousePosition))
                {
                    Debug.WriteLine($"Label under cursor: {childLabel.Text}");

                    // Find the parent and child objects associated with the label
                    foreach (var parent in aircraftList)
                    {
                        var child = parent.Children.FirstOrDefault(c => c.Callsign == childLabel.Text);
                        if (child != null)
                        {
                            Debug.WriteLine($"Found parent: {parent.Callsign}, child: {child.Callsign}");
                            HandleMiddleClick(parent, child);
                            break;
                        }
                    }

                    // Prevent the default behavior by not calling the base method for middle-click
                    return;
                }
            }

            Debug.WriteLine("No label found under the mouse cursor.");
            return; // Prevent the default behavior if no label is found
        }

        base.OnPreviewClientMouseUp(e); // Call the base method for other mouse buttons
    }

    private void HandleMiddleClick(Aircraft parent, ChildAircraft child)
    {
        Debug.WriteLine("Executing custom middle-click functionality to remove child.");

        // Remove the child from the parent's children list
        parent.Children.Remove(child);
        Debug.WriteLine($"Removed child {child.Callsign} from parent {parent.Callsign}");

        // If the parent has no more children, remove the parent from the aircraft list
        if (parent.Children.Count == 0)
        {
            aircraftList.Remove(parent);
            Debug.WriteLine($"Removed parent {parent.Callsign} as it has no more children");
        }

        // Check if there are no parents and no children left
        if (aircraftList.Count == 0)
        {
            Debug.WriteLine("No parents or children remaining in the system.");
        }

        // Refresh the UI to reflect the change
        PopulateAircraftDisplay();
    }

    public void SetDesignatedAircraft(Aircraft? aircraft)
    {
        designatedAircraft = aircraft;
        Debug.WriteLine($"Designated aircraft set to: {designatedAircraft?.Callsign ?? "None"}");

        // Refresh the UI to reflect the change
        PopulateAircraftDisplay();
    }
}
