using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using claims.src.auxialiry;
using claims.src.bb;
using claims.src.citylog;
using claims.src.gui.playerGui.structures;
using claims.src.messages;
using claims.src.part;
using claims.src.part.structure;
using claims.src.part.structure.conflict;
using claims.src.part.structure.war;
using claims.src.renderer;
using Vintagestory.API.Client;
using Vintagestory.API.Common;
using Vintagestory.API.Config;
using Vintagestory.API.Datastructures;
using Vintagestory.API.MathTools;

namespace claims.src.beb
{
    public class BlockEntityBehaviorFlag : BlockEntityBehavior
    {
        public ItemStack Banner { get; protected set; }
        public float CapturedPercent { get; protected set; }
        protected float captureDuration;
        private FlagRenderer renderer;
        public BlockBehaviorFlag BlockBehavior { get; protected set; }

        private long? updateRef;
        private long? captureRef;
        public string AllianceGuid { get; set; }
        public string CityGuid { get; set; }
        public string ConflictGuid { get; set; }
        public string PlayerGuid { get; set; }
        public int TimesToBreak { get; set; } = 0;
        public BlockEntityBehaviorFlag(BlockEntity blockEntity) : base(blockEntity) { }

        public override void Initialize(ICoreAPI api, JsonObject properties)
        {
            base.Initialize(api, properties);

            this.BlockBehavior = this.Block.GetBehavior<BlockBehaviorFlag>();

            this.captureDuration = claims.config.FLAG_CAPTURE_DURATION_SECONDS;

            this.Banner?.ResolveBlockOrItem(this.Api.World);
            if (this.Banner != null && this.Api is ICoreClientAPI client)
            {
                client.Tesselator.TesselateShape(this.Banner.Item, Shape.TryGet(client, "claims:shapes/flag/banner.json"), out MeshData meshData);
                this.renderer = new FlagRenderer(client, meshData, this.Pos, this, this.BlockBehavior.PoleTop, this.BlockBehavior.PoleBottom);
                client.Event.RegisterRenderer(this.renderer, EnumRenderStage.Opaque, "flag");

            }

            //this.updateRef = api.Event.RegisterGameTickListener(this.Update, 1000);

        }
        public override void OnBlockBroken(IPlayer byPlayer = null)
        {          
            base.OnBlockBroken(byPlayer);
            this.renderer?.Dispose();
            if (this.updateRef.HasValue) this.Api.Event.UnregisterGameTickListener(this.updateRef.Value);
            if (this.captureRef.HasValue) this.Api.Event.UnregisterGameTickListener(this.captureRef.Value);
            if (this.Banner != null) this.Api.World.SpawnItemEntity(this.Banner, this.Pos.ToVec3d());
        }
        public override void OnBlockRemoved()
        {
            base.OnBlockRemoved();
            this.renderer?.Dispose();
            if (this.updateRef.HasValue) this.Api.Event.UnregisterGameTickListener(this.updateRef.Value);
            if (this.captureRef.HasValue) this.Api.Event.UnregisterGameTickListener(this.captureRef.Value);
        }
        public override void OnBlockUnloaded()
        {
            base.OnBlockUnloaded();
            this.renderer?.Dispose();
            if (this.updateRef.HasValue) this.Api.Event.UnregisterGameTickListener(this.updateRef.Value);
            if (this.captureRef.HasValue) this.Api.Event.UnregisterGameTickListener(this.captureRef.Value);
        }
        public override void GetBlockInfo(IPlayer forPlayer, StringBuilder dsc)
        {
            base.GetBlockInfo(forPlayer, dsc);
        }
        private void Update(float deltaTime)
        {
            if(Banner == null)
            {
                return;
            }
            if (this.Api.Side == EnumAppSide.Server && IsCaptureNoLongerValid())
            {
                CancelCapture();
                return;
            }
            this.CapturedPercent += GameMath.Clamp(1.0f - this.CapturedPercent, -deltaTime / this.captureDuration, deltaTime / this.captureDuration);
            if (this.Api.Side == EnumAppSide.Server)
            {
                if (this.CapturedPercent >= 1f)
                {
                    if (this.captureRef.HasValue) this.Api.Event.UnregisterGameTickListener(this.captureRef.Value);
                    this.captureRef = null;
                    this.Banner = null;
                    this.renderer?.Dispose();
                    this.renderer = null;
                    this.CapturedPercent = 0;
                    this.captureDuration = 0;
                    if (!ConflictHandler.TryGetConflictByGuid(this.ConflictGuid, out var conflict))
                    {
                        return;
                    }
                    if (!conflict.ActiveWarTime)
                    {
                        return;
                    }
                    if (!claims.dataStorage.WarsTimes.TryGetValue(conflict.Guid, out var warTime))
                    {
                        return;
                    }
                    if (!warTime.PlotAttacks.TryGetValue(PlotPosition.fromBlockPos(this.Pos), out var plotAttack))
                    {
                        return;
                    }
                    if (!claims.dataStorage.getCityByGUID(this.CityGuid, out City attackerCity))
                    {
                        return;
                    }
                    if (!claims.dataStorage.GetPlot(PlotPosition.fromBlockPos(this.Pos), out var defenderPlot))
                    {
                        return;
                    }
                    if (defenderPlot.getCity().getCityPlots().Count == 1)
                    {
                        City defenderCity = defenderPlot.getCity();
                        warTime.PlotAttacks.Remove(PlotPosition.fromBlockPos(this.Pos));
                        TimesToBreak = 0;

                        if (claims.config.SEND_ANNOUNCEMENTS_PLOT_WAS_CAPTURED)
                        {
                            IConflictParty defenderParty = defenderCity.HasAlliance()
                                ? (IConflictParty)defenderCity.Alliance
                                : (IConflictParty)defenderCity;
                            IConflictParty attackerParty = attackerCity.HasAlliance()
                                ? (IConflictParty)attackerCity.Alliance
                                : (IConflictParty)attackerCity;

                            MessageHandler.SendMsgInAlliance(defenderParty,
                                Lang.Get("claims:city_destroyed_by_war", defenderCity.GetPartName(), attackerCity.GetPartName()));
                            MessageHandler.SendMsgInAlliance(attackerParty,
                                Lang.Get("claims:we_destroyed_city", defenderCity.GetPartName()));
                        }

                        if (defenderCity.HasAlliance())
                        {
                            Alliance alliance = defenderCity.Alliance;
                            alliance.Cities.Remove(defenderCity);
                            foreach (var hostileParty in alliance.HostileParties)
                            {
                                foreach (var hosCity in hostileParty.GetCities())
                                {
                                    hosCity.HostileCities.Remove(defenderCity);
                                    hosCity.saveToDatabase();
                                }
                            }
                            foreach (var comradeAlliance in alliance.ComradAlliancies)
                            {
                                foreach (var comCity in comradeAlliance.Cities)
                                {
                                    comCity.ComradeCities.Remove(defenderCity);
                                    comCity.saveToDatabase();
                                }
                            }
                            defenderCity.Alliance = null;
                            if (alliance.Cities.Count == 0)
                            {
                                PartDemolition.DemolishAlliance(alliance);
                            }
                            else
                            {
                                if (alliance.MainCity != null && alliance.MainCity.Equals(defenderCity))
                                {
                                    City newMain = alliance.Cities[0];
                                    alliance.MainCity = newMain;
                                    alliance.Leader = newMain.getMayor();
                                }
                                alliance.saveToDatabase();
                                UsefullPacketsSend.AddToQueueAllianceInfoUpdate(alliance.Guid,
                                    new Dictionary<string, object> { { "value", alliance.Guid } }, EnumPlayerRelatedInfo.NEW_ALLIANCE_ALL);
                            }
                        }

                        IConflictParty attackerParty = attackerCity.HasAlliance()
                            ? (IConflictParty)attackerCity.Alliance
                            : (IConflictParty)attackerCity;
                        foreach (var runningConflict in defenderCity.RunningConflicts.ToArray())
                        {
                            if (runningConflict.First.Equals(attackerParty))
                                runningConflict.State = ConflictState.FIRST_WON;
                            else if (runningConflict.Second.Equals(attackerParty))
                                runningConflict.State = ConflictState.SECOND_WON;
                            PartDemolition.DemolishConflict(runningConflict, EnumConflictEndReason.CityDestroyed);
                        }

                        PartDemolition.demolishCity(defenderCity, string.Format("Last plot captured by {0}", attackerCity.GetPartName()));

                        this.Api.Event.RegisterCallback((float ft) =>
                        {
                            this.Api.World.BlockAccessor.BreakBlock(this.Pos, null);
                        }, 1000);
                        return;
                    }
                    else
                    {
                        City defenderCity = defenderPlot.getCity();
                        defenderCity.AddLogEntry(EnumCityLogEvent.FlagCaptured, attackerCity.GetPartName(), defenderPlot.GetPartName());
                        attackerCity.AddLogEntry(EnumCityLogEvent.FlagCaptured, attackerCity.GetPartName(), defenderPlot.GetPartName());
                        defenderPlot.setCity(attackerCity);
                        defenderCity.getCityPlots().Remove(defenderPlot);
                        attackerCity.getCityPlots().Add(defenderPlot);
                        defenderPlot.setPlotOwner(null);
                        defenderPlot.UpdateBorderPlotValue();
                        defenderPlot.setCustomTax(0);
                        //shouldn't crash with default but better to remake it somehow with init functions
                        defenderPlot.setNewType(new TextCommandResult(), "default", null);
                        defenderPlot.setPlotGroup(null);
                        defenderPlot.extraBought = false;
                        defenderCity.saveToDatabase();
                        defenderPlot.saveToDatabase();
                        defenderPlot.getCity().saveToDatabase();

                        defenderPlot.CheckBorderPlotValue();
                        claims.serverPlayerMovementListener.markPlotToWasReUpdated(defenderPlot.getPos());

                        UsefullPacketsSend.AddToQueueCityInfoUpdate(defenderPlot.getCity().Guid, EnumPlayerRelatedInfo.CLAIMED_PLOTS, EnumPlayerRelatedInfo.CITY_LOG);
                        UsefullPacketsSend.AddToQueueCityInfoUpdate(defenderCity.Guid, EnumPlayerRelatedInfo.CLAIMED_PLOTS, EnumPlayerRelatedInfo.CITY_LOG);
                        defenderPlot.getCity().FirePlotsMapChanged(EnumPlotsMapChangeReason.PlotCapturedByUs);
                        defenderCity.FirePlotsMapChanged(EnumPlotsMapChangeReason.PlotLostToEnemy);
                        UsefullPacketsSend.AddToQueueAllPlayersInfoUpdate(new Dictionary<string, object> { { "value", defenderPlot.getPos() } }, EnumPlayerRelatedInfo.CITY_PLOT_RECOLOR);
                        warTime.PlotAttacks.Remove(PlotPosition.fromBlockPos(this.Pos));
                        TimesToBreak = 0;
                        this.Api.Event.RegisterCallback((float ft) =>
                        {
                            this.Api.World.BlockAccessor.BreakBlock(this.Pos, null);
                        }, 1000);
                        if (claims.config.SEND_ANNOUNCEMENTS_PLOT_WAS_CAPTURED)
                        {
                            IConflictParty defenderParty = defenderCity.HasAlliance()
                                ? (IConflictParty)defenderCity.Alliance
                                : (IConflictParty)defenderCity;
                            IConflictParty attackerParty = attackerCity.HasAlliance()
                                ? (IConflictParty)attackerCity.Alliance
                                : (IConflictParty)attackerCity;

                            if (claims.config.SEND_COORDS_OF_PLOT_WAS_CAPTURED)
                            {
                                var localPos = PosFunctions.TranslateCoordsToLocalVec3d(this.Api, this.Pos);
                                MessageHandler.SendMsgInAlliance(defenderParty,
                                    Lang.Get("claims:our_city_plot_was_captured_on_pos", defenderCity.GetPartName(), attackerCity.GetPartName(), localPos.X, localPos.Y, localPos.Z));
                                MessageHandler.SendMsgInAlliance(attackerParty,
                                    Lang.Get("claims:we_captured_plot_on_pos", defenderCity.GetPartName(), localPos.X, localPos.Y, localPos.Z));
                            }
                            else
                            {
                                MessageHandler.SendMsgInAlliance(defenderParty,
                                    Lang.Get("claims:our_city_plot_was_captured", defenderCity.GetPartName(), attackerCity.GetPartName()));
                                MessageHandler.SendMsgInAlliance(attackerParty,
                                    Lang.Get("claims:we_captured_plot", defenderCity.GetPartName()));
                            }
                        }
                        return;
                    }
                }
            }
        }
        private bool IsCaptureNoLongerValid()
        {
            if (!claims.dataStorage.GetPlot(PlotPosition.fromBlockPos(this.Pos), out var plot))
            {
                return true;
            }
            City defenderCity = plot.getCity();
            if (defenderCity == null)
            {
                return true;
            }
            if (!claims.dataStorage.getCityByGUID(this.CityGuid, out City attackerCity))
            {
                return true;
            }
            IConflictParty defenderParty = defenderCity.HasAlliance()
                ? (IConflictParty)defenderCity.Alliance
                : (IConflictParty)defenderCity;
            IConflictParty attackerParty = attackerCity.HasAlliance()
                ? (IConflictParty)attackerCity.Alliance
                : (IConflictParty)attackerCity;
            return attackerParty.Equals(defenderParty);
        }
        private void CancelCapture()
        {
            if (this.updateRef.HasValue)
            {
                this.Api.Event.UnregisterGameTickListener(this.updateRef.Value);
                this.updateRef = null;
            }
            this.CapturedPercent = 0;
            this.TimesToBreak = 0;
            if (this.ConflictGuid != null
                && claims.dataStorage.WarsTimes.TryGetValue(this.ConflictGuid, out var warTime))
            {
                warTime.PlotAttacks.Remove(PlotPosition.fromBlockPos(this.Pos));
            }
            this.Api.Event.RegisterCallback((float ft) =>
            {
                this.Api.World.BlockAccessor.BreakBlock(this.Pos, null);
            }, 1000);
        }
        public void TryStartCapture(IPlayer byPlayer)
        {
            if (this.Api.Side == EnumAppSide.Server)
            {
                if (this.updateRef == null)
                {
                    if (!claims.dataStorage.GetPlayerByUid(byPlayer.PlayerUID, out var playerInfo) || !playerInfo.hasCity())
                    {
                        return;
                    }
                    IConflictParty attackerParty = playerInfo.HasAlliance()
                        ? (IConflictParty)playerInfo.Alliance
                        : (IConflictParty)playerInfo.City;
                    PlotPosition currentPlotPosition = PlotPosition.fromBlockPos(this.Pos);
                    if (!claims.dataStorage.GetPlot(currentPlotPosition, out Plot plotHere))
                    {
                        return;
                    }
                    City defenderCity = plotHere.getCity();
                    if (defenderCity == null)
                    {
                        return;
                    }
                    IConflictParty defenderParty = defenderCity.HasAlliance()
                        ? (IConflictParty)defenderCity.Alliance
                        : (IConflictParty)defenderCity;
                    if (attackerParty.Equals(defenderParty))
                    {
                        return;
                    }
                    if (!ConflictHandler.TryGetConflictWithSides(attackerParty, defenderParty, out var conflict))
                    {
                        return;
                    }
                    if (!conflict.ActiveWarTime)
                    {
                        return;
                    }
                    if (!claims.dataStorage.WarsTimes.TryGetValue(conflict.Guid, out var warTime))
                    {
                        return;
                    }
                    if (warTime.PlotAttacks.Count() >= claims.config.MAX_AMOUNT_OF_CAPTURE_FLAGS_ACTIVE)
                    {
                        return;
                    }
                    if (warTime.PlotAttacks.TryGetValue(currentPlotPosition, out var _))
                    {
                        return;
                    }

                    this.AllianceGuid = playerInfo.HasAlliance() ? playerInfo.Alliance.Guid : null;
                    this.CityGuid = playerInfo.City.Guid;
                    this.ConflictGuid = conflict.Guid;
                    this.PlayerGuid = playerInfo.Guid;
                    this.TimesToBreak = claims.config.FLAG_REINFORCEMENT_AMOUNT;
                    warTime.PlotAttacks.TryAdd(currentPlotPosition, new PlotAttack(this.Pos, this.PlayerGuid, DateTimeOffset.UtcNow.ToUnixTimeSeconds()));
                    this.updateRef = this.Api.Event.RegisterGameTickListener(this.Update, 1000);
                    if (this.Banner == null
                        && byPlayer.InventoryManager.ActiveHotbarSlot.CanTake()
                        && (byPlayer.InventoryManager.ActiveHotbarSlot.Itemstack?.Collectible.Code.Path.Contains("cloth-") ?? false)
                    )
                    {

                        this.Banner = byPlayer.InventoryManager.ActiveHotbarSlot.TakeOut(1);
                        this.Blockentity.MarkDirty();

                        if (this.Api is ICoreClientAPI client)
                        {

                            this.renderer?.Dispose();
                            client.Tesselator.TesselateShape(this.Banner.Item, Shape.TryGet(client, "claims:shapes/flag/banner.json"), out MeshData meshData);
                            this.renderer = new FlagRenderer(client, meshData, this.Pos, this, this.BlockBehavior.PoleTop, this.BlockBehavior.PoleBottom);
                            client.Event.RegisterRenderer(this.renderer, EnumRenderStage.Opaque, "flag");

                        }
                    }
                    if (claims.config.SEND_ANNOUNCEMENTS_PLOT_IN_UNDER_ATTACK)
                    {
                        StringBuilder sb = new();
                        if(claims.config.SEND_COORDS_OF_PLOT_IN_UNDER_ATTACK)
                        {
                            var localPos = PosFunctions.TranslateCoordsToLocalVec3d(this.Api, this.Pos);
                            sb.Append(Lang.Get("claims:our_city_is_under_attack_on_pos", defenderCity.GetPartName(), localPos.X, localPos.Y, localPos.Z));
                        }
                        else
                        {
                            sb.Append(Lang.Get("claims:our_city_is_under_attack", defenderCity.GetPartName()));
                        }

                        MessageHandler.SendMsgInAlliance(defenderParty, sb.ToString());
                    }
                }
            }
        }
        public void EndCapture()
        {
            if (this.captureRef.HasValue) this.Api.Event.UnregisterGameTickListener(this.captureRef.Value);
            this.captureRef = null;
        }
        public override void FromTreeAttributes(ITreeAttribute tree, IWorldAccessor worldForResolving)
        {
            this.CapturedPercent = tree.GetFloat("capturedPercent");
            if (this.Api is ICoreClientAPI client)
            {
                if (this.Banner == null)
                {
                    var newBanner = tree.GetItemstack("banner");
                    if (newBanner != null)
                    {
                        this.Banner = newBanner;
                        this.Banner?.ResolveBlockOrItem(this.Api.World);
                        this.renderer?.Dispose();
                        client.Tesselator.TesselateShape(this.Banner.Item, Shape.TryGet(client, "claims:shapes/flag/banner.json"), out MeshData meshData);
                        this.renderer = new FlagRenderer(client, meshData, this.Pos, this, this.BlockBehavior.PoleTop, this.BlockBehavior.PoleBottom);
                        client.Event.RegisterRenderer(this.renderer, EnumRenderStage.Opaque, "flag");
                        if (this.updateRef == null)
                        {
                            this.updateRef = this.Api.Event.RegisterGameTickListener(this.Update, 1000);
                        }
                    }
                    else
                    {
                        if (this.updateRef != null)
                        {
                            this.Api.Event.UnregisterGameTickListener(this.updateRef.Value);
                        }
                    }
                }
            }
            this.Banner = tree.GetItemstack("banner");
            this.TimesToBreak = tree.GetInt("TimesToBreak");
            base.FromTreeAttributes(tree, worldForResolving);

        }
        public override void ToTreeAttributes(ITreeAttribute tree)
        {

            if (this.Block != null) tree.SetString("forBlockCode", this.Block.Code.ToShortString());

            if (this.Banner != null) tree.SetItemstack("banner", this.Banner); else tree.RemoveAttribute("banner");

            tree.SetFloat("capturedPercent", this.CapturedPercent);

            tree.SetInt("TimesToBreak", this.TimesToBreak);

            base.ToTreeAttributes(tree);

        }
    }
}
