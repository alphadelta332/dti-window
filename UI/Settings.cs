using System.Reflection;
using System.Windows.Forms;
using DTIWindow.Events;
using vatsys;

namespace DTIWindow.UI
{
    public class Settings : BaseForm
    {
        private readonly TextLabel keybindLabel;
        private readonly BevelButton keybindButton;

        public Settings()
        {
            BackColor = Colours.GetColour(Colours.Identities.WindowBackground);
            Text = "Traffic Info Settings";
            FormBorderStyle = FormBorderStyle.FixedSingle;
            ClientSize = new Size(320, 60);
            StartPosition = FormStartPosition.CenterParent;
            KeyPreview = true;

            try
            {
                typeof(BaseForm)
                    .GetField("middleclickclose", BindingFlags.NonPublic | BindingFlags.Instance)
                    ?.SetValue(this, false);
            }
            catch { }

            keybindLabel = new TextLabel
            {
                Text = "Create Traffic Pairing",
                AutoSize = true,
                Font = new Font("Terminus (TTF)", 16f, FontStyle.Bold, GraphicsUnit.Pixel),
                ForeColor = SystemColors.ControlDark,
                HasBorder = false,
                InteractiveText = true,
                Location = new Point(10, 22),
                TextAlign = ContentAlignment.MiddleCenter
            };

            keybindButton = new BevelButton
            {
                Font = new Font("Terminus (TTF)", 18f, FontStyle.Bold, GraphicsUnit.Pixel),
                Location = new Point(180, 15),
                Size = new Size(130, 28),
                Text = KeyEventsHelper.GetKeybind().ToString()
            };

            keybindButton.Click += KeybindButton_Click;
            keybindButton.LostFocus += KeybindButton_LostFocus;
            keybindButton.PreviewKeyDown += KeybindButton_PreviewKeyDown;
            keybindButton.KeyUp += KeybindButton_KeyUp;

            Controls.Add(keybindLabel);
            Controls.Add(keybindButton);
        }

        private void KeybindButton_Click(object sender, EventArgs e)
        {
            keybindButton.Pressed = !keybindButton.Pressed;
            if (keybindButton.Pressed)
            {
                keybindButton.Text = "...";
                Cursor.Clip = keybindButton.RectangleToScreen(keybindButton.ClientRectangle);
            }
            else
            {
                keybindButton.Text = KeyEventsHelper.GetKeybind().ToString();
                Cursor.Clip = Rectangle.Empty;
            }
        }

        private void KeybindButton_LostFocus(object sender, EventArgs e)
        {
            keybindButton.Pressed = false;
            keybindButton.Text = KeyEventsHelper.GetKeybind().ToString();
            Cursor.Clip = Rectangle.Empty;
        }

        private void KeybindButton_PreviewKeyDown(object sender, PreviewKeyDownEventArgs e)
        {
            if (keybindButton.Pressed)
                e.IsInputKey = true;
        }

        private void KeybindButton_KeyUp(object sender, KeyEventArgs e)
        {
            if (!keybindButton.Pressed)
                return;

            e.SuppressKeyPress = true;

            if (e.KeyCode is Keys.Menu or Keys.Alt or Keys.LWin or Keys.RWin)
                return;

            KeyEventsHelper.SetKeybind(e.KeyCode);
            keybindButton.Text = e.KeyCode.ToString();
            keybindButton.Pressed = false;
            Cursor.Clip = Rectangle.Empty;
        }

        private sealed class BevelButton : Button
        {
            private bool _pressed;

            public bool Pressed
            {
                get => _pressed;
                set { _pressed = value; Invalidate(); }
            }

            protected override void OnPaint(PaintEventArgs e)
            {
                var bgColor = _pressed
                    ? Colours.GetColour(Colours.Identities.KeybindButtonActive)
                    : Colours.GetColour(Colours.Identities.KeybindButtonBackground);

                using var bgBrush = new SolidBrush(bgColor);
                e.Graphics.FillRectangle(bgBrush, ClientRectangle);

                ControlPaint.DrawBorder3D(e.Graphics, ClientRectangle,
                    _pressed ? Border3DStyle.Sunken : Border3DStyle.Raised);

                var offset = _pressed ? 1 : 0;
                var textRect = new Rectangle(offset, offset, Width, Height);
                TextRenderer.DrawText(e.Graphics, Text, Font, textRect, ForeColor,
                    TextFormatFlags.HorizontalCenter | TextFormatFlags.VerticalCenter);
            }
        }
    }
}
