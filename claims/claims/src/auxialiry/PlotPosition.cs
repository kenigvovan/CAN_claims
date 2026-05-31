using System.Collections.Generic;
using claims.src.part;
using claims.src.part.structure;
using Vintagestory.API.Common;
using Vintagestory.API.Common.Entities;
using Vintagestory.API.MathTools;

namespace claims.src.auxialiry
{
    public class PlotPosition
    {
        public const int COLUMN_Y = -1;

        public static int plotSize => claims.config.PLOT_SIZE;
        Vec2i pos = new Vec2i();

        // COLUMN_Y (-1) = legacy column plot (full world height).
        // >= 0 = specific 16-block layer index (3D plot system).
        public int LayerY = COLUMN_Y;

        public int X
        {
            get { return pos.X; }
            set { pos.X = value; }
        }
        public int Z
        {
            get { return pos.Y; }
            set { pos.Y = value; }
        }
        public Vec2i getPos()
        {
            return pos;
        }
        public PlotPosition()
        {
        }

        // Creates a column-plot key (full-height, legacy).
        public static PlotPosition fromXZ(int x, int z)
        {
            PlotPosition tmp = new PlotPosition();
            tmp.pos.X = x / plotSize;
            tmp.pos.Y = z / plotSize;
            return tmp;
        }

        // Use this everywhere a player's standing position should determine the plot.
        // Returns a 3D key (includes LayerY) when ENABLE_3D_PLOTS is on, column key otherwise.
        public static PlotPosition fromPlayerPos(EntityPos pos)
        {
            return claims.config.ENABLE_3D_PLOTS
                ? fromEntityyPos(pos)
                : fromXZ((int)pos.X, (int)pos.Z);
        }

        // Creates a 3D-plot key from world coordinates.
        public static PlotPosition fromXZY(int worldX, int worldZ, int worldY)
        {
            PlotPosition tmp = new PlotPosition();
            tmp.pos.X = worldX / plotSize;
            tmp.pos.Y = worldZ / plotSize;
            tmp.LayerY = worldY / plotSize;
            return tmp;
        }

        // Returns a column-plot key at the same X,Z (for 2-step GetPlot fallback).
        public PlotPosition ToColumnKey()
        {
            return new PlotPosition(pos.X, pos.Y); // LayerY stays COLUMN_Y (default)
        }

        public PlotPosition(int x, int z)
        {
            this.pos.X = x;
            this.pos.Y = z;
        }

        public PlotPosition Clone()
        {
            var c = new PlotPosition(pos.X, pos.Y);
            c.LayerY = this.LayerY;
            return c;
        }
        public PlotPosition(Vec2i pos)
        {
            this.pos.X = pos.X;
            this.pos.Y = pos.Y;
        }
        public PlotPosition(BlockPos pos)
        {
            this.pos.X = pos.X;
            this.pos.Y = pos.Z;
        }

        // Produces a 3D-aware position: LayerY set from blockPos.Y.
        public static PlotPosition fromBlockPos(BlockPos pos)
        {
            PlotPosition tmp = new PlotPosition();
            tmp.pos.X = pos.X / plotSize;
            tmp.pos.Y = pos.Z / plotSize;
            tmp.LayerY = pos.Y / plotSize;
            return tmp;
        }

        public PlotPosition(EntityPos pos)
        {
            this.pos.X = (int)(pos.X);
            this.pos.Y = (int)(pos.Z);
        }

        // Produces a 3D-aware position: LayerY set from entityPos.Y.
        public static PlotPosition fromEntityyPos(EntityPos pos)
        {
            PlotPosition tmp = new PlotPosition();
            tmp.pos.X = (int)(pos.X / plotSize);
            tmp.pos.Y = (int)(pos.Z / plotSize);
            tmp.LayerY = (int)(pos.Y / plotSize);
            return tmp;
        }

        public void setX(int val)
        {
            this.pos.X = val;
        }
        public void setY(int val)
        {
            this.pos.Y = val;
        }
        public void setXY(Vec2i val)
        {
            this.pos.X = val.X;
            this.pos.Y = val.Y;
        }
        // Sets X, Z, and LayerY from a Vec3i plot key (X=gridX, Y=layerY, Z=gridZ).
        public void setXYZ(Vec3i val)
        {
            this.pos.X = val.X;
            this.pos.Y = val.Z;
            this.LayerY = val.Y;
        }

        public override bool Equals(object obj)
        {
            if (obj == this) return true;
            if (!(obj is PlotPosition tmp)) return false;
            return pos.X == tmp.pos.X && pos.Y == tmp.pos.Y && LayerY == tmp.LayerY;
        }

        public override int GetHashCode()
        {
            int hash = 13;
            hash = (hash * 7) + pos.X;
            hash = (hash * 7) + pos.Y;
            hash = (hash * 7) + LayerY;
            return hash;
        }

        public static void makeChunkHighlight(IWorldAccessor world, IPlayer player, Plot toPlot = null)
        {
            List<BlockPos> bList = new List<BlockPos>();

            int ps = plotSize;
            int x = (int)(player.Entity.Pos.X - (player.Entity.Pos.X % ps));
            int z = (int)(player.Entity.Pos.Z - player.Entity.Pos.Z % ps);

            // Lookup plot now if not provided — needed to determine Y bounds before building bList.
            if (toPlot == null)
            {
                claims.dataStorage.GetPlot(PlotPosition.fromEntityyPos(player.Entity.ServerPos), out toPlot);
            }

            // Always show only the 16-block layer the player is currently standing in.
            // For column plots this is just a visual hint — the actual protection covers the full height.
            int playerLayer = (int)(player.Entity.Pos.Y / ps);
            int yBottom = playerLayer * ps;
            int yTop    = yBottom + ps;

            bList.Add(new BlockPos(x, yBottom, z));
            x = (int)(player.Entity.Pos.X + ps - (player.Entity.Pos.X % ps));
            z = (int)(player.Entity.Pos.Z + ps - (player.Entity.Pos.Z % ps));
            bList.Add(new BlockPos(x, yTop, z));

            List<int> colors = new List<int>();

            if (!claims.dataStorage.GetPlayerByUid(player.PlayerUID, out PlayerInfo playerInfo))
            {
                return;
            }
            if (toPlot == null || !toPlot.hasCity() || !playerInfo.hasCity())
            {
                colors.Add(ColorUtil.ToRgba(claims.config.PLOT_BORDERS_COLOR_WILD_PLOT[0],
                                            claims.config.PLOT_BORDERS_COLOR_WILD_PLOT[1],
                                            claims.config.PLOT_BORDERS_COLOR_WILD_PLOT[2],
                                            claims.config.PLOT_BORDERS_COLOR_WILD_PLOT[3]));
                claims.sapi.World.HighlightBlocks(player, 59, bList, colors, shape: EnumHighlightShape.Cubes);
                return;
            }
            City city = playerInfo.City;
            if (toPlot.getCity().Equals(city))
            {
                colors.Add(ColorUtil.ToRgba(claims.config.PLOT_BORDERS_COLOR_OUR_CITY_PLOT[0],
                                            claims.config.PLOT_BORDERS_COLOR_OUR_CITY_PLOT[1],
                                            claims.config.PLOT_BORDERS_COLOR_OUR_CITY_PLOT[2],
                                            claims.config.PLOT_BORDERS_COLOR_OUR_CITY_PLOT[3]));
                claims.sapi.World.HighlightBlocks(player, 59, bList, colors, shape: EnumHighlightShape.Cubes);
                return;
            }
            colors.Add(ColorUtil.ToRgba(claims.config.PLOT_BORDERS_COLOR_OTHER_PLOT[0],
                                        claims.config.PLOT_BORDERS_COLOR_OTHER_PLOT[1],
                                        claims.config.PLOT_BORDERS_COLOR_OTHER_PLOT[2],
                                        claims.config.PLOT_BORDERS_COLOR_OTHER_PLOT[3]));
            claims.sapi.World.HighlightBlocks(player, 59, bList, colors, shape: EnumHighlightShape.Cubes);
        }
        public static void clearChunkHighlight(IWorldAccessor world, IPlayer player)
        {
            world.HighlightBlocks(player, 59, new List<BlockPos>(), new List<int>());
        }
    }
}
