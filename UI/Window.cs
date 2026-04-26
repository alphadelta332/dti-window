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
        private Panel aircraftPanel;
        private BindingList<Aircraft> aircraftList;
        private Font terminusFont = new Font("Terminus (TTF)", 12F, System.Drawing.FontStyle.Regular);

        public Window(BindingList<Aircraft> aircraftList)
        {
            this.aircraftList = aircraftList;
            try
            {
                var field = typeof(BaseForm).GetField("middleclickclose", BindingFlags.NonPublic | BindingFlags.Instance);
                field?.SetValue(this, false);
            }
            catch (Exception) { }

            aircraftList.ListChanged += AircraftList_ListChanged;

            Text = "Traffic Info";
            Width = 300;
            Height = 400;
            BackColor = UIColours.GetColour(UIColours.Identities.WindowBackground);
            Resizeable = true;
            Padding = new Padding(0, 0, 0, 16);

            aircraftPanel = new DoubleBufferedPanel
            {
                Dock = DockStyle.Fill,
                AutoScroll = true
            };
            Controls.Add(aircraftPanel);

            CheckAndSetDesignatedAircraft();
            PopulateAircraftDisplay();
        }

        protected override CreateParams CreateParams
        {
            get
            {
                var cp = base.CreateParams;
                cp.ExStyle |= 0x02000000; // WS_EX_COMPOSITED
                return cp;
            }
        }

        private void AircraftList_ListChanged(object? sender, ListChangedEventArgs e)
        {
            if (InvokeRequired)
            {
                Invoke(new MethodInvoker(() => AircraftList_ListChanged(sender, e)));
                return;
            }

            if (aircraftList.Count == 0)
            {
                aircraftList.ListChanged -= AircraftList_ListChanged;
                Close();
                return;
            }

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
                aircraftPanel.SuspendLayout();
                try
                {
                    ClearAircraftPanel();

                    int yOffset = 10;

                    foreach (var aircraft in aircraftList)
                    {
                        CreateParentAircraftUI(aircraft, ref yOffset);
                        CreateChildAircraftUI(aircraft, ref yOffset);
                        yOffset += 10;
                    }
                }
                finally
                {
                    aircraftPanel.ResumeLayout(true);
                }
            }
            catch (Exception ex)
            {
                ErrorReporter.ThrowError("Traffic Info", ex.Message);
            }
        }

        // Controls.Clear() alone leaks Win32 HWND handles — must Dispose each control first.
        private void ClearAircraftPanel()
        {
            var controls = aircraftPanel.Controls.Cast<Control>().ToList();
            aircraftPanel.Controls.Clear();
            foreach (var control in controls)
                control.Dispose();
        }

        private void CreateParentAircraftUI(Aircraft aircraft, ref int yOffset)
        {
            string hmiState = States.GetHMIState(aircraft.Callsign);
            var (state, color) = Colours.GetHMIStateAndColour(hmiState);

            Label parentLabel = new Label
            {
                Text = aircraft.Callsign,
                Font = terminusFont,
                ForeColor = color,
                Location = new Point(30, yOffset),
                AutoSize = true
            };
            aircraftPanel.Controls.Add(parentLabel);

            Panel boxPanel = CreateDesignationBox(aircraft, parentLabel.Location);
            aircraftPanel.Controls.Add(boxPanel);

            yOffset += 25;
        }

        private Panel CreateDesignationBox(Aircraft aircraft, Point parentLabelLocation)
        {
            Panel boxPanel = new Panel
            {
                Size = new Size(16, 16),
                Location = new Point(parentLabelLocation.X - 20, parentLabelLocation.Y),
                BorderStyle = BorderStyle.None
            };

            bool isMouseDown = false;

            boxPanel.Paint += (s, e) =>
            {
                e.Graphics.SmoothingMode = System.Drawing.Drawing2D.SmoothingMode.AntiAlias;

                if (isMouseDown)
                {
                    using (Brush brush = new SolidBrush(UIColours.GetColour(UIColours.Identities.ChildLabelBackgroundClick)))
                        e.Graphics.FillRectangle(brush, new Rectangle(0, 0, boxPanel.Width, boxPanel.Height));
                }
                else if (aircraft.IsDesignated)
                {
                    using (Brush brush = new SolidBrush(UIColours.GetColour(UIColours.Identities.DesignationBox)))
                        e.Graphics.FillRectangle(brush, new Rectangle(0, 0, boxPanel.Width, boxPanel.Height));
                }
                else
                {
                    using (Brush brush = new SolidBrush(Color.Transparent))
                        e.Graphics.FillRectangle(brush, new Rectangle(0, 0, boxPanel.Width, boxPanel.Height));
                }

                using (Pen pen = new Pen(UIColours.GetColour(UIColours.Identities.DesignationBox), 3))
                    e.Graphics.DrawRectangle(pen, new Rectangle(0, 0, boxPanel.Width - 1, boxPanel.Height - 1));
            };

            boxPanel.MouseDown += (sender, e) => MouseEvents.DesignationBox_MouseDown(sender, e, aircraft, ref isMouseDown, boxPanel);
            boxPanel.MouseUp += (sender, e) => MouseEvents.DesignationBox_MouseUp(sender, e, aircraft, ref isMouseDown, boxPanel);

            return boxPanel;
        }

        private void CreateChildAircraftUI(Aircraft aircraft, ref int yOffset)
        {
            foreach (var child in aircraft.Children)
            {
                Label childLabel = new Label
                {
                    Text = child.Callsign,
                    Font = terminusFont,
                    ForeColor = child.Status == PairingStatus.Passed
                        ? UIColours.GetColour(UIColours.Identities.ChildLabelPassedText)
                        : UIColours.GetColour(UIColours.Identities.ChildLabelUnpassedText),
                    Location = new Point(100, yOffset),
                    AutoSize = true,
                    BackColor = UIColours.GetColour(UIColours.Identities.ChildLabelBackground)
                };

                childLabel.MouseDown += (sender, e) => MouseEvents.ChildLabel_MouseDown(sender, e, aircraft, child);
                childLabel.MouseUp += (sender, e) => MouseEvents.ChildLabel_MouseUp(sender, e, aircraft, child);

                aircraftPanel.Controls.Add(childLabel);
                yOffset += 20;
            }
        }

        protected override void OnFormClosed(FormClosedEventArgs e)
        {
            aircraftList.ListChanged -= AircraftList_ListChanged;
            base.OnFormClosed(e);
        }

        protected override void Dispose(bool disposing)
        {
            if (disposing)
                terminusFont.Dispose();
            base.Dispose(disposing);
        }

        public void CheckAndSetDesignatedAircraft()
        {
            var callsign = MMI.SelectedTrack?.GetPilot()?.Callsign;
            if (string.IsNullOrEmpty(callsign))
                return;

            var designatedAircraft = AircraftManager.AircraftList.FirstOrDefault(a => a.Callsign == callsign);
            if (designatedAircraft == null)
                return;

            foreach (var aircraft in AircraftManager.AircraftList)
                aircraft.IsDesignated = false;

            designatedAircraft.SetDesignatedAircraft(triggeredByDesignateWithWindow: false);
        }
    }

    public class DoubleBufferedPanel : Panel
    {
        public DoubleBufferedPanel()
        {
            DoubleBuffered = true;
            SetStyle(ControlStyles.AllPaintingInWmPaint | ControlStyles.UserPaint | ControlStyles.OptimizedDoubleBuffer, true);
            UpdateStyles();
        }
    }
}
