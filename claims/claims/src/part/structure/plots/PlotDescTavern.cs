using claims.src.auxialiry;
using claims.src.part.structure;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using Vintagestory.API.Common;
using Vintagestory.API.MathTools;
using Vintagestory.API.Server;

namespace claims.src.part.structure.plots
{
    public class PlotDescTavern : PlotDesc
    {
        public override string Serialize(Plot plot) => toSaveStringInnerClaims();
        public override void Deserialize(string data, Plot plot) => fromLoadStringInnerClaims(data);
        public override bool Validate(Plot plot, IServerPlayer player, ref TextCommandResult tcr)
        {
            int count = plot.getCity().getCityPlots().Count(p => p.Type == PlotType.TAVERN);
            if (count >= claims.config.MAX_NUMBER_TAVERN_PER_CITY)
            {
                tcr.StatusMessage = "claims:too_much_taverns";
                tcr.MessageParams = new object[] { claims.config.MAX_NUMBER_TAVERN_PER_CITY };
                return false;
            }
            return true;
        }
        public List<InnerClaim> innerClaims = new();
        public string innerClaimsToString()
        {
            StringBuilder sb = new();
            for(int i = 0; i < innerClaims.Count; i++)
            {
                sb.Append(i.ToString()).Append(". ").Append("Use: ").Append(innerClaims[i].permissionsFlags[0] ? "on " : "off ")
                    .Append("Build: ").Append(innerClaims[i].permissionsFlags[1] ? "on " : "off ")
                    .Append("AttackAnimals: ").Append(innerClaims[i].permissionsFlags[2] ? "on " : "off");
                if (innerClaims[i].membersUids.Count == 0)
                { }
                else
                {
                    sb.Append("\n");
                }
                foreach(string it in innerClaims[i].membersUids)
                {
                    claims.dataStorage.GetPlayerByUid(it, out PlayerInfo playerInfo);
                    if(playerInfo != null)
                    {
                        sb.Append(playerInfo.GetPartName());
                    }
                    if(!it.Equals(innerClaims[i].membersUids.Last()))
                    {
                        sb.Append(", ");
                    }
                }
                if(i != innerClaims.Count - 1)
                {
                    sb.Append("\n");
                }             
            }
            return sb.ToString();
        }
        public void addNewInnerClaim(Vec3i pos1, Vec3i pos2)
        {
            innerClaims.Add(new InnerClaim(pos1, pos2));
        }
        public void fromLoadStringInnerClaims(string val)
        {
            string[] innerClaimsStrs = val.Split('#');
            foreach(var it in innerClaimsStrs)
            {
                if(it.Length == 0)
                {
                    continue;
                }
                string[] innerClaimParts = it.Split(':');

                string[] pos1 = innerClaimParts[0].Split(',');

                Vec3i tmp_pos_1 = new();
                try
                {
                    tmp_pos_1.X = int.Parse(pos1[0]);
                    tmp_pos_1.Y = int.Parse(pos1[1]);
                    tmp_pos_1.Z = int.Parse(pos1[2]);
                }catch
                {
                    continue;
                }
                string[] pos2 = innerClaimParts[1].Split(',');
                Vec3i tmp_pos_2 = new();
                try
                {
                    tmp_pos_2.X = int.Parse(pos2[0]);
                    tmp_pos_2.Y = int.Parse(pos2[1]);
                    tmp_pos_2.Z = int.Parse(pos2[2]);
                }
                catch
                {
                    continue;
                }

                InnerClaim innerClaim = new(tmp_pos_1, tmp_pos_2);

                string[] flags = innerClaimParts[2].Split(',');
                for(int i = 0; i < 3; i++)
                {
                    innerClaim.permissionsFlags[i] = flags[i].Equals("1") ? true : false;
                }
                string[] uidsPlayers = innerClaimParts[3].Split(',');
                foreach(var uid in uidsPlayers)
                {
                    if(uid.Length == 0)
                    {
                        continue;
                    }
                    innerClaim.membersUids.Add(uid);
                }
                this.innerClaims.Add(innerClaim);
            }
            //summonPoint = new Vec3d(double.Parse(strs[0]), double.Parse(strs[1]), double.Parse(strs[2]));
        }
        public string toSaveStringInnerClaims()
        {
            StringBuilder stringBuilder = new();
            foreach (var it in innerClaims)
            {
                stringBuilder.Append(it.ToString());
                stringBuilder.Append("#");
            }
            //summonPoint = new Vec3d(double.Parse(strs[0]), double.Parse(strs[1]), double.Parse(strs[2]));
            return stringBuilder.ToString();
        }
    }
}
