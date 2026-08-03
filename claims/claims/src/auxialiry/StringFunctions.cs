using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using claims.src.cityplotsgroups;
using claims.src.delayed.invitations;
using claims.src.part;
using claims.src.part.structure;
using claims.src.part.structure.plots;
using SkiaSharp;
using Vintagestory.API.MathTools;

namespace claims.src.auxialiry
{
    public static class StringFunctions
    {
        public static readonly int maxWidthLine = 1085 - 2;
        public static string replaceUnderscore(string str)
        {
            return str.Replace("_", " ");
        }
        public static string setBold(string str)
        {
            return "<strong>" + str + "</strong>";
        }
        public static string setStringColor(string value, string color)
        {
            return "<font color=" + color + ">" + value + "</font>";
        }
        public static List<string> getNamesOfCitizens(City city)
        {
            List<string> names = new List<string>();
            foreach(var it in city.getCityCitizens())
            {
                names.Add(it.GetPartName());
            }
            return names;
        }
        public static List<string> getNamesOfFriends(PlayerInfo playerInfo)
        {
            List<string> names = new();
            foreach (var it in playerInfo.Friends)
            {
                names.Add(it.GetPartName());
            }
            return names;
        }
        public static List<string> getNamesOfCriminals(City city)
        {
            List<string> names = new List<string>();
            foreach (var it in city.criminals)
            {
                names.Add(it.GetPartName());
            }
            return names;
        }
        public static List<string> GetPartsNames(IEnumerable<Part> li)
        {
            List<string> names = new List<string>();
            foreach (var it in li)
            {
                names.Add(it.GetPartName());
            }
            return names;
        }
        public static List<string> getNamesOfCitiesFromInvitations(string prefix, List<CityPlotsGroupInvitation> li)
        {
            List<string> outList = new();
            outList.Add(prefix);
            foreach(var invite in li)
            {
                outList.Add(invite.Sender.GetPartName());
            }
            return outList;
        }
        public static List<string> getNamesOfAllianciesFromInvitations(string prefix, List<Invitation> li)
        {
            List<string> outList = new List<string>();
            outList.Add(prefix);
            foreach (var invite in li)
            {
                outList.Add(invite.getSender().getNameSender());
            }
            return outList;
        }
        public static int getStringLength(string name)
        {
            using (var paint = new SKPaint())
            {
                paint.Typeface = SKTypeface.FromFamilyName("Times New Roman");

                paint.TextSize = 24f;
                var skBounds = SKRect.Empty;
                return (int)paint.MeasureText(name.AsSpan(), ref skBounds);

            }
        }
        public static string makeFeasibleStringFromNames(List<string> li, char delim)
        {
            StringBuilder stringBuilder = new();
            using (var paint = new SKPaint())
            {
                paint.Typeface = SKTypeface.FromFamilyName("Times New Roman");

                paint.TextSize = 12f;
                var skBounds = SKRect.Empty;
                
                float w = 0;
                int currLine = 0;
                int counter = 0;

                foreach (string str in li)
                {
                    paint.MeasureText(str.AsSpan(), ref skBounds);
                    w = skBounds.Width;
                    if (currLine >= maxWidthLine)
                    {
                        stringBuilder.Append("\n");
                    }
                    stringBuilder.Append(str);
                    if (counter < li.Count - 1 && counter != 0)
                    {
                        stringBuilder.Append(", ");
                    }
                    counter++;
                }
            }
            return stringBuilder.ToString();
        }
        public static string concatGroupsNames(ICollection<CityPlotsGroup> li, char delim)
        {
            StringBuilder resultString = new StringBuilder("");
            foreach (var it in li)
            {
                resultString.Append(it.GetPartName());
                if (!it.Equals(li.Last()))
                {
                    resultString.Append(delim);
                }
            }
            return resultString.ToString();
        }
        public static string concatStringsWithPrefixAndDelim(string prefix, ICollection<string> li, string delim)
        {
            StringBuilder resultString = new();
            resultString.Append(prefix);
            foreach (var it in li)
            {
                resultString.Append(it);
                if (!it.Equals(li.Last()))
                {
                    resultString.Append(delim);
                }
            }
            return resultString.ToString();
        }
        public static List<string> getNamesOfPartsForChat(string prefix, ICollection<Part> li)
        {
            List<string> outList = new List<string>();
            outList.Add(prefix);
            foreach(var part in li)
            {
                outList.Add(part.GetPartName());
            }
            return outList;
        }
        public static string getSummonPoints(City city)
        {
            StringBuilder sb = new();
            int i = 0;
            foreach(var it in city.summonPlots)
            {
                if (it.PlotDesc is not PlotDescSummon desc) continue;
                Vec3i summonPoint = desc.SummonPoint.AsVec3i;
                sb.AppendLine(string.Format("{0}: {1}/{2}/{3}", i, summonPoint.X - claims.capi.World.DefaultSpawnPosition.AsBlockPos.X,
                                     summonPoint.Y - claims.capi.World.DefaultSpawnPosition.AsBlockPos.Y,
                                     summonPoint.Z - claims.capi.World.DefaultSpawnPosition.AsBlockPos.Z));
            }
            return sb.ToString();
        }
        public static string concatStringsWithDelim(ICollection<string> li, char delim)
        {
            StringBuilder resultString = new();
            foreach(var it in li)
            {
                resultString.Append(it);
                if(!it.Equals(li.Last()))
                {
                    resultString.Append(delim);
                }
            }
            return resultString.ToString();
        }
        public static string concatStringsWithDelim(ICollection<PlayerInfo> li, char delim)
        {
            StringBuilder resultString = new StringBuilder();
            foreach (var it in li)
            {
                resultString.Append(it.Guid);
                if (!it.Equals(li.Last()))
                {
                    resultString.Append(delim);
                }
            }
            return resultString.ToString();
        }
        public static string concatStringsWithDelim(ICollection<CityPlotsGroup> li, char delim)
        {
            StringBuilder resultString = new StringBuilder();
            foreach (var it in li)
            {
                resultString.Append(it.Guid);
                if (!it.Equals(li.Last()))
                {
                    resultString.Append(delim);
                }
            }
            return resultString.ToString();
        }
        public static string concatStringsWithDelim(ICollection<Prison> li, char delim)
        {
            StringBuilder resultString = new();
            foreach (var it in li)
            {
                /*if(it == null)
                {
                    continue;
                }*/
                resultString.Append(it.Guid);
                if (!it.Equals(li.Last()))
                {
                    resultString.Append(delim);
                }
            }
            return resultString.ToString();
        }
        public static string concatStringsWithDelim(ICollection<City> li, char delim)
        {
            StringBuilder resultString = new StringBuilder();
            foreach (var it in li)
            {
                resultString.Append(it.Guid);
                if (!it.Equals(li.Last()))
                {
                    resultString.Append(delim);
                }
            }
            return resultString.ToString();
        }
        public static string makeStringPlayersName(ICollection<PlayerInfo> li, char delim)
        {
            StringBuilder resultString = new();
            foreach (var it in li)
            {
                resultString.Append(it.getPartNameReplaceUnder());
                if (!it.Equals(li.Last()))
                {
                    resultString.Append(delim);
                }
            }
            return resultString.ToString();
        }
        public static string makeStringPlayersName(ICollection<PlayerInfo> li, string delim)
        {
            StringBuilder resultString = new StringBuilder();
            foreach (var it in li)
            {
                resultString.Append(it.getPartNameReplaceUnder());
                if (!it.Equals(li.Last()))
                {
                    resultString.Append(delim);
                }
            }
            return resultString.ToString();
        }
        public static string concatStringsWithDelim(ICollection<Alliance> li, char delim)
        {
            StringBuilder resultString = new StringBuilder();
            foreach (var it in li)
            {
                resultString.Append(it.Guid);
                if (!it.Equals(li.Last()))
                {
                    resultString.Append(delim);
                }
            }
            return resultString.ToString();
        }
        public static string getNthPageOf(List<Invitation>li, int pageNumber, int pageSize = 4)
        {
            StringBuilder resultString = new StringBuilder();
            if(pageNumber > li.Count / pageSize + 1)
            {
                return "";
            }
            for(int i = 0; i < pageSize; i++)
            {
                if (i + pageSize * (pageNumber - 1) >= li.Count)
                    break;
                resultString.Append(string.Join("", li[i + pageSize * (pageNumber - 1)].getStatus()));
            }
            return resultString.ToString();
        }
        public static List<string> getNamesOfCities(string prefix, ICollection<City> li)
        {
            List<string> result = new();
            result.Add(prefix);
            foreach (var it in li)
            {
                result.Add(it.getPartNameReplaceUnder());
            }
            return result;
        }
        public static List<string> getNamesOfPartReplaceUnder(string prefix, ICollection<Part> li)
        {
            List<string> result = new List<string>
            {
                prefix
            };
            foreach (var it in li)
            {
                result.Add(it.getPartNameReplaceUnder());
            }
            return result;
        }
        public static bool getBoolFromString(string str)
        {
            if (str.Equals("on"))
            {
                return true;
            }
            else
            {
                return false;
            }
        }
    }
}
