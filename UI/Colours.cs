using System.Diagnostics;

namespace DTIWindow.UI
{
    public static class Colours
    {
        public enum Identities
        {
            WindowBackground,
            ChildLabelBackground,
            ChildLabelBackgroundClick,
            ChildLabelTextClick,
            ChildLabelPassedText,
            ChildLabelUnpassedText,
            DesignationBox,
        }

        public static Color GetColour(Identities identity)
        {
            return identity switch
            {
                Identities.WindowBackground => vatsys.Colours.GetColour(vatsys.Colours.Identities.WindowBackground),
                Identities.ChildLabelBackground => Color.Transparent,
                Identities.ChildLabelBackgroundClick => vatsys.Colours.GetColour(vatsys.Colours.Identities.GenericText),
                Identities.ChildLabelTextClick => Color.White,
                Identities.DesignationBox => Color.White,
                Identities.ChildLabelPassedText => Color.FromArgb(0, 0, 188),
                Identities.ChildLabelUnpassedText => Color.White,
                _ => Color.Gray // Default color
            };
        }
        public static (string hmiState, Color color) GetHMIStateAndColor(string hmiState)
        {
            try
            {
                Debug.WriteLine($"GetHMIStateAndColor called with hmiState: {hmiState}");

                if (string.IsNullOrEmpty(hmiState) || hmiState == "Unknown State")
                {
                    Debug.WriteLine("HMI state is null, empty, or 'Unknown State'. Returning default values.");
                    return ("Unknown State", Color.Gray); // Default for unknown states
                }

                // Map HMI state to vatsys.Colours identities
                vatsys.Colours.Identities identity = hmiState switch
                {
                    "Jurisdiction" => vatsys.Colours.Identities.Jurisdiction,
                    "HandoverOut" => vatsys.Colours.Identities.Jurisdiction,
                    "Announced" => vatsys.Colours.Identities.Announced,
                    "HandoverIn" => vatsys.Colours.Identities.Announced,
                    "Preactive" => vatsys.Colours.Identities.Preactive,
                    "PostJurisdiction" => vatsys.Colours.Identities.PostJurisdiction,
                    "NonJurisdiction" => vatsys.Colours.Identities.NonJurisdiction,
                    "GhostJurisdiction" => vatsys.Colours.Identities.GhostJurisdiction,
                    _ => vatsys.Colours.Identities.Default
                };

                Debug.WriteLine($"Mapped HMI state '{hmiState}' to vatsys.Colours identity: {identity}");

                // Get the color from vatsys.Colours
                Color color = vatsys.Colours.GetColour(identity);

                Debug.WriteLine($"Retrieved color for identity '{identity}': {color}");

                return (hmiState, color);
            }
            catch (Exception ex)
            {
                Debug.WriteLine($"Error in GetHMIStateAndColor: {ex.Message}");
                return ("Error", Color.Red); // Default error case
            }
        }
    }
}