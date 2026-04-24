using System.Windows.Forms;
using DTIWindow.Events;
using vatsys;

namespace DTIWindow.UI
{
    public static class KeyboardSettingsInjector
    {
        private static Form? _injectedWindow;

        public static void HookKeyboardSettingsMenu(Form mainForm)
        {
            var menuStrip = mainForm.Controls.OfType<MenuStrip>().FirstOrDefault();
            if (menuStrip == null) return;

            var menuItem = FindMenuItemByText(menuStrip.Items, "Keyboard");
            if (menuItem == null) return;

            menuItem.Click += (s, e) => mainForm.BeginInvoke(InjectWhenReady);
        }

        private static void InjectWhenReady()
        {
            var kbWindow = Application.OpenForms
                .OfType<Form>()
                .FirstOrDefault(f => f.GetType().Name == "KeyboardSettingsWindow");

            if (kbWindow == null || kbWindow == _injectedWindow) return;

            Inject(kbWindow);
            _injectedWindow = kbWindow;
            kbWindow.FormClosed += (s, e) => _injectedWindow = null;
        }

        private static ToolStripMenuItem? FindMenuItemByText(ToolStripItemCollection items, string text)
        {
            foreach (ToolStripItem item in items)
            {
                if (item is ToolStripMenuItem menuItem)
                {
                    if (menuItem.Text.IndexOf(text, StringComparison.OrdinalIgnoreCase) >= 0)
                        return menuItem;

                    var found = FindMenuItemByText(menuItem.DropDownItems, text);
                    if (found != null) return found;
                }
            }
            return null;
        }

        private static void Inject(Form form)
        {
            var panel = form.Controls.Find("tableLayoutPanel2", false).FirstOrDefault() as TableLayoutPanel;
            if (panel == null) return;

            var headerLabel = new TextLabel
            {
                Text = "Traffic Info",
                AutoSize = true,
                Font = new Font("Terminus (TTF)", 16f, FontStyle.Bold, GraphicsUnit.Pixel),
                ForeColor = SystemColors.ControlDark,
                HasBorder = false,
                InteractiveText = false,
                Anchor = AnchorStyles.Left,
                Margin = new Padding(0, 10, 0, 3),
                TextAlign = ContentAlignment.MiddleCenter
            };

            var keyLabel = new TextLabel
            {
                Text = "Create Pair",
                AutoSize = true,
                Font = new Font("Terminus (TTF)", 16f, FontStyle.Bold, GraphicsUnit.Pixel),
                ForeColor = SystemColors.ControlDark,
                HasBorder = false,
                InteractiveText = true,
                Anchor = AnchorStyles.Left,
                Margin = new Padding(0, 3, 0, 3),
                TextAlign = ContentAlignment.MiddleCenter
            };

            var toggleButton = new PluginToggleButton
            {
                Font = MMI.eurofont_winverysml,
                ForeColor = Colours.GetColour(Colours.Identities.KeybindButtonText),
                Size = new Size(166, 28),
                TextAlign = ContentAlignment.MiddleCenter,
                Text = KeyEventsHelper.GetKeybind().ToString()
            };

            toggleButton.Click += ToggleButton_Click;
            toggleButton.LostFocus += ToggleButton_LostFocus;
            toggleButton.PreviewKeyDown += ToggleButton_PreviewKeyDown;
            toggleButton.KeyUp += ToggleButton_KeyUp;

            int rowIndex = panel.RowCount;
            panel.RowCount += 2;
            panel.RowStyles.Add(new RowStyle());
            panel.RowStyles.Add(new RowStyle());

            panel.Controls.Add(headerLabel, 0, rowIndex);
            panel.Controls.Add(keyLabel, 0, rowIndex + 1);
            panel.Controls.Add(toggleButton, 1, rowIndex + 1);

            const int addedHeight = 64;
            var defaultButton = form.Controls.Find("defaultButton", false).FirstOrDefault();
            if (defaultButton != null)
                defaultButton.Top += addedHeight;

            form.ClientSize = new Size(form.ClientSize.Width, form.ClientSize.Height + addedHeight);
        }

        private static void ToggleButton_Click(object sender, EventArgs e)
        {
            var btn = (PluginToggleButton)sender;
            btn.Checked = !btn.Checked;
            if (btn.Checked)
                Cursor.Clip = btn.RectangleToScreen(btn.ClientRectangle);
            else
            {
                btn.Text = KeyEventsHelper.GetKeybind().ToString();
                Cursor.Clip = Rectangle.Empty;
            }
        }

        private static void ToggleButton_LostFocus(object sender, EventArgs e)
        {
            var btn = (PluginToggleButton)sender;
            btn.Checked = false;
            btn.Text = KeyEventsHelper.GetKeybind().ToString();
            Cursor.Clip = Rectangle.Empty;
        }

        private static void ToggleButton_PreviewKeyDown(object sender, PreviewKeyDownEventArgs e)
        {
            if (((PluginToggleButton)sender).Checked)
                e.IsInputKey = true;
        }

        private static void ToggleButton_KeyUp(object sender, KeyEventArgs e)
        {
            var btn = (PluginToggleButton)sender;
            if (!btn.Checked)
                return;

            e.SuppressKeyPress = true;

            if (e.KeyCode is Keys.Menu or Keys.Alt or Keys.LWin or Keys.RWin)
                return;

            KeyEventsHelper.SetKeybind(e.KeyCode);
            btn.Text = e.KeyCode.ToString();
            btn.Checked = false;
            Cursor.Clip = Rectangle.Empty;
        }

        private sealed class PluginToggleButton : Button
        {
            private bool _checked;

            public bool Checked
            {
                get => _checked;
                set
                {
                    if (_checked == value) return;
                    _checked = value;
                    Invalidate();
                }
            }

            protected override void OnPaint(PaintEventArgs e)
            {
                var bgColor = _checked
                    ? Colours.GetColour(Colours.Identities.KeybindButtonActive)
                    : Colours.GetColour(Colours.Identities.KeybindButtonBackground);

                using var bgBrush = new SolidBrush(bgColor);
                e.Graphics.FillRectangle(bgBrush, ClientRectangle);

                ControlPaint.DrawBorder3D(e.Graphics, ClientRectangle,
                    _checked ? Border3DStyle.Sunken : Border3DStyle.Raised);

                var offset = _checked ? 1 : 0;
                var textRect = new Rectangle(offset, offset, Width, Height);
                TextRenderer.DrawText(e.Graphics, Text, Font, textRect, ForeColor,
                    TextFormatFlags.HorizontalCenter | TextFormatFlags.VerticalCenter);
            }
        }
    }
}
