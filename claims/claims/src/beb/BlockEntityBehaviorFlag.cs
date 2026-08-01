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
using claims.src.part.structure.plots;
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
        // Purely informational, synced to clients so the block tooltip can explain what is going on
        // (clients know neither the cities behind the guids nor whether defenders are contesting).
        public string AttackerName { get; set; } = "";
        public string DefenderName { get; set; } = "";
        public bool Contested { get; set; }
        // Last CapturedPercent value pushed to clients — used to throttle MarkDirty network syncs.
        private float lastSyncedPercent = -1f;
        private bool lastSyncedContested;
        public BlockEntityBehaviorFlag(BlockEntity blockEntity) : base(blockEntity) { }

        public override void Initialize(ICoreAPI api, JsonObject properties)
        {
            base.Initialize(api, properties);

            this.BlockBehavior = this.Block.GetBehavior<BlockBehaviorFlag>();

            this.captureDuration = claims.config.FLAG_CAPTURE_DURATION_SECONDS;

            this.Banner?.ResolveBlockOrItem(this.Api.World);
            if (this.Banner != null && this.Api is ICoreClientAPI client)
            {
                RebuildRenderer(client);
            }

            //this.updateRef = api.Event.RegisterGameTickListener(this.Update, 1000);

        }
        /// <summary>
        /// Builds the banner mesh and hangs it on the pole.
        ///
        /// The cloth flies the arms of whoever planted it - the attacking city, or its alliance when
        /// the city has none of its own - so a defender can see at a glance who is storming the plot.
        /// Without arms (or before they reached this client) it falls back to the dyed cloth that was
        /// used to raise the flag, which is what it always looked like.
        /// </summary>
        private void RebuildRenderer(ICoreClientAPI client)
        {
            Shape shape = Shape.TryGet(client, "claims:shapes/flag/banner.json");
            if (shape == null) return;

            MeshData meshData;
            var arms = new renderer.EmblemTexSource(client, AttackerEmblem(), client.ItemTextureAtlas);
            if (arms.Resolved)
            {
                client.Tesselator.TesselateShape("captureflag", shape, out meshData, arms);
            }
            else
            {
                client.Tesselator.TesselateShape(this.Banner.Item, shape, out meshData);
            }

            this.renderer = new FlagRenderer(client, meshData, this.Pos, this, this.BlockBehavior.PoleTop, this.BlockBehavior.PoleBottom);
            client.Event.RegisterRenderer(this.renderer, EnumRenderStage.Opaque, "flag");
        }

        /// <summary>Arms of the attacking party, city first, empty when neither has any.</summary>
        private string AttackerEmblem()
        {
            string emblem = claims.clientDataStorage?.ClientGetEmblem(this.CityGuid) ?? "";
            if (emblem.Length == 0) emblem = claims.clientDataStorage?.ClientGetEmblem(this.AllianceGuid) ?? "";
            return emblem;
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

            bool captureRunning = !string.IsNullOrEmpty(this.AttackerName) || this.TimesToBreak > 0;
            if (!captureRunning)
            {
                // Placed but never activated (no war, wrong plot, ...) - explain what it is for.
                dsc.AppendLine(Lang.Get("claims:flag-info-idle"));
                return;
            }

            if (!string.IsNullOrEmpty(this.AttackerName) && !string.IsNullOrEmpty(this.DefenderName))
            {
                dsc.AppendLine(Lang.Get("claims:flag-info-parties", this.AttackerName, this.DefenderName));
            }

            int percent = (int)(GameMath.Clamp(this.CapturedPercent, 0f, 1f) * 100);
            float duration = claims.config.FLAG_CAPTURE_DURATION_SECONDS;
            if (duration <= 0) duration = 1;
            int secondsLeft = (int)System.Math.Ceiling((1f - GameMath.Clamp(this.CapturedPercent, 0f, 1f)) * duration);

            dsc.AppendLine(this.Contested
                ? Lang.Get("claims:flag-info-progress-contested", percent)
                : Lang.Get("claims:flag-info-progress", percent, secondsLeft));

            if (this.TimesToBreak > 0)
            {
                dsc.AppendLine(Lang.Get("claims:flag-info-breaks-left", this.TimesToBreak));
            }

            if (claims.config.WAR_FLAG_DEFENDER_INTERRUPT_ENABLED)
            {
                dsc.AppendLine(Lang.Get("claims:flag-info-defender-hint", claims.config.WAR_FLAG_DEFENDER_RADIUS));
            }
            dsc.AppendLine(Lang.Get("claims:flag-info-capture-hint"));
        }
        private void Update(float deltaTime)
        {
            if(Banner == null)
            {
                return;
            }
            // Read the capture duration live so /cadmin setcfg edits apply to in-progress captures.
            float dur = claims.config.FLAG_CAPTURE_DURATION_SECONDS;
            if (dur <= 0) dur = 1;

            if (this.Api.Side != EnumAppSide.Server)
            {
                // Progress is server-authoritative (it can now regress when defenders contest the
                // flag), so the client just renders the last CapturedPercent the server synced.
                return;
            }

            if (IsCaptureNoLongerValid())
            {
                CancelCapture();
                return;
            }

            float progressBefore = this.CapturedPercent;
            int defenders = 0, attackers = 0;
            if (claims.config.WAR_FLAG_DEFENDER_INTERRUPT_ENABLED) CountContestants(out attackers, out defenders);
            // Defenders contest the flag, but the attackers push through if they outnumber them.
            bool contested = defenders > 0 && attackers <= defenders;
            this.Contested = contested;
            if (contested)
            {
                this.CapturedPercent -= (float)claims.config.WAR_FLAG_REGRESS_MULTIPLIER * deltaTime / dur;
                if (this.CapturedPercent < 0f) this.CapturedPercent = 0f;
            }
            else
            {
                this.CapturedPercent += GameMath.Clamp(1.0f - this.CapturedPercent, -deltaTime / dur, deltaTime / dur);
            }

            // Push the authoritative progress to clients so the capture bar reflects regress too,
            // but throttle: only sync when the bar moved a visible amount or hit zero.
            if ((this.CapturedPercent != progressBefore
                && (System.Math.Abs(this.CapturedPercent - this.lastSyncedPercent) >= 0.02f
                    || (this.CapturedPercent <= 0f && this.lastSyncedPercent > 0f)))
                || contested != this.lastSyncedContested)
            {
                this.lastSyncedPercent = this.CapturedPercent;
                this.lastSyncedContested = contested;
                this.Blockentity.MarkDirty();
            }

            // War score for uncontested holding of an in-progress capture.
            GrantHoldScoreIfDue(defenders);

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

                        IConflictParty attackerParty = attackerCity.HasAlliance()
                            ? (IConflictParty)attackerCity.Alliance
                            : (IConflictParty)attackerCity;

                        if (claims.config.SEND_ANNOUNCEMENTS_PLOT_WAS_CAPTURED)
                        {
                            IConflictParty defenderParty = defenderCity.HasAlliance()
                                ? (IConflictParty)defenderCity.Alliance
                                : (IConflictParty)defenderCity;

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

                        foreach (var runningConflict in defenderCity.RunningConflicts.ToArray())
                        {
                            if (runningConflict.First.Equals(attackerParty))
                                runningConflict.State = ConflictState.FIRST_WON;
                            else if (runningConflict.Second.Equals(attackerParty))
                                runningConflict.State = ConflictState.SECOND_WON;
                            runningConflict.saveToDatabase();
                            PartDemolition.DemolishConflict(runningConflict, EnumConflictEndReason.CityDestroyed);
                        }

                        // Pillage the whole eligible share before the city (and its account) is gone.
                        WarPillageHelper.Pillage(attackerCity, defenderCity);
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
                        PlotTransferHelper.Transfer(defenderPlot, defenderCity, attackerCity, markCaptured: false);

                        UsefullPacketsSend.AddToQueueCityInfoUpdate(defenderCity.Guid, EnumPlayerRelatedInfo.CITY_LOG);
                        // One event per side: both were fired on the defender before, so the
                        // attackers' own city map never learned about the plot they had just taken.
                        defenderCity.FirePlotsMapChanged(EnumPlotsMapChangeReason.PlotLostToEnemy);
                        attackerCity.FirePlotsMapChanged(EnumPlotsMapChangeReason.PlotCapturedByUs);
                        // Treasury pillage + war score/stats for the successful plot capture.
                        long pillaged = WarPillageHelper.Pillage(attackerCity, defenderCity);
                        {
                            IConflictParty attackerPartyScore = attackerCity.HasAlliance() ? (IConflictParty)attackerCity.Alliance : (IConflictParty)attackerCity;
                            // Record stats BEFORE AddScore, since AddScore may end the war (and build the report).
                            WarScoreHelper.RecordPlotCapture(conflict, attackerPartyScore);
                            WarScoreHelper.RecordPillaged(conflict, attackerPartyScore, pillaged);
                            WarScoreHelper.AddScore(conflict, attackerPartyScore, claims.config.WAR_SCORE_PER_PLOT_CAPTURE, "plot_capture");
                        }
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
        // Counts online players within FLAG_DEFENDER_RADIUS of the flag, split into defenders
        // (the plot's owning party or its comrades) and attackers (the flag owner's party or its
        // comrades). Neutral players are ignored.
        private void CountContestants(out int attackers, out int defenders)
        {
            attackers = 0;
            defenders = 0;
            if (!claims.dataStorage.GetPlot(PlotPosition.fromBlockPos(this.Pos), out var plot)) return;
            City defenderCity = plot.getCity();
            if (defenderCity == null) return;
            if (!claims.dataStorage.getCityByGUID(this.CityGuid, out City attackerCity)) return;

            IConflictParty defenderParty = defenderCity.HasAlliance() ? (IConflictParty)defenderCity.Alliance : defenderCity;
            IConflictParty attackerParty = attackerCity.HasAlliance() ? (IConflictParty)attackerCity.Alliance : attackerCity;

            double radius = claims.config.WAR_FLAG_DEFENDER_RADIUS;
            double radiusSq = radius * radius;
            double fx = this.Pos.X + 0.5, fy = this.Pos.Y + 0.5, fz = this.Pos.Z + 0.5;

            foreach (var p in this.Api.World.AllOnlinePlayers)
            {
                if (p?.Entity == null) continue;
                double dx = p.Entity.ServerPos.X - fx, dy = p.Entity.ServerPos.Y - fy, dz = p.Entity.ServerPos.Z - fz;
                if (dx * dx + dy * dy + dz * dz > radiusSq) continue;
                if (!claims.dataStorage.GetPlayerByUid(p.PlayerUID, out var pInfo) || !pInfo.hasCity()) continue;

                IConflictParty pParty = pInfo.HasAlliance() ? (IConflictParty)pInfo.Alliance : pInfo.City;
                if (BelongsToSide(pParty, pInfo, defenderParty, defenderCity)) defenders++;
                else if (BelongsToSide(pParty, pInfo, attackerParty, attackerCity)) attackers++;
            }
        }

        private static bool BelongsToSide(IConflictParty pParty, PlayerInfo pInfo, IConflictParty sideParty, City sideCity)
        {
            if (pParty.Equals(sideParty)) return true;
            // Comrade alliances fight alongside the side.
            return sideCity.HasAlliance() && pInfo.HasAlliance()
                && sideCity.Alliance.ComradAlliancies.Contains(pInfo.Alliance);
        }

        // Grants a "holding" war-score tick to the attacking side while the capture is uncontested.
        private void GrantHoldScoreIfDue(int defenders)
        {
            if (!claims.config.WAR_SCORE_ENABLED || claims.config.WAR_SCORE_PER_HOLD_TICK <= 0) return;
            if (defenders > 0) return; // only reward uncontested holding
            if (!ConflictHandler.TryGetConflictByGuid(this.ConflictGuid, out var conflict)) return;
            if (!claims.dataStorage.WarsTimes.TryGetValue(conflict.Guid, out var warTime)) return;
            if (!warTime.PlotAttacks.TryGetValue(PlotPosition.fromBlockPos(this.Pos), out var plotAttack)) return;

            long now = DateTimeOffset.UtcNow.ToUnixTimeSeconds();
            int interval = claims.config.WAR_SCORE_HOLD_TICK_SECONDS;
            if (interval <= 0) interval = 1;
            if (now - plotAttack.LastHoldScoreTimestamp < interval) return;
            plotAttack.LastHoldScoreTimestamp = now;

            if (!claims.dataStorage.getCityByGUID(this.CityGuid, out City attackerCity)) return;
            IConflictParty attackerParty = attackerCity.HasAlliance() ? (IConflictParty)attackerCity.Alliance : attackerCity;
            WarScoreHelper.AddScore(conflict, attackerParty, claims.config.WAR_SCORE_PER_HOLD_TICK, "hold", announce: false);
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
            // Capture is only valid while the war battle window is open.
            if (!ConflictHandler.TryGetConflictByGuid(this.ConflictGuid, out var conflict) || !conflict.ActiveWarTime)
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
                    this.AttackerName = attackerParty.GetPartName();
                    this.DefenderName = defenderParty.GetPartName();
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
                            RebuildRenderer(client);
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
                        RebuildRenderer(client);
                        // No client-side tick: capture progress is server-authoritative and arrives
                        // via MarkDirty sync; the renderer reads CapturedPercent directly each frame.
                    }
                }
            }
            this.Banner = tree.GetItemstack("banner");
            this.TimesToBreak = tree.GetInt("TimesToBreak");
            this.AttackerName = tree.GetString("attackerName", "");
            this.DefenderName = tree.GetString("defenderName", "");
            this.Contested = tree.GetBool("contested");
            base.FromTreeAttributes(tree, worldForResolving);

        }
        public override void ToTreeAttributes(ITreeAttribute tree)
        {

            if (this.Block != null) tree.SetString("forBlockCode", this.Block.Code.ToShortString());

            if (this.Banner != null) tree.SetItemstack("banner", this.Banner); else tree.RemoveAttribute("banner");

            tree.SetFloat("capturedPercent", this.CapturedPercent);

            tree.SetInt("TimesToBreak", this.TimesToBreak);

            tree.SetString("attackerName", this.AttackerName ?? "");
            tree.SetString("defenderName", this.DefenderName ?? "");
            tree.SetBool("contested", this.Contested);

            base.ToTreeAttributes(tree);

        }
    }
}
