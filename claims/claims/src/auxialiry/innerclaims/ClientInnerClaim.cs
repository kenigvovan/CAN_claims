using ProtoBuf;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Vintagestory.API.MathTools;

namespace claims.src.auxialiry.innerclaims
{
    [ProtoContract]
    public class ClientInnerClaim
    {
        [ProtoMember(1)]
        public Vec3i pos1;
        [ProtoMember(2)]
        public Vec3i pos2;
        /// <summary>
        /// use, build, attack. Empty by default, filled by the constructor: protobuf-net appends to
        /// an array that is already there, and the client map cache is protobuf - a default of three
        /// came back as six, the stored three after three falses, read as "nothing allowed".
        /// </summary>
        [ProtoMember(3)]
        public bool[] permissionsFlags = new bool[0];
        /// <summary>The sub-plot this piece belongs to; empty for a tavern room.</summary>
        [ProtoMember(4)]
        public string Name = "";
        /// <summary>The sub-plot's owner; empty while the city holds it.</summary>
        [ProtoMember(5)]
        public string Owner = "";
        /// <summary>Asking price, -1 when not for sale. Required: protobuf-net would skip a 0 and read back -1.</summary>
        [ProtoMember(6, IsRequired = true)]
        public int Price = -1;
        /// <summary>Daily tax of the owner.</summary>
        [ProtoMember(7)]
        public double Tax;

        public bool IsSubPlot => !string.IsNullOrEmpty(Name);
        public int MinX => Math.Min(pos1.X, pos2.X);

        public int MinY => Math.Min(pos1.Y, pos2.Y);

        public int MinZ => Math.Min(pos1.Z, pos2.Z);

        public int MaxX => Math.Max(pos1.X, pos2.X);

        public int MaxY => Math.Max(pos1.Y, pos2.Y);

        public int MaxZ => Math.Max(pos1.Z, pos2.Z);
        public bool Intersects(ClientInnerClaim with)
        {
            if (with.MaxX < MinX || with.MinX > MaxX)
            {
                return false;
            }

            if (with.MaxY < MinY || with.MinY > MaxY)
            {
                return false;
            }

            if (with.MaxZ >= MinZ)
            {
                return with.MinZ <= MaxZ;
            }

            return false;
        }
        public bool Contains(Vec3d pos)
        {
            if (pos.X >= (double)MinX && pos.X < (double)MaxX && pos.Y >= (double)MinY && pos.Y < (double)MaxY && pos.Z >= (double)MinZ)
            {
                return pos.Z < (double)MaxZ;
            }

            return false;
        }

        public bool Contains(int x, int y, int z)
        {
            if (x >= MinX && x < MaxX && y >= MinY && y < MaxY && z >= MinZ)
            {
                return z < MaxZ;
            }

            return false;
        }

        /// <summary>Max corner outside on every axis, as the other overloads; it used to be inside on X and Y.</summary>
        public bool Contains(BlockPos pos) => Contains(pos.X, pos.Y, pos.Z);

        /// <summary>One of use (0), build (1), attack (2); false when the flags did not arrive whole.</summary>
        public bool May(int type) => permissionsFlags != null && type >= 0 && type < permissionsFlags.Length && permissionsFlags[type];

        public ClientInnerClaim()
        {

        }
        public ClientInnerClaim(Vec3i pos1, Vec3i pos2, bool[] permissionsFlags)
        {
            this.pos1 = pos1;
            this.pos2 = pos2;
            this.permissionsFlags = new bool[3] { permissionsFlags[0], permissionsFlags[1], permissionsFlags[2] };
        }

        public override string ToString()
        {
            StringBuilder sb = new StringBuilder();
            sb.Append(pos1.X).Append(",").Append(pos1.Y).Append(",").Append(pos1.Z).Append(":").Append(pos2.X).Append(",").Append(pos2.Y).Append(",").Append(pos2.Z).Append(":").Append(May(0) ? "1" : "0").Append(",").
                Append(May(1) ? "1" : "0").Append(",").Append(May(2) ? "1" : "0").Append(":");
            return sb.ToString();
        }
    }
}
