using System.Configuration;
using System.Reflection;
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
            if (panel == null)
            {
                Errors.Add(new Exception("Could not inject into KeyboardSettingsWindow — tableLayoutPanel2 not found") { Source = "Traffic Info Plugin" });
                return;
            }

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

            panel.PerformLayout();
            var defaultButton = form.Controls.Find("defaultButton", false).FirstOrDefault();
            if (defaultButton != null)
                defaultButton.Top = panel.Bottom + 8;

            form.ClientSize = new Size(form.ClientSize.Width, panel.Bottom + (defaultButton?.Height ?? 32) + 16);

            HookVatsysToggleButtons(form);
        }

        // vatsys.Properties.Keys is internal, so access it via reflection through the public
        // ApplicationSettingsBase API it inherits from. Cached — the Default instance never changes.
        private static ApplicationSettingsBase? _vatsysKeysSettings;
        private static ApplicationSettingsBase? GetVatsysKeysSettings()
        {
            if (_vatsysKeysSettings != null) return _vatsysKeysSettings;
            try
            {
                var keysType = typeof(MMI).Assembly.GetType("vatsys.Properties.Keys");
                var defaultVal = keysType?
                    .GetProperty("Default", BindingFlags.Static | BindingFlags.Public | BindingFlags.NonPublic)
                    ?.GetValue(null);
                _vatsysKeysSettings = defaultVal as ApplicationSettingsBase;
            }
            catch { }
            return _vatsysKeysSettings;
        }

        // Returns the vatsys property name that already uses the given key, or null if none.
        private static string? FindVatsysKeybindConflict(Keys key)
        {
            var settings = GetVatsysKeysSettings();
            if (settings == null) return null;

            foreach (SettingsProperty prop in settings.Properties)
            {
                try
                {
                    if ((Keys)settings[prop.Name] == key)
                        return prop.Name;
                }
                catch { }
            }
            return null;
        }

        // Direction 2: for each vatsys ToggleButton, intercept KeyDown/KeyUp so that pressing
        // the plugin keybind is rejected and the previous vatsys binding is restored.
        // ToggleButton is internal to vatsys, so we work through Control + reflection.
        private static void HookVatsysToggleButtons(Form form)
        {
            var settings = GetVatsysKeysSettings();
            if (settings == null) return;

            foreach (var btn in GetAllVatsysKeyButtons(form))
            {
                // Only hook buttons whose tag is a vatsys Keys property (not InputMap buttons).
                if (btn.Tag is not string tag || settings.Properties[tag] == null)
                    continue;

                var checkedChangedEvent = btn.GetType().GetEvent("CheckedChanged");
                if (checkedChangedEvent == null) continue;

                bool conflictDetected = false;
                string capturedText = "";
                Keys capturedKey = Keys.None;

                KeyEventHandler keyDownHandler = (s, e) =>
                {
                    if (e.KeyCode == KeyEventsHelper.GetKeybind())
                        conflictDetected = true;
                };

                KeyEventHandler keyUpHandler = (s, e) =>
                {
                    if (!conflictDetected) return;
                    conflictDetected = false;

                    var c = (Control)s;
                    Errors.Add(new Exception("Key command already selected for Traffic Info") { Source = "Keyboard Settings" });

                    // Vatsys already saved the conflicting key and unchecked the button;
                    // revert the stored setting and text, then re-arm for another attempt.
                    settings[(string)c.Tag] = capturedKey;
                    c.Text = capturedText;
                    SetChecked(c, true);
                };

                checkedChangedEvent.AddEventHandler(btn, (EventHandler)((s, e) =>
                {
                    var c = (Control)s;
                    if (GetChecked(c))
                    {
                        capturedText = c.Text;
                        capturedKey = (Keys)settings[(string)c.Tag];
                        conflictDetected = false;
                        c.KeyDown += keyDownHandler;
                        c.KeyUp += keyUpHandler;
                    }
                    else
                    {
                        c.KeyDown -= keyDownHandler;
                        c.KeyUp -= keyUpHandler;
                    }
                }));
            }
        }

        private static bool GetChecked(Control c) =>
            (bool)(c.GetType().GetProperty("Checked")?.GetValue(c) ?? false);

        private static void SetChecked(Control c, bool value) =>
            c.GetType().GetProperty("Checked")?.SetValue(c, value);

        private static IEnumerable<Control> GetAllVatsysKeyButtons(Control parent)
        {
            foreach (Control c in parent.Controls)
            {
                if (c.GetType().Name == "ToggleButton" && c.Tag is string)
                    yield return c;
                foreach (var child in GetAllVatsysKeyButtons(c))
                    yield return child;
            }
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

            // Direction 1: reject if this key is already bound in vatsys.
            var conflict = FindVatsysKeybindConflict(e.KeyCode);
            if (conflict != null)
            {
                Errors.Add(new Exception("Key command already selected for " + conflict) { Source = "Keyboard Settings" });
                return;
            }

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
