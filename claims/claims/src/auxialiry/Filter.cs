using System.Text.RegularExpressions;

namespace claims.src.auxialiry
{
    public static class Filter
    {
        static readonly Regex rgxName = new Regex("[^a-zA-Z0-9-_]");
        static readonly Regex rgxNameWithSpaces = new Regex("[^a-zA-Z0-9-_ ]");

        public static string filterName(string inputString)
        {
            return rgxName.Replace(inputString, "");
        }
        public static string filterNameWithSpaces(string inputString)
        {
            return rgxNameWithSpaces.Replace(inputString, "");
        }
        public static bool checkForBlockedNames(string inputString)
        {
            if(Settings.blockedNames.Contains(inputString))
            {
                return false;
            }
            return true;
        }
    }
}
