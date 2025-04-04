using vatsys;

namespace DtiWindow;

/// <summary>
/// Represents the AircraftViewer form, which displays and manages aircraft and their traffic pairings.
/// </summary>
public class AircraftViewer : BaseForm
{
    private readonly Panel _aircraftPanel;
    private readonly BindingSource _bindingSource = new();
    private readonly Font _terminusFont = new("Terminus (TTF)", 12F, FontStyle.Regular);
    private readonly AircraftManager _aircraftManager;

    /// <summary>
    /// Initializes a new instance of the <see cref="AircraftViewer"/> class.
    /// </summary>
    /// <param name="aircraftManager">The aircraft manager.</param>
    public AircraftViewer(AircraftManager aircraftManager)
    {
        _aircraftManager = aircraftManager;

        _bindingSource.DataSource = _aircraftManager.AircraftList;
        _bindingSource.ListChanged += (_, _) => PopulateAircraftDisplay();
        _aircraftManager.AircraftListChanged += (_, _) => PopulateAircraftDisplay();
        _aircraftManager.TrackFdrHelper.TracksChanged += (_, _) => PopulateAircraftDisplay();

        Text = "Traffic Info";
        Width = 200;
        Height = 350;
        BackColor = Colours.GetColour(Colours.Identities.WindowBackground);

        _aircraftPanel = new Panel { Dock = DockStyle.Fill, AutoScroll = true };
        Controls.Add(_aircraftPanel);

        PopulateAircraftDisplay();
    }

    /// <inheritdoc/>
    protected override void Dispose(bool disposing)
    {
        if (disposing)
        {
            _bindingSource.ListChanged -= (_, _) => PopulateAircraftDisplay();
            _aircraftManager.TrackFdrHelper.TracksChanged -= (_, _) => PopulateAircraftDisplay();
            _terminusFont.Dispose();
            _aircraftPanel.Dispose();
            _bindingSource.Dispose();
        }

        base.Dispose(disposing);
    }

    private void PopulateAircraftDisplay()
    {
        _aircraftPanel.Controls.Clear();
        var yOffset = 10;

        foreach (var aircraft in _aircraftManager.AircraftList)
        {
            var (hmiState, color) = TrackAndFdrHelpers.GetHmiStateAndColor(aircraft.Callsign);
            var parentLabel = new Label
            {
                Text = aircraft.Callsign,
                Font = _terminusFont,
                ForeColor = color,
                Location = new Point(30, yOffset),
                AutoSize = true
            };
            _aircraftPanel.Controls.Add(parentLabel);

            var boxPanel = new Panel
            {
                Size = new Size(16, 16),
                Location = new Point(parentLabel.Location.X - 20, parentLabel.Location.Y),
                BorderStyle = BorderStyle.None
            };

            boxPanel.Paint += (s, e) =>
            {
                e.Graphics.SmoothingMode = System.Drawing.Drawing2D.SmoothingMode.AntiAlias;
                using var pen = new Pen(Color.White, 3);
                e.Graphics.DrawRectangle(pen, new Rectangle(0, 0, boxPanel.Width - 1, boxPanel.Height - 1));

                if (_aircraftManager.GetDesignatedAircraft() == aircraft)
                {
                    using var brush = new SolidBrush(Color.White);
                    e.Graphics.FillRectangle(brush, new Rectangle(1, 1, boxPanel.Width - 2, boxPanel.Height - 2));
                }
            };

            _aircraftPanel.Controls.Add(boxPanel);
            yOffset += 25;

            foreach (var child in aircraft.Children)
            {
                var childLabel = new Label
                {
                    Text = child.Callsign,
                    Font = _terminusFont,
                    ForeColor = child.Status == AircraftStatus.Passed ? Color.FromArgb(0, 0, 188) : Color.White,
                    Location = new Point(100, yOffset),
                    AutoSize = true
                };

                childLabel.MouseDown += (s, e) => HandleChildClick(e, aircraft, child);
                _aircraftPanel.Controls.Add(childLabel);
                yOffset += 20;
            }

            yOffset += 10;
        }
    }

    private void HandleChildClick(MouseEventArgs e, Aircraft parent, ChildAircraft child)
    {
        if (e.Button == MouseButtons.Left)
        {
            child.Status = child.Status == AircraftStatus.Passed ? AircraftStatus.Unpassed : AircraftStatus.Passed;
        }
        else if (e.Button == MouseButtons.Right)
        {
            child.Status = AircraftStatus.Unpassed;
        }
        else if (e.Button == MouseButtons.Middle)
        {
            parent.Children.Remove(child);
            if (parent.Children.Count == 0)
            {
                _aircraftManager.RemoveAircraft(parent);
            }
        }

        PopulateAircraftDisplay();
    }
}

