using System.ComponentModel;
using System.Reflection;
using DTIWindow.Events;
using DTIWindow.Integration;
using DTIWindow.Models;
using vatsys;
using UIColours = DTIWindow.UI.Colours;

namespace DTIWindow.UI
{
    public class Window : BaseForm
    {
        public Panel aircraftPanel; // UI panel to display the list of aircraft
        private BindingList<Aircraft> aircraftList; // List of aircraft in the system
        private Dictionary<Aircraft, List<Aircraft>> trafficPairings; // Stores traffic pairings between aircraft
        private Font terminusFont = new Font("Terminus (TTF)", 12F, System.Drawing.FontStyle.Regular); // Font for UI labels

        // Constructor for the AircraftViewer form
        public Window(BindingList<Aircraft> aircraftList, Dictionary<Aircraft, List<Aircraft>> trafficPairings)
        {
            this.aircraftList = aircraftList; // Initialise the aircraft list
            this.trafficPairings = trafficPairings; // Initialise the traffic pairings dictionary
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
            aircraftList.ListChanged += AircraftList_ListChanged;

            // Set form properties
            Text = "Traffic Info";
            Width = 200;
            Height = 350;
            BackColor = UIColours.GetColour(UIColours.Identities.WindowBackground);

            // Create the main panel for displaying aircraft
            aircraftPanel = new DoubleBufferedPanel
            {
                Dock = DockStyle.Fill,
                AutoScroll = true
            };

            // Populate the aircraft list initially
            PopulateAircraftDisplay();

            // Add the panel to the form
            Controls.Add(aircraftPanel);

            // Initialise the TracksChanged event subscription
            var eventsInstance = new VatsysEvents();
            eventsInstance.InitialiseTracksChanged();

            // Check and set the designated aircraft after populating the display
            CheckAndSetDesignatedAircraft();
        }

        protected override CreateParams CreateParams
        {
            get
            {
                var cp = base.CreateParams;
                cp.ExStyle |= 0x02000000; // WS_EX_COMPOSITED to enable double buffering
                return cp;
            }
        }

        // Event handler to refresh the UI when the aircraft list changes
        public void AircraftList_ListChanged(object? sender, ListChangedEventArgs e)
        {
            if (InvokeRequired)
            {
                // If called from a different thread, invoke the UI update on the main thread
                Invoke(new MethodInvoker(() => AircraftList_ListChanged(sender, e)));
                return;
            }

            // Close the window if there are no aircraft remaining
            if (aircraftList.Count == 0)
            {
                // Unsubscribe from the ListChanged event
                aircraftList.ListChanged -= AircraftList_ListChanged;

                Close();
                return;
            }

            // Update the UI directly
            PopulateAircraftDisplay();
        }

        public void PopulateAircraftDisplay()
        {
            if (InvokeRequired)
            {
                Invoke(new MethodInvoker(PopulateAircraftDisplay));
                return;
            }

            try
            {
                ClearAircraftPanel(); // Step 1: Clear and reset the UI

                int yOffset = 10; // Y-positioning for UI elements

                foreach (var aircraft in aircraftList)
                {
                    CreateParentAircraftUI(aircraft, ref yOffset); // Step 2: Create parent aircraft UI
                    CreateChildAircraftUI(aircraft, ref yOffset);  // Step 3: Create child aircraft UI
                    yOffset += 10; // Add spacing between parent aircraft
                }
            }
            catch (Exception)
            {
            }
        }

        // Step 1: Clear and reset the UI
        private void ClearAircraftPanel()
        {
            aircraftPanel.Controls.Clear();
        }

        // Step 2: Create parent aircraft UI
        private void CreateParentAircraftUI(Aircraft aircraft, ref int yOffset)
        {
            // Retrieve the HMI state and colour
            string hmiState = States.GetHMIState(aircraft.Callsign);
            var (state, color) = Colours.GetHMIStateAndColour(hmiState);

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
            Panel boxPanel = CreateDesignationBox(aircraft, parentLabel.Location);
            aircraftPanel.Controls.Add(boxPanel);

            yOffset += 25; // Adjust Y-offset for the next element
        }

        // Step 2.1: Create the designation box
        private Panel CreateDesignationBox(Aircraft aircraft, Point parentLabelLocation)
        {
            Panel boxPanel = new Panel
            {
                Size = new Size(16, 16),
                Location = new Point(parentLabelLocation.X - 20, parentLabelLocation.Y),
                BorderStyle = BorderStyle.None
            };

            bool isMouseDown = false;

            // Draw the white outline and the background as two separate layers
            boxPanel.Paint += (s, e) =>
            {
                e.Graphics.SmoothingMode = System.Drawing.Drawing2D.SmoothingMode.AntiAlias;

                // Draw the background layer (bottom layer)
                if (isMouseDown)
                {
                    using (Brush brush = new SolidBrush(UIColours.GetColour(UIColours.Identities.ChildLabelBackgroundClick)))
                    {
                        e.Graphics.FillRectangle(brush, new Rectangle(0, 0, boxPanel.Width, boxPanel.Height));
                    }
                }
                else if (aircraft.designatedAircraft != null && aircraft.designatedAircraft.Callsign == aircraft.Callsign)
                {
                    using (Brush brush = new SolidBrush(UIColours.GetColour(UIColours.Identities.DesignationBox)))
                    {
                        e.Graphics.FillRectangle(brush, new Rectangle(0, 0, boxPanel.Width, boxPanel.Height));
                    }
                }
                else
                {
                    using (Brush brush = new SolidBrush(Color.Transparent))
                    {
                        e.Graphics.FillRectangle(brush, new Rectangle(0, 0, boxPanel.Width, boxPanel.Height));
                    }
                }

                // Draw the white outline (top layer)
                using (Pen pen = new Pen(UIColours.GetColour(UIColours.Identities.DesignationBox), 3))
                {
                    e.Graphics.DrawRectangle(pen, new Rectangle(0, 0, boxPanel.Width - 1, boxPanel.Height - 1));
                }
            };

            // Delegate mouse events to MouseEvents
            var mouseEvents = new MouseEvents();
            boxPanel.MouseDown += (sender, e) => mouseEvents.DesignationBox_MouseDown(sender, e, aircraft, ref isMouseDown, boxPanel);
            boxPanel.MouseUp += (sender, e) => mouseEvents.DesignationBox_MouseUp(sender, e, aircraft, ref isMouseDown, boxPanel);

            return boxPanel;
        }

        // Step 3: Create child aircraft UI
        private void CreateChildAircraftUI(Aircraft aircraft, ref int yOffset)
        {
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
                yOffset += 20; // Adjust Y-offset for the next child
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
            designatedAircraft.SetDesignatedAircraft(triggeredByDesignateWithWindow: false);

            // Refresh the UI to reflect the new designation
            PopulateAircraftDisplay();
        }
    }

    public class DoubleBufferedPanel : Panel
    {
        public DoubleBufferedPanel()
        {
            // Enable double buffering
            DoubleBuffered = true;

            // Reduce flickering by optimizing redraw behavior
            SetStyle(ControlStyles.AllPaintingInWmPaint | ControlStyles.UserPaint | ControlStyles.OptimizedDoubleBuffer, true);
            UpdateStyles();
        }
    }
}