using System;
using System.ComponentModel;
using System.Drawing;
using System.Drawing.Text; // Add this for PrivateFontCollection
using System.Runtime.Versioning;
using System.Windows.Forms;
using System.IO;
using System.Linq; // Add for LINQ methods like FirstOrDefault

[SupportedOSPlatform("windows6.1")]
public class AircraftViewer : Form
{
    private Panel aircraftPanel;
    private BindingList<Aircraft> aircraftList;
    private Dictionary<Aircraft, List<Aircraft>> trafficPairings;
    private Font terminusFont;
    private Aircraft? designatedAircraft = null; // To hold the currently designated aircraft

    public AircraftViewer(BindingList<Aircraft> aircraftList, Dictionary<Aircraft, List<Aircraft>> trafficPairings)
    {
        this.aircraftList = aircraftList;
        this.trafficPairings = trafficPairings;

        // Calculate the correct path to the font
        string baseDirectory = AppDomain.CurrentDomain.BaseDirectory;
        string fontPath = Path.Combine(baseDirectory, @"..\..\fonts\Terminus.ttf");

        // Use Path.GetFullPath to resolve the path correctly
        fontPath = Path.GetFullPath(fontPath);

        if (File.Exists(fontPath))
        {
            PrivateFontCollection privateFonts = new PrivateFontCollection();
            privateFonts.AddFontFile(fontPath);
            terminusFont = new Font(privateFonts.Families[0], 12, FontStyle.Regular); // Correct FontStyle usage
        }
        else
        {
            // Fallback in case the font file is not found
            terminusFont = new Font("Arial", 12); // Use 12 as the font size for fallback
            MessageBox.Show($"Terminus font not found. Looking in: {fontPath}\nDefault font 'Arial' will be used.");
        }

        this.aircraftList.ListChanged += AircraftList_ListChanged;

        this.Text = "DTI Window";
        this.Width = 200;
        this.Height = 350; // Slightly increased height for better spacing
        this.BackColor = Color.FromArgb(160, 170, 170); // Set background color to RGB(160, 170, 170)

        aircraftPanel = new Panel
        {
            Dock = DockStyle.Fill,
            AutoScroll = true
        };

        PopulateAircraftDisplay();
        this.Controls.Add(aircraftPanel);
    }

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

    public void PopulateAircraftDisplay()
    {
        aircraftPanel.Controls.Clear(); // Clear previous UI elements
        int yOffset = 10; // Vertical spacing

        // Static aircraft list at the top
        string[] staticAircraft = { "QFA123", "VOZ456", "JST789" };

        foreach (string callsign in staticAircraft)
        {
            Label fixedAircraftLabel = new Label
            {
                Text = callsign, // Display only callsign
                Font = terminusFont, // Apply Terminus font
                ForeColor = Color.FromArgb(200, 255, 200), // Set color for fixed aircraft
                Location = new Point(30, yOffset),
                AutoSize = true
            };
            fixedAircraftLabel.MouseDown += (sender, e) => FixedAircraftLabel_MouseDown(sender, e, callsign);

            // Add border around designated aircraft
            if (designatedAircraft != null && designatedAircraft.Callsign == callsign)
            {
                fixedAircraftLabel.BorderStyle = BorderStyle.FixedSingle;
                fixedAircraftLabel.BackColor = Color.LightBlue; // Highlight designated aircraft with a background color
            }
            else
            {
                fixedAircraftLabel.BackColor = Color.Transparent; // No background for non-designated aircraft
                fixedAircraftLabel.BorderStyle = BorderStyle.None; // No border for non-designated aircraft
            }

            aircraftPanel.Controls.Add(fixedAircraftLabel);
            yOffset += 25; // Space between static aircraft
        }

        // Horizontal line separator
        Panel separator = new Panel
        {
            Size = new Size(aircraftPanel.Width, 2),
            Location = new Point(0, yOffset),
            BackColor = Color.Gray
        };
        aircraftPanel.Controls.Add(separator);

        yOffset += 10; // Adjust yOffset after separator

        foreach (var aircraft in aircraftList)
        {
            // Parent aircraft label with only the callsign displayed
            Label parentLabel = new Label
            {
                Text = aircraft.Callsign, // Display only callsign
                Font = terminusFont, // Apply Terminus font
                ForeColor = Color.FromArgb(200, 255, 200), // Set parent text color to RGB(200, 255, 200)
                Location = new Point(30, yOffset), // Move parent label further right (increase X by 50 to align with box)
                AutoSize = true
            };
            aircraftPanel.Controls.Add(parentLabel);

            // Create and add a Panel as the box next to the parent label
            Panel boxPanel = new Panel
            {
                Size = new Size(14, 14), // Make the box smaller (e.g., 15x15)
                Location = new Point(parentLabel.Location.X - 20, parentLabel.Location.Y), // Move box even further right (increase X by 50)
                BorderStyle = BorderStyle.None
            };

            boxPanel.Paint += (s, e) =>
            {
                e.Graphics.SmoothingMode = System.Drawing.Drawing2D.SmoothingMode.AntiAlias;
                using (Pen pen = new Pen(Color.White, 5)) // Thicker outline (pen width 4)
                {
                    e.Graphics.DrawRectangle(pen, new Rectangle(0, 0, boxPanel.Width - 1, boxPanel.Height - 1));
                }
            };

            // Add the boxPanel to the form
            aircraftPanel.Controls.Add(boxPanel);

            yOffset += 25; // Reduced space between parent and first child

            foreach (var child in aircraft.Children)
            {
                // Child aircraft label with only the callsign displayed
                Label childLabel = new Label
                {
                    Text = child.Callsign, // Display only callsign
                    Font = terminusFont, // Apply Terminus font
                    ForeColor = child.Status == "Passed" ? Color.FromArgb(0, 0, 188) : Color.FromArgb(255, 255, 255), // Set color based on status
                    Location = new Point(65, yOffset), // Doubled indent size for children
                    AutoSize = true
                };

                // Add event handlers for mouse clicks
                childLabel.MouseDown += (sender, e) => ChildLabel_MouseDown(sender, e, aircraft, child);

                aircraftPanel.Controls.Add(childLabel);
                yOffset += 20; // Reduced space between children
            }

            yOffset += 10; // Extra space before the next parent aircraft
        }
    }

    private void FixedAircraftLabel_MouseDown(object? sender, MouseEventArgs e, string callsign)
    {
        if (sender is Label fixedAircraftLabel && e.Button == MouseButtons.Left)
        {
            // Find the aircraft from the list by callsign
            Aircraft? clickedAircraft = aircraftList.FirstOrDefault(a => a.Callsign == callsign);
            if (clickedAircraft != null)
            {
                // If there is a designated aircraft, remove its box
                if (designatedAircraft != null)
                {
                    // Refresh the previous designated aircraft's box to remove the border
                    RefreshAircraftBox(designatedAircraft);
                }

                // Designate the clicked aircraft
                designatedAircraft = clickedAircraft;
                RefreshAircraftBox(designatedAircraft); // Add the box around the designated aircraft
            }
        }
    }

    private void ChildLabel_MouseDown(object? sender, MouseEventArgs e, Aircraft parentAircraft, ChildAircraft child)
    {
        if (sender is Label childLabel)
        {
            if (e.Button == MouseButtons.Left)
            {
                child.Status = "Passed";
            }
            else if (e.Button == MouseButtons.Right)
            {
                child.Status = "Unpassed";
            }
            else if (e.Button == MouseButtons.Middle)
            {
                parentAircraft.Children.Remove(child);

                // Check if the parent has any remaining children. If not, remove the parent from the list
                if (parentAircraft.Children.Count == 0)
                {
                    aircraftList.Remove(parentAircraft);
                }
            }

            // Refresh the UI after status change or deletion
            PopulateAircraftDisplay(); // Refresh UI
        }
    }

    private void RefreshAircraftBox(Aircraft aircraft)
    {
        // This method will refresh the box (border) around the designated aircraft
        PopulateAircraftDisplay(); // Re-populate the UI to reflect the changes
    }
}
