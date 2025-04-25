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
                if (string.IsNullOrEmpty(hmiState) || hmiState == "Unknown State")
                {
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

                // Get the color from vatsys.Colours
                Color color = vatsys.Colours.GetColour(identity);

                return (hmiState, color);
            }
            catch (Exception)
            {
                return ("Error", Color.Red); // Default error case
            }
        }
    }
}