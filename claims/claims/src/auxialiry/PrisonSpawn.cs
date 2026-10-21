using System.Linq;
using Vintagestory.API.Common;
using Vintagestory.API.MathTools;
using Vintagestory.API.Server;
using Vintagestory.Server;

namespace claims.src.auxialiry
{
    /// <summary>
    /// Imprisonment moves the player's spawn into the cell. The spawn they had before is kept in
    /// the engine's own player data (saved with the player, also across offline releases) and
    /// given back on release or escape.
    /// </summary>
    public static class PrisonSpawn
    {
        private const string Key = "claims:spawnbeforeprison";
        private const string NoOwnSpawn = "none";

        /// <summary>Call before setting the cell spawn. A second arrest keeps the first saved spawn.</summary>
        public static void Remember(IServerPlayer player)
        {
            var data = player.ServerData.CustomPlayerData;
            if (data.ContainsKey(Key)) return;
            PlayerSpawnPos own = (player.WorldData as ServerWorldPlayerData)?.SpawnPosition;
            data[Key] = own == null ? NoOwnSpawn : $"{own.x};{own.y?.ToString() ?? ""};{own.z}";
        }

        /// <summary>
        /// Gives back the spawn saved by <see cref="Remember"/>. Without one (arrested before this
        /// was saved) only a spawn still pointing at a prison cell is cleared to the world spawn.
        /// </summary>
        public static void Restore(IServerPlayer player)
        {
            var data = player.ServerData.CustomPlayerData;
            if (!data.TryGetValue(Key, out string saved))
            {
                PlayerSpawnPos current = (player.WorldData as ServerWorldPlayerData)?.SpawnPosition;
                if (current != null && IsAtPrisonCell(current)) player.ClearSpawnPosition();
                return;
            }
            data.Remove(Key);
            string[] parts = saved.Split(';');
            if (parts.Length == 3 && int.TryParse(parts[0], out int x) && int.TryParse(parts[2], out int z))
            {
                int? y = int.TryParse(parts[1], out int py) ? py : null;
                player.SetSpawnPosition(new PlayerSpawnPos(x, y, z));
            }
            else
            {
                player.ClearSpawnPosition();
            }
        }

        private static bool IsAtPrisonCell(PlayerSpawnPos pos) =>
            claims.dataStorage.getCitiesList()
                .SelectMany(c => c.getPrisons())
                .SelectMany(p => p.getPrisonCells())
                .Any(cell => cell.spawnPostion is Vec3i s && s.X == pos.x && s.Z == pos.z
                    && (pos.y == null || s.Y == pos.y));
    }
}
