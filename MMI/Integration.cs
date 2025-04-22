using System;
using System.ComponentModel;
using System.Windows.Forms;
using vatsys;
using vatsys.Plugin;

namespace DTIWindow.MMI
{

    public class DTIWindowPluginClass : Form
    {
        private AircraftViewer? Window; // Reference to the DTI Window form
        private BindingList<Aircraft> AircraftList = new(); // List of all parent aircraft
        private Dictionary<Aircraft, List<Aircraft>> AircraftPairings = new(); // Dictionary of traffic pairings between aircraft
        private CustomToolStripMenuItem _opener; // Declare the _opener field
            public DTIWindowPluginClass()
        {
            // Initialize the menu bar button for the plugin
            _opener = new(CustomToolStripMenuItemWindowType.Main, CustomToolStripMenuItemCategory.Windows, new ToolStripMenuItem("Traffic Info"));
            _opener.Item.Click += OpenForm; // Attach event handler to open the form

            vatsys.MMI.AddCustomMenuItem(_opener); // Add the menu item to the vatSys menu
        }

            // Opens the DTI Window form
        private void OpenForm(object? sender = null, EventArgs? e = null)
        {
            try
            {
                if (Window == null || Window.IsDisposed)
                {
                    // Create a new AircraftViewer window if it doesn't exist or has been closed
                    Window = new AircraftViewer(AircraftList, AircraftPairings);
                }

                Window.Show(Form.ActiveForm); // Show the AircraftViewer window
            }
            catch (Exception)
            {
                // Handle exceptions silently in release mode
            }
        }
    }
}