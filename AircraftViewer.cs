using System;
using System.ComponentModel;
using System.Drawing;
using System.Runtime.Versioning;
using System.Windows.Forms;

[SupportedOSPlatform("windows6.1")]
public class AircraftViewer : Form
{
    private Panel aircraftPanel;
    private BindingList<Aircraft> aircraftList;

    public AircraftViewer(BindingList<Aircraft> aircraftList)
    {
        this.aircraftList = aircraftList;
        this.aircraftList.ListChanged += AircraftList_ListChanged;

        this.Text = "Aircraft Viewer";
        this.Width = 600;
        this.Height = 500;
        this.BackColor = Color.Gray; // Set background color

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

    // Changed to 'public' to allow access from Program.cs
    public void PopulateAircraftDisplay()
    {
        aircraftPanel.Controls.Clear(); // Clear previous UI elements
        int yOffset = 10; // Vertical spacing

        foreach (var aircraft in aircraftList)
        {
            // Parent aircraft label
            Label parentLabel = new Label
            {
                Text = $"{aircraft.Name} ({aircraft.Callsign})",
                Font = new Font("Arial", 12, FontStyle.Bold),
                ForeColor = Color.White,
                Location = new Point(20, yOffset),
                AutoSize = true
            };
            aircraftPanel.Controls.Add(parentLabel);
            yOffset += 30; // Space between parent and first child

            foreach (var child in aircraft.Children)
            {
                // Child aircraft label
                Label childLabel = new Label
                {
                    Text = $"    {child.Name} ({child.Callsign}) - {child.Status}",
                    Font = new Font("Arial", 10, FontStyle.Regular),
                    ForeColor = Color.LightGray,
                    Location = new Point(40, yOffset),
                    AutoSize = true
                };

                // Add event handlers for mouse clicks
                childLabel.MouseDown += (sender, e) => ChildLabel_MouseDown(sender, e, aircraft, child);

                aircraftPanel.Controls.Add(childLabel);
                yOffset += 25; // Space between children
            }

            yOffset += 10; // Extra space before the next parent aircraft
        }
    }

    // Handle mouse down events on child labels
    private void ChildLabel_MouseDown(object? sender, MouseEventArgs e, Aircraft parentAircraft, ChildAircraft child)
    {
        if (sender is Label childLabel) // Ensure sender is a label
        {
            if (e.Button == MouseButtons.Left) // Left-click
            {
                child.Status = "Passed";
            }
            else if (e.Button == MouseButtons.Right) // Right-click
            {
                child.Status = "Unpassed";
            }
            else if (e.Button == MouseButtons.Middle) // Middle-click
            {
                // Remove the child from the parent's list of children
                parentAircraft.Children.Remove(child);
            }

            // Refresh the UI immediately after status change or deletion
            PopulateAircraftDisplay(); // Refresh UI
        }
    }
}
