using System.ComponentModel;
using DTIWindow.Models;
using DTIWindow.Events;
using DTIWindow.Integration;
using vatsys;
using UIColours = DTIWindow.UI.Colours;
using System.Reflection;

namespace DTIWindow.UI
{
    public class Window : BaseForm
    {
        public Panel aircraftPanel; // UI panel to display the list of aircraft
        private BindingList<Aircraft> aircraftList; // List of aircraft in the system
        private Dictionary<Aircraft, List<Aircraft>> trafficPairings; // Stores traffic pairings between aircraft
        private Font terminusFont = new Font("Terminus (TTF)", 12F, System.Drawing.FontStyle.Regular); // Font for UI labels
        private bool middleclickclose = false; // Prevent middle-click from closing the form

        // Constructor for the AircraftViewer form
        public Window(BindingList<Aircraft> aircraftList, Dictionary<Aircraft, List<Aircraft>> trafficPairings)
        {
            this.aircraftList = aircraftList; // Initialize the aircraft list
            this.trafficPairings = trafficPairings; // Initialize the traffic pairings dictionary
            try
            {
                var field = typeof(BaseForm).GetField("middleclickclose", BindingFlags.NonPublic | BindingFlags.Instance);
                if (field != null)
                {
                    field.SetValue(this, false);
                }
            }
            catch (Exception)
            {
            }

            // Register an event to refresh the UI when the aircraft list changes
            this.aircraftList.ListChanged += AircraftList_ListChanged;

            // Set form properties
            this.Text = "Traffic Info";
            this.Width = 200;
            this.Height = 350;
            this.BackColor = UIColours.GetColour(UIColours.Identities.WindowBackground); // Updated

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

            // Initialize the TracksChanged event subscription
            var eventsInstance = new VatsysEvents();
            eventsInstance.Initialize();

            // Check and set the designated aircraft after populating the display
            CheckAndSetDesignatedAircraft();
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
            try
            {
                aircraftPanel.Controls.Clear(); // Clear all previous UI elements
                int yOffset = 10; // Y-positioning for UI elements

                // Display traffic pairings for each aircraft
                foreach (var aircraft in aircraftList)
                {
                    // Retrieve the HMI state for the aircraft's callsign
                    string hmiState = States.GetHMIState(aircraft.Callsign);

                    // Retrieve the HMI state and color
                    var (state, color) = Colours.GetHMIStateAndColor(hmiState);

                    // Create a label for the parent aircraft
                    Label parentLabel = new Label
                    {
                        Text = aircraft.Callsign,
                        Font = terminusFont,
                        ForeColor = color,
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
                        using (Pen pen = new Pen(UIColours.GetColour(UIColours.Identities.DesignationBox), 3))
                        {
                            e.Graphics.DrawRectangle(pen, new Rectangle(0, 0, boxPanel.Width - 1, boxPanel.Height - 1));
                        }

                        if (aircraft.designatedAircraft != null && aircraft.designatedAircraft.Callsign == aircraft.Callsign)
                        {
                            using (Brush brush = new SolidBrush(UIColours.GetColour(UIColours.Identities.DesignationBox)))
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
                            ForeColor = child.Status == "Passed"
                                ? UIColours.GetColour(UIColours.Identities.ChildLabelPassedText)
                                : UIColours.GetColour(UIColours.Identities.ChildLabelUnpassedText),
                            Location = new Point(100, yOffset),
                            AutoSize = true,
                            BackColor = UIColours.GetColour(UIColours.Identities.ChildLabelBackground)
                        };

                        // Set event handlers for mouse actions on child labels
                        var mouseEvents = new MouseEvents();
                        childLabel.MouseDown += (sender, e) =>
                        {
                            mouseEvents.ChildLabel_MouseDown(sender, e, aircraft, child);
                        };
                        childLabel.MouseUp += (sender, e) =>
                        {
                            mouseEvents.ChildLabel_MouseUp(sender, e, aircraft, child);
                        };

                        aircraftPanel.Controls.Add(childLabel);
                        yOffset += 20;
                    }

                    yOffset += 10;
                }
            }
            catch (Exception)
            {
            }
        }

        public void CheckAndSetDesignatedAircraft()
        {
            // Retrieve the designated aircraft callsign from vatSys
            var designatedAircraftCallsign = Tracks.GetDesignatedTrack()?.GetPilot()?.Callsign;

            if (string.IsNullOrEmpty(designatedAircraftCallsign))
            {
                return; // Exit if no valid designated aircraft is found
            }

            // Find the aircraft corresponding to the designated callsign
            var designatedAircraft = AircraftManager.AircraftList.FirstOrDefault(a => a.Callsign == designatedAircraftCallsign);

            if (designatedAircraft == null)
            {
                return; // Exit if no matching aircraft is found
            }

            // Clear the designation for all other aircraft
            foreach (var aircraft in AircraftManager.AircraftList)
            {
                aircraft.designatedAircraft = null;
            }

            // Set the new designated aircraft
            designatedAircraft.SetDesignatedAircraft();

            // Refresh the UI to reflect the new designation
            PopulateAircraftDisplay();
        }
    }
}