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
    private Panel aircraftPanel;
    private BindingList<Aircraft> aircraftList;
    private Dictionary<Aircraft, List<Aircraft>> trafficPairings;
    private Font terminusFont;
    private Aircraft? designatedAircraft = null; // To hold the currently designated aircraft

    public AircraftViewer(BindingList<Aircraft> aircraftList, Dictionary<Aircraft, List<Aircraft>> trafficPairings)
    {
        this.aircraftList = aircraftList;
        this.trafficPairings = trafficPairings;

        // Load Terminus font or fallback to Arial
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

        this.aircraftList.ListChanged += AircraftList_ListChanged;

        this.Text = "DTI Window";
        this.Width = 200;
        this.Height = 350;
        this.BackColor = Color.FromArgb(160, 170, 170);

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
        aircraftPanel.Controls.Clear();
        int yOffset = 10;

        string[] staticAircraft = { "QFA123", "VOZ456", "JST789" };

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
            fixedAircraftLabel.MouseDown += (sender, e) => FixedAircraftLabel_MouseDown(sender, e, callsign);

            aircraftPanel.Controls.Add(fixedAircraftLabel);
            yOffset += 25;
        }

        Panel separator = new Panel
        {
            Size = new Size(aircraftPanel.Width, 2),
            Location = new Point(0, yOffset),
            BackColor = Color.Gray
        };
        aircraftPanel.Controls.Add(separator);
        yOffset += 10;

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

    private void FixedAircraftLabel_MouseDown(object? sender, MouseEventArgs e, string callsign)
    {
        if (sender is Label fixedAircraftLabel && e.Button == MouseButtons.Left)
        {
            Aircraft? clickedAircraft = aircraftList.FirstOrDefault(a => a.Callsign == callsign);
            if (clickedAircraft != null)
            {
                designatedAircraft = clickedAircraft;
                PopulateAircraftDisplay();
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

                if (parentAircraft.Children.Count == 0)
                {
                    aircraftList.Remove(parentAircraft);
                }
            }

            PopulateAircraftDisplay();
        }
    }
}
