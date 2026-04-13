using System;
using System.Collections.Generic;
using System.Text;
using claims.src.auxialiry;
using claims.src.perms.type;
using Vintagestory.API.Common;

namespace claims.src.perms
{
    public class PermsHandler
    {
        /// <summary>
        /// If pvpFlag is ture - you can attack in the Part
        /// If fireFlag is true - fire can spread
        /// If blastFlag is true - blast are prevented
        /// </summary>
        public bool pvpFlag { get; set; }
        public bool fireFlag { get; set; }
        public bool blastFlag { get; set; } = true;
        public bool[] CitizenPerms { get; set; }
        public bool[] StrangerPerms { get; set; }
        public bool[] AlliancePerms { get; set; }
        public bool[] ComradePerms { get; set; }
        public static Dictionary<PermGroup, string> permGroupToString;

        public static Dictionary<int, string> permTypeToString;

        public static Dictionary<string, int> permTypeStringToInt;

        public PermsHandler()
        {
            CitizenPerms = new bool[3];
            StrangerPerms = new bool[3];
            AlliancePerms = new bool[3];
            ComradePerms = new bool[3];         
        }
        public static void initDicts()
        {
            permGroupToString = new Dictionary<PermGroup, string> {
            { PermGroup.STRANGER, "stranger" },
            { PermGroup.CITIZEN, "citizen"},
            { PermGroup.ALLY, "ally"},
            { PermGroup.COMRADE, "friend"} };

            permTypeToString = new Dictionary<int, string> {
            { 0, "use"},
            { 1, "build" },
            { 2, "attack" }};

            permTypeStringToInt = new Dictionary<string, int> {
            { "use", 0 },
            { "build", 1},
            { "attack", 2 } };
        }
        public static void ClearDicts()
        {
            permGroupToString = null;
            permTypeStringToInt = null;
            permTypeToString = null;
        }
        public string getStringForChat()
        {
            StringBuilder stringBuilder = new StringBuilder();
            stringBuilder.Append("pvp: ").Append(pvpFlag ? "on" : "off").Append("| ");
            stringBuilder.Append("fire: ").Append(fireFlag ? "on" : "off").Append("| ");
            stringBuilder.Append("blast: ").Append(blastFlag ? "off" : "on").Append("\n");
            stringBuilder.Append("use, build, attack").Append("\n");

            stringBuilder.Append("Citizen:");
            for (int i = 0;i< CitizenPerms.Length;i++)
            {
                stringBuilder.Append(CitizenPerms[i] ? "+" : "-");
            }
            stringBuilder.Append("\n");
            stringBuilder.Append("Stranger:");
            for (int i = 0; i < StrangerPerms.Length; i++)
            {
                stringBuilder.Append(StrangerPerms[i] ? "+" : "-");
            }
            stringBuilder.Append("\n");
            stringBuilder.Append("Alliance:");
            for (int i = 0; i < AlliancePerms.Length; i++)
            {
                stringBuilder.Append(AlliancePerms[i] ? "+" : "-");
            }
            stringBuilder.Append("\n");
            stringBuilder.Append("Friend:");
            for (int i = 0; i < ComradePerms.Length; i++)
            {
                stringBuilder.Append(ComradePerms[i] ? "+" : "-");
            }
            return stringBuilder.ToString();
        }
        public void setPerm(PermsHandler copyFrom)
        {
            pvpFlag = copyFrom.pvpFlag;
            fireFlag = copyFrom.fireFlag;
            blastFlag = copyFrom.blastFlag;

            for(int i = 0;i < CitizenPerms.Length;i++)
            {
                CitizenPerms[i] = copyFrom.CitizenPerms[i];
            }
            for (int i = 0; i < StrangerPerms.Length; i++)
            {
                StrangerPerms[i] = copyFrom.StrangerPerms[i];
            }
            for (int i = 0; i < AlliancePerms.Length; i++)
            {
                AlliancePerms[i] = copyFrom.AlliancePerms[i];
            }
            for (int i = 0; i < ComradePerms.Length; i++)
            {
                ComradePerms[i] = copyFrom.ComradePerms[i];
            }
        }
        public void setPerm(PermGroup group, PermType type, bool val)
        {
            switch(group)
            {
                case PermGroup.STRANGER:
                    this.StrangerPerms[(int)type] = val;
                    break;
                case PermGroup.CITIZEN:
                    this.CitizenPerms[(int)type] = val;
                    break;
                case PermGroup.ALLY:
                    this.AlliancePerms[(int)type] = val;
                    break;
                case PermGroup.COMRADE:
                    this.ComradePerms[(int)type] = val;
                    break;
            }
        }
        public void ApplyFromHandler(PermsHandler anotherHandler, PermGroup group)
        {
            var tmp = new bool[3];
            switch (group)
            {               
                case PermGroup.STRANGER:
                    Array.Copy(anotherHandler.StrangerPerms, tmp, 3);
                    this.StrangerPerms = tmp;
                    break;
                case PermGroup.CITIZEN:
                    Array.Copy(anotherHandler.CitizenPerms, tmp, 3);
                    this.CitizenPerms = tmp;
                    break;
                case PermGroup.ALLY:
                    Array.Copy(anotherHandler.AlliancePerms, tmp, 3);
                    this.AlliancePerms = tmp;
                    break;
                case PermGroup.COMRADE:
                    Array.Copy(anotherHandler.ComradePerms, tmp, 3);
                    this.ComradePerms = tmp;
                    break;
            }
        }
        /// <summary>
        /// Set permission for group, get args and expect 3 arguments group, type and new value.
        /// </summary>
        /// <param name="args"></param>
        public bool setAccessPerm(CmdArgs args)
        {
            PermGroup permGroup;
            switch (args[0])
            {
                case "citizen":
                    permGroup = PermGroup.CITIZEN;
                    break;
                case "stranger":
                    permGroup = PermGroup.STRANGER;
                    break;
                case "ally":
                    permGroup = PermGroup.ALLY;
                    break;
                case "friend":
                    permGroup = PermGroup.COMRADE;
                    break;
                default:
                    return false;
            }
            PermType permType;
            switch (args[1])
            {
                case "use":
                    permType = PermType.USE_PERM;
                    break;
                case "build":
                    permType = PermType.BUILD_AND_DESTROY_PERM;
                    break;
                case "attack":
                    permType = PermType.ATTACK_ANIMALS_PERM;
                    break;
                default:
                    return false;
            }

            bool newValue = StringFunctions.getBoolFromString(args[2]);

            switch (permGroup)
            {
                case PermGroup.STRANGER:
                    this.StrangerPerms[(int)permType] = newValue;
                    break;
                case PermGroup.CITIZEN:
                    this.CitizenPerms[(int)permType] = newValue;
                    break;
                case PermGroup.ALLY:
                    this.AlliancePerms[(int)permType] = newValue;
                    break;
                case PermGroup.COMRADE:
                    this.ComradePerms[(int)permType] = newValue;
                    break;
            }
            return true;
        }
        public bool setAccessPerm(string permGroupStr, string permTypeStr, string newValueStr)
        {
            PermGroup permGroup;
            switch (permGroupStr)
            {
                case "citizen":
                    permGroup = PermGroup.CITIZEN;
                    break;
                case "stranger":
                    permGroup = PermGroup.STRANGER;
                    break;
                case "ally":
                    permGroup = PermGroup.ALLY;
                    break;
                case "friend":
                    permGroup = PermGroup.COMRADE;
                    break;
                default:
                    return false;
            }
            PermType permType;
            switch (permTypeStr)
            {
                case "use":
                    permType = PermType.USE_PERM;
                    break;
                case "build":
                    permType = PermType.BUILD_AND_DESTROY_PERM;
                    break;
                case "attack":
                    permType = PermType.ATTACK_ANIMALS_PERM;
                    break;
                default:
                    return false;
            }

            bool newValue = StringFunctions.getBoolFromString(newValueStr);

            switch (permGroup)
            {
                case PermGroup.STRANGER:
                    this.StrangerPerms[(int)permType] = newValue;
                    break;
                case PermGroup.CITIZEN:
                    this.CitizenPerms[(int)permType] = newValue;
                    break;
                case PermGroup.ALLY:
                    this.AlliancePerms[(int)permType] = newValue;
                    break;
                case PermGroup.COMRADE:
                    this.ComradePerms[(int)permType] = newValue;
                    break;
            }
            return true;
        }
        public bool getPerm(PermGroup group, PermType type)
        {
            switch (group)
            {
                case PermGroup.STRANGER:
                    return StrangerPerms[(int)type];
                case PermGroup.CITIZEN:
                    return CitizenPerms[(int)type];
                case PermGroup.ALLY:
                    return AlliancePerms[(int)type];
                case PermGroup.COMRADE:
                    return ComradePerms[(int)type];
            }
            return false;
        }
        public void setValueForAll(bool val)
        {
            for(int i = 0; i < CitizenPerms.Length; i++)
            {
                CitizenPerms[i] = val;
            }
            for (int i = 0; i < StrangerPerms.Length; i++)
            {
                StrangerPerms[i] = val;
            }
            for (int i = 0; i < AlliancePerms.Length; i++)
            {
                AlliancePerms[i] = val;
            }
            for (int i = 0; i < ComradePerms.Length; i++)
            {
                ComradePerms[i] = val;
            }
        }
        public void setPerms(string loadedStr)
        {
            foreach(string it in loadedStr.Split(';'))
            {
                if (it.Length == 0)
                    continue;

                if (it.Contains(":"))
                {
                    string[] parts = it.Split(':');
                    switch(parts[0])
                    {
                        case "stanger":
                            StrangerPerms[permTypeStringToInt[parts[1]]] = true;
                            continue;
                        case "citizen":
                            CitizenPerms[permTypeStringToInt[parts[1]]] = true;
                            continue;
                        case "ally":
                            AlliancePerms[permTypeStringToInt[parts[1]]] = true;
                            continue;
                        case "friend":
                            ComradePerms[permTypeStringToInt[parts[1]]] = true;
                            continue;
                    }
                }
                else {

                    switch (it)
                    {
                        case "pvp":
                            this.pvpFlag = true;
                            continue;
                        case "fire":
                            this.fireFlag = true;
                            continue;
                        case "blast":
                            this.blastFlag = true;
                            continue;
                    }
                }

            }
        }
        public void setPvp(bool val)
        {
            pvpFlag = val;
        }
        /// <summary>
        /// Get string (should be on/off string) and set pvp flag value
        /// </summary>
        /// <param name="val"></param>
        public bool setPvp(string val)
        {
            bool? whichValue = LogicFunctions.IsOnOffNone(val);
            if(whichValue.HasValue)
            {
                if(whichValue.Value == this.pvpFlag)
                {
                    return false;
                }
                else
                {
                    this.pvpFlag = whichValue.Value;
                }
            }
            return true;
        }
        public void setFire(bool val)
        {
            fireFlag = val;
        }
        /// <summary>
        /// Get string (should be on/off string) and set firespread flag value
        /// </summary>
        /// <param name="val"></param>
        public bool setFire(string val)
        {
            bool? whichValue = LogicFunctions.IsOnOffNone(val);
            if (whichValue.HasValue)
            {
                if (whichValue.Value == this.fireFlag)
                {
                    return false;
                }
                else
                {
                    this.fireFlag = whichValue.Value;
                }
            }
            return true;
        }
        public void setBlast(bool val)
        {
            blastFlag = val;
        }
        /// <summary>
        /// Get string (should be on/off string) and set blast flag value
        /// </summary>
        /// <param name="val"></param>
        public bool setBlast(string val)
        {
            bool? whichValue = LogicFunctions.IsOnOffNone(val);
            if (whichValue.HasValue)
            {
                if (whichValue.Value == this.blastFlag)
                {
                    return false;
                }
                else
                {
                    this.blastFlag = whichValue.Value;
                }
            }
            return true;
        }

        public new string ToString()
        {
            StringBuilder stringBuilder = new StringBuilder();  
            if(pvpFlag)
            {
                stringBuilder.Append("pvp;");
            }
            if(fireFlag)
            {
                stringBuilder.Append("fire;");
            }
            if(blastFlag)
            {
                stringBuilder.Append("blast;");
            }

            for(int i = 0; i < StrangerPerms.Length; i++)
            {
                if(StrangerPerms[i])
                {
                    stringBuilder.Append("stanger").Append(":").Append(permTypeToString[i]).Append(";");
                }
            }

            for (int i = 0; i < CitizenPerms.Length; i++)
            {
                if (CitizenPerms[i])
                {
                    stringBuilder.Append("citizen").Append(":").Append(permTypeToString[i]).Append(";");
                }
            }
            for (int i = 0; i < AlliancePerms.Length; i++)
            {
                if (AlliancePerms[i])
                {
                    stringBuilder.Append("ally").Append(":").Append(permTypeToString[i]).Append(";");
                }
            }
            for (int i = 0; i < ComradePerms.Length; i++)
            {
                if (ComradePerms[i])
                {
                    stringBuilder.Append("friend").Append(":").Append(permTypeToString[i]).Append(";");
                }
            }
            return stringBuilder.ToString();
        }
    }
}
