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
        public static int plotSize => claims.config.PLOT_SIZE;
        Vec2i pos = new Vec2i();
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
        public static PlotPosition fromXZ(int x, int z)
        {
            PlotPosition tmp = new PlotPosition();
            tmp.pos.X = x / plotSize;
            tmp.pos.Y = z / plotSize;
            return tmp;
        }
        public PlotPosition(int x, int z)
        {
            this.pos.X = x;
            this.pos.Y = z;
        }

        public PlotPosition Clone()
        {
            return new PlotPosition(pos.X, pos.Y);
        }
        public PlotPosition(Vec2i pos)
        {
            this.pos.X = pos.X;
            this.pos.Y= pos.Y;
        }
        public PlotPosition(BlockPos pos)
        {
            this.pos.X = pos.X;
            this.pos.Y = pos.Z;
        }
        public static PlotPosition fromBlockPos(BlockPos pos)
        {
            PlotPosition tmp = new PlotPosition();
            tmp.pos.X = pos.X / plotSize;
            tmp.pos.Y = pos.Z / plotSize;
            return tmp;
        }
        public PlotPosition(EntityPos pos)
        {
            this.pos.X = (int)(pos.X);
            this.pos.Y = (int)(pos.Z);
        }
        public static PlotPosition fromEntityyPos(EntityPos pos)
        {
            PlotPosition tmp = new PlotPosition();
            tmp.pos.X = (int)(pos.X / plotSize);
            tmp.pos.Y = (int)(pos.Z / plotSize);
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
        public override bool Equals(object obj)
        {
            if (obj == this)
                return true;
            if (!(obj is PlotPosition))
                return false;

            if (!(obj is PlotPosition))
            {
                PlotPosition tmp_in = (PlotPosition)obj;
                return this.getPos().X == tmp_in.getPos().X && this.getPos().Y == tmp_in.getPos().Y;
            }

            PlotPosition tmp = (PlotPosition)obj;

            return this.getPos().X == tmp.getPos().X && this.getPos().Y == tmp.getPos().Y;
        }
        public override int GetHashCode()
        {
            int hash = 13;
            hash = (hash * 7) + pos.X;
            hash = (hash * 7) + pos.Y;
            return hash;
        }
        public static void makeChunkHighlight(IWorldAccessor world, IPlayer player, Plot toPlot = null)
        {
            List<BlockPos> bList = new List<BlockPos>();

            int ps = plotSize;
            // Span the full world height instead of a hardcoded 256, so borders are
            // visible for sky/high-altitude builds on worlds taller than 256.
            int worldHeight = world.BlockAccessor.MapSizeY;
            int x = (int)(player.Entity.Pos.X - (player.Entity.Pos.X % ps));
            int z = (int)(player.Entity.Pos.Z - player.Entity.Pos.Z % ps);
            bList.Add(new BlockPos(x, 0, z));
            x = (int)(player.Entity.Pos.X + ps - (player.Entity.Pos.X % ps));
            z = (int)(player.Entity.Pos.Z + ps - (player.Entity.Pos.Z % ps));
            bList.Add(new BlockPos(x, worldHeight, z));
            List<int> colors = new List<int>();

            if(!claims.dataStorage.GetPlayerByUid(player.PlayerUID, out PlayerInfo playerInfo))
            {
                return;
            }
            if(toPlot == null)
            {
                claims.dataStorage.GetPlot(PlotPosition.fromEntityyPos(player.Entity.ServerPos), out toPlot);
            }
            //NO CITY, no plot
            if(toPlot == null || !toPlot.hasCity() || !playerInfo.hasCity())
            {
                colors.Add(ColorUtil.ToRgba(claims.config.PLOT_BORDERS_COLOR_WILD_PLOT[0],
                                            claims.config.PLOT_BORDERS_COLOR_WILD_PLOT[1],
                                            claims.config.PLOT_BORDERS_COLOR_WILD_PLOT[2],
                                            claims.config.PLOT_BORDERS_COLOR_WILD_PLOT[3]));
                claims.sapi.World.HighlightBlocks(player, 59, bList, colors, shape: EnumHighlightShape.Cube);
                return;
            }
            City city = playerInfo.City;
            //OUR CITY
            if(toPlot.getCity().Equals(city))
            {
                colors.Add(ColorUtil.ToRgba(claims.config.PLOT_BORDERS_COLOR_OUR_CITY_PLOT[0],
                                            claims.config.PLOT_BORDERS_COLOR_OUR_CITY_PLOT[1],
                                            claims.config.PLOT_BORDERS_COLOR_OUR_CITY_PLOT[2],
                                            claims.config.PLOT_BORDERS_COLOR_OUR_CITY_PLOT[3]));
                claims.sapi.World.HighlightBlocks(player, 59, bList, colors, shape: EnumHighlightShape.Cube);
                return;
            }

            //SOME OTHER CITY
            colors.Add(ColorUtil.ToRgba(claims.config.PLOT_BORDERS_COLOR_OTHER_PLOT[0],
                                        claims.config.PLOT_BORDERS_COLOR_OTHER_PLOT[1],
                                        claims.config.PLOT_BORDERS_COLOR_OTHER_PLOT[2],
                                        claims.config.PLOT_BORDERS_COLOR_OTHER_PLOT[3]));
            claims.sapi.World.HighlightBlocks(player, 59, bList, colors, shape: EnumHighlightShape.Cube);
            return;          
        }
        public static void clearChunkHighlight(IWorldAccessor world, IPlayer player)
        {
            world.HighlightBlocks(player, 59, new List<BlockPos>(), new List<int>());
        }
    }
}
