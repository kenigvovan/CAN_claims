using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Newtonsoft.Json;
using Vintagestory.API.MathTools;

namespace claims.src.part.structure
{
    public class Prison : Part
    {
        List<PrisonCellInfo> prisonCells { get; set; } = new List<PrisonCellInfo>();
        public City City { get; set; }
        public Plot Plot { get; set; }
        public Prison(string val, string guid) : base(val, guid)
        {
        } 
        public Vec3i getRandomRespawnPoint()
        {
            if (prisonCells.Count < 1)
                return null;
            return prisonCells[claims.dataStorage.r.Next() % prisonCells.Count].getSpawnPosition();
        }
        public bool TryGetRandomCell(out PrisonCellInfo cell)
        {
            if(prisonCells.Count() < 1)
            {
                cell = null;
                return false;
            }
            cell = prisonCells[claims.dataStorage.r.Next() % prisonCells.Count];
            return true;
        }
        public void addPrisonCell(PrisonCellInfo prisonCellInfo)
        {
            prisonCells.Add(prisonCellInfo);
        }
        public void removePrisonCell(int val)
        {
            prisonCells.Remove(prisonCells[val]);
        }
        public void removePrisonCell(PrisonCellInfo cell)
        {
            prisonCells.Remove(cell);
        }
        public List<PrisonCellInfo> getPrisonCells()
        {
            return prisonCells; 
        }
        public void RemovePlayerFromAllCells(PlayerInfo player)
        {
            foreach(var it in prisonCells)
            {
                if(it.prisonedPlayers.Contains(player))
                {
                    it.prisonedPlayers.Remove(player);
                    it.playerNames.Remove(player.GetPartName());
                }
            }
        }
        public bool TryGetCellInWhichPlayer(PlayerInfo player, out PrisonCellInfo cell)
        {
            foreach (var it in prisonCells)
            {
                if (it.prisonedPlayers.Contains(player))
                {
                    cell = it;
                    return true;
                }
            }
            cell = null;
            return false;
        }
        public string SerializeCells()
        {
            StringBuilder sb = new StringBuilder();

            //one cell info/ second cell info
            foreach (var cell in prisonCells)
            {
                sb.Append(cell.ToString());
                if (!cell.Equals(prisonCells.Last()))
                {
                    sb.Append("|");
                }
            }
            //DeserializeCells(sb.ToString());
            return sb.ToString();
        }
        public void DeserializeCells(string input)
        {
            string[] cells = input.Split('|');
            foreach (var cell in cells)
            {
                if (cell.Length == 0)
                    continue;
                PrisonCellInfo newCell = new PrisonCellInfo();
                newCell.fromString(cell);
                prisonCells.Add(newCell);
            }
        }
        public override string ToString()
        {
            StringBuilder sb = new StringBuilder();

            //one cell info/ second cell info
            foreach(var cell in prisonCells)
            {
                sb.Append(cell.ToString());
                if(!cell.Equals(prisonCells.Last()))
                {
                    sb.Append("|");
                }
            }
            return sb.ToString();
        }
        public void fromString(string input)
        {
            string[] cells = input.Split('|');
            foreach(var cell in cells)
            {
                if (cell.Length == 0)
                    continue;
                PrisonCellInfo newCell = new PrisonCellInfo();
                newCell.fromString(cell);
                prisonCells.Add(newCell);
            }
        }
        public override bool saveToDatabase(bool update = true)
        {
            return claims.getModInstance().getDatabaseHandler().savePrison(this, update);
        }
    }
}
