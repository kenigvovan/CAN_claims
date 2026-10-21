using System.Collections.Generic;
using System.Linq;
using claims.src.part;
using claims.src.subplots;
using claims.src.subplots.geometry;
using Vintagestory.API.Client;
using Vintagestory.API.Common;
using Vintagestory.API.Config;
using Vintagestory.API.MathTools;
using Vintagestory.API.Server;

namespace claims.src.commands
{
    /// <summary>
    /// /subplot: two corners marked on the blocks the player looks at make a box, boxes make a
    /// sub-plot. The rules live in <see cref="SubPlotEditor"/>; this resolves the arguments.
    ///
    /// The sub-plot's name is the last argument and may be left out: then it is the one the player
    /// looks at, the one they stand in, or the one they last picked with the wand, in that order.
    /// </summary>
    public class SubPlotCommand : BaseCommand
    {
        // Per player, until the server stops; two corners, either may be missing.
        private static readonly Dictionary<string, BlockPos[]> selections = new Dictionary<string, BlockPos[]>();
        // Per player: the sub-plot picked with the wand, by guid.
        private static readonly Dictionary<string, string> picked = new Dictionary<string, string>();

        public static TextCommandResult Pos1(TextCommandCallingArgs args) => MarkCorner(args, 0);
        public static TextCommandResult Pos2(TextCommandCallingArgs args) => MarkCorner(args, 1);

        private static TextCommandResult MarkCorner(TextCommandCallingArgs args, int corner)
        {
            if (args.Caller.Player is not IServerPlayer player) return TextCommandResult.Error("");
            // The block looked at; without one, the block the player stands in.
            BlockPos pos = player.CurrentBlockSelection?.Position?.Copy() ?? player.Entity.Pos.AsBlockPos;
            return SetCorner(player, corner, pos);
        }

        // Highlight slot of the selection; vanilla uses the low numbers.
        private const int HighlightSlot = 47;
        // HighlightBlocks takes ColorFromRgba order (red in the low byte), as the vanilla WorldEdit markers;
        // ToRgba puts red in bits 16-23 and would show red as blue.
        private static readonly List<int> FitsColor = new List<int> { ColorUtil.ColorFromRgba(90, 230, 110, 60) };
        private static readonly List<int> RefusedColor = new List<int> { ColorUtil.ColorFromRgba(230, 80, 70, 60) };

        /// <summary>What the marked box is for, so the highlight judges it the way it will be used.</summary>
        public enum CornerPurpose
        {
            /// <summary>A new sub-plot: /subplot create, the wand's corner mode.</summary>
            Create,
            /// <summary>Into the picked sub-plot: the wand's add mode.</summary>
            Add,
            /// <summary>Out of the picked sub-plot: the wand's cut mode.</summary>
            Cut
        }

        /// <summary>
        /// Corner 0 or 1 of the player's selection, from /subplot pos1|pos2 or the wand. With both
        /// set, says how big the box is and whether it can be used for <paramref name="purpose"/> -
        /// green or red, with the reason.
        /// </summary>
        public static TextCommandResult SetCorner(IServerPlayer player, int corner, BlockPos pos,
                                                  CornerPurpose purpose = CornerPurpose.Create)
        {
            if (!selections.TryGetValue(player.PlayerUID, out BlockPos[] corners))
            {
                corners = new BlockPos[2];
                selections[player.PlayerUID] = corners;
            }
            corners[corner] = pos.Copy();

            if (corners[0] == null || corners[1] == null)
            {
                Highlight(player, BlockBox.FromCorners(pos, pos), true);
                return SuccessWithParams("claims:subplot_pos_set", new object[] { corner + 1, pos.X, pos.Y, pos.Z });
            }

            BlockBox box = BlockBox.FromCorners(corners[0], corners[1]);
            string verdict = Verdict(player, box, purpose, out bool fits);
            Highlight(player, box, fits);
            return SuccessWithParams("claims:subplot_box_marked", new object[]
            {
                corner + 1, box.SizeX, box.SizeY, box.SizeZ, box.Volume, verdict
            });
        }

        /// <summary>
        /// Whether the box can be used for <paramref name="purpose"/>, as text for the player: what it
        /// would do, or why it cannot. Judged as the command will - rights, then the shape's rules.
        /// </summary>
        private static string Verdict(IServerPlayer player, BlockBox box, CornerPurpose purpose, out bool fits)
        {
            fits = false;
            if (!claims.dataStorage.GetPlayerByUid(player.PlayerUID, out PlayerInfo info) || !info.hasCity())
            {
                return Lang.Get("claims:you_dont_have_city");
            }
            if (!SubPlotEditor.CanManage(info, info.City)) return Lang.Get("claims:you_dont_have_right_for_that_command");

            if (purpose == CornerPurpose.Create)
            {
                string problem = SubPlotEditor.Validate(new BoxShape(box), info.City, null);
                fits = problem == null;
                return Lang.Get(fits ? "claims:subplot_box_fits" : problem);
            }

            SubPlot target = Picked(player, info);
            if (target == null) return Lang.Get("claims:subplot_pick_first");
            bool add = purpose == CornerPurpose.Add;
            string why = SubPlotEditor.WhyNotChange(target, box, add);
            fits = why == null;
            return fits
                ? Lang.Get(add ? "claims:subplot_box_fits_add" : "claims:subplot_box_fits_cut", target.GetPartName())
                : Lang.Get(why, target.GetPartName());
        }

        /// <summary>The box as a translucent cube, from its first block to its far corner outside.</summary>
        private static void Highlight(IServerPlayer player, BlockBox box, bool fits)
        {
            claims.sapi.World.HighlightBlocks(player, HighlightSlot,
                new List<BlockPos> { new BlockPos(box.MinX, box.MinY, box.MinZ), new BlockPos(box.MaxX, box.MaxY, box.MaxZ) },
                fits ? FitsColor : RefusedColor, EnumHighlightBlocksMode.Absolute, EnumHighlightShape.Cube);
        }

        /// <summary>/subplot clearsel: forgets the corners and takes the highlight away.</summary>
        public static TextCommandResult ClearSelection(TextCommandCallingArgs args)
        {
            if (args.Caller.Player is not IServerPlayer player) return TextCommandResult.Error("");
            ForgetSelection(player);
            return TextCommandResult.Success("claims:subplot_selection_cleared");
        }

        private static void ForgetSelection(IServerPlayer player)
        {
            selections.Remove(player.PlayerUID);
            claims.sapi.World.HighlightBlocks(player, HighlightSlot, new List<BlockPos>());
        }

        /// <summary>/subplot wand: hands the player the marking wand.</summary>
        public static TextCommandResult GiveWand(TextCommandCallingArgs args)
        {
            if (args.Caller.Player is not IServerPlayer player) return TextCommandResult.Error("");
            Item wand = claims.sapi.World.GetItem(new AssetLocation("claims:subplotwand"));
            if (wand == null) return TextCommandResult.Error("");
            var stack = new ItemStack(wand);
            if (!player.InventoryManager.TryGiveItemstack(stack, true))
            {
                claims.sapi.World.SpawnItemEntity(stack, player.Entity.Pos.XYZ);
            }
            return TextCommandResult.Success("claims:subplot_wand_given");
        }

        private static bool TrySelection(IServerPlayer player, out BlockBox box)
        {
            box = default;
            if (!selections.TryGetValue(player.PlayerUID, out BlockPos[] corners) || corners[0] == null || corners[1] == null) return false;
            box = BlockBox.FromCorners(corners[0], corners[1]);
            return true;
        }

        // ---- the wand's other modes ---------------------------------------------------------

        /// <summary>Picks the sub-plot holding this block for the commands that follow without a name.</summary>
        public static TextCommandResult PickAt(IServerPlayer player, BlockPos pos)
        {
            if (!claims.dataStorage.TryGetSubPlotAt(pos.X, pos.Y, pos.Z, out SubPlot subPlot))
            {
                return TextCommandResult.Error("claims:subplot_none_here");
            }
            picked[player.PlayerUID] = subPlot.Guid;
            return TextCommandResult.Success(Lang.Get("claims:subplot_picked", subPlot.GetPartName()) + "\n" + SubPlotEditor.Describe(subPlot));
        }

        /// <summary>
        /// The wand in add or cut mode, once both corners are set: the marked box goes into the
        /// picked sub-plot, or out of it. The corners are forgotten after a change that went through.
        /// </summary>
        public static TextCommandResult ApplyBox(IServerPlayer player, bool add)
        {
            if (!claims.dataStorage.GetPlayerByUid(player.PlayerUID, out PlayerInfo info)) return TextCommandResult.Error("");
            if (!TrySelection(player, out BlockBox box)) return TextCommandResult.Error("claims:subplot_no_selection");
            SubPlot subPlot = Picked(player, info);
            if (subPlot == null) return TextCommandResult.Error("claims:subplot_pick_first");
            TextCommandResult result = add ? SubPlotEditor.AddBox(info, subPlot, box) : SubPlotEditor.CutBox(info, subPlot, box);
            if (result.Status == EnumCommandStatus.Success) ForgetSelection(player);
            return result;
        }

        private static SubPlot Picked(IServerPlayer player, PlayerInfo info)
        {
            if (!picked.TryGetValue(player.PlayerUID, out string guid)) return null;
            // Gone, or of a city the player has left since.
            if (!claims.dataStorage.TryGetSubPlot(guid, out SubPlot subPlot) || !info.hasCity() || !info.City.Equals(subPlot.City))
            {
                picked.Remove(player.PlayerUID);
                return null;
            }
            return subPlot;
        }

        // ---- commands ------------------------------------------------------------------------

        public static TextCommandResult Create(TextCommandCallingArgs args)
        {
            if (!TryResolveCaller(args, out var player, out var playerInfo, out var err)) return err;
            if (!playerInfo.hasCity()) return TextCommandResult.Error("claims:you_dont_have_city");
            if (!TrySelection(player, out BlockBox box)) return TextCommandResult.Error("claims:subplot_no_selection");
            TextCommandResult result = SubPlotEditor.Create(playerInfo, (string)args.Parsers[0].GetValue(), box);
            if (result.Status == EnumCommandStatus.Success)
            {
                ForgetSelection(player);
                // The new one is what the next box goes into.
                SubPlot made = SubPlotEditor.FindByName(playerInfo.City, auxialiry.Filter.filterName((string)args.Parsers[0].GetValue()));
                if (made != null) picked[player.PlayerUID] = made.Guid;
            }
            return result;
        }

        public static TextCommandResult AddBox(TextCommandCallingArgs args) => WithSelection(args, SubPlotEditor.AddBox);
        public static TextCommandResult CutBox(TextCommandCallingArgs args) => WithSelection(args, SubPlotEditor.CutBox);

        private static TextCommandResult WithSelection(TextCommandCallingArgs args,
            System.Func<PlayerInfo, SubPlot, BlockBox, TextCommandResult> edit)
        {
            if (!TryResolveTarget(args, 0, out var player, out var playerInfo, out var subPlot, out var err)) return err;
            if (!TrySelection(player, out BlockBox box)) return TextCommandResult.Error("claims:subplot_no_selection");
            TextCommandResult result = edit(playerInfo, subPlot, box);
            if (result.Status == EnumCommandStatus.Success) ForgetSelection(player);
            return result;
        }

        public static TextCommandResult Delete(TextCommandCallingArgs args)
        {
            if (!TryResolveTarget(args, 0, out _, out var playerInfo, out var subPlot, out var err)) return err;
            return SubPlotEditor.Delete(playerInfo, subPlot);
        }

        /// <summary>/subplot setowner &lt;player&gt; [name]</summary>
        public static TextCommandResult SetOwner(TextCommandCallingArgs args)
        {
            if (!TryResolveTarget(args, 1, out _, out var playerInfo, out var subPlot, out var err)) return err;
            if (!claims.dataStorage.getPlayerByName((string)args.Parsers[0].GetValue(), out PlayerInfo owner))
            {
                return TextCommandResult.Error("claims:no_such_player");
            }
            TextCommandResult result = SubPlotEditor.SetOwner(playerInfo, subPlot, owner);
            if (result.Status == EnumCommandStatus.Success)
            {
                messages.MessageHandler.sendMsgToPlayerInfo(owner, Lang.Get("claims:subplot_given_to_you", subPlot.GetPartName()));
            }
            return result;
        }

        public static TextCommandResult ClearOwner(TextCommandCallingArgs args)
        {
            if (!TryResolveTarget(args, 0, out _, out var playerInfo, out var subPlot, out var err)) return err;
            return SubPlotEditor.SetOwner(playerInfo, subPlot, null);
        }

        public static TextCommandResult AddMember(TextCommandCallingArgs args) => WithPlayer(args, SubPlotEditor.AddMember);
        public static TextCommandResult RemoveMember(TextCommandCallingArgs args) => WithPlayer(args, SubPlotEditor.RemoveMember);

        /// <summary>/subplot addmember|removemember &lt;player&gt; [name]</summary>
        private static TextCommandResult WithPlayer(TextCommandCallingArgs args,
            System.Func<PlayerInfo, SubPlot, PlayerInfo, TextCommandResult> edit)
        {
            if (!TryResolveTarget(args, 1, out _, out var playerInfo, out var subPlot, out var err)) return err;
            claims.dataStorage.getPlayerByName((string)args.Parsers[0].GetValue(), out PlayerInfo other);
            return edit(playerInfo, subPlot, other);
        }

        /// <summary>/subplot perm &lt;group&gt; &lt;type&gt; &lt;on|off&gt; [name]</summary>
        public static TextCommandResult SetPerm(TextCommandCallingArgs args)
        {
            if (!TryResolveTarget(args, 3, out _, out var playerInfo, out var subPlot, out var err)) return err;
            return SubPlotEditor.SetPerm(playerInfo, subPlot, (string)args.Parsers[0].GetValue(),
                (string)args.Parsers[1].GetValue(), (string)args.Parsers[2].GetValue());
        }

        /// <summary>/subplot group &lt;plots group&gt; [name]</summary>
        public static TextCommandResult SetGroup(TextCommandCallingArgs args)
        {
            if (!TryResolveTarget(args, 1, out _, out var playerInfo, out var subPlot, out var err)) return err;
            return SubPlotEditor.SetGroup(playerInfo, subPlot, (string)args.Parsers[0].GetValue());
        }

        /// <summary>/subplot ungroup [name]</summary>
        public static TextCommandResult ClearGroup(TextCommandCallingArgs args)
        {
            if (!TryResolveTarget(args, 0, out _, out var playerInfo, out var subPlot, out var err)) return err;
            return SubPlotEditor.SetGroup(playerInfo, subPlot, null);
        }

        /// <summary>/subplot preset &lt;private|shop|open&gt; [name]</summary>
        public static TextCommandResult ApplyPreset(TextCommandCallingArgs args)
        {
            if (!TryResolveTarget(args, 1, out _, out var playerInfo, out var subPlot, out var err)) return err;
            return SubPlotEditor.ApplyPreset(playerInfo, subPlot, (string)args.Parsers[0].GetValue());
        }

        /// <summary>/subplot fs &lt;price&gt; [name]</summary>
        public static TextCommandResult SetForSale(TextCommandCallingArgs args)
        {
            if (!TryResolveTarget(args, 1, out _, out var playerInfo, out var subPlot, out var err)) return err;
            return SubPlotEditor.SetForSale(playerInfo, subPlot, (int)args.Parsers[0].GetValue());
        }

        public static TextCommandResult SetNotForSale(TextCommandCallingArgs args)
        {
            if (!TryResolveTarget(args, 0, out _, out var playerInfo, out var subPlot, out var err)) return err;
            return SubPlotEditor.SetForSale(playerInfo, subPlot, -1);
        }

        /// <summary>
        /// /subplot buy [name] [price tax]. With the terms the buyer was shown - the GUI's confirm
        /// sends them - it buys at once if they still hold. Typed by hand, it asks for /agree at the
        /// terms of the moment; either may change before the answer.
        /// </summary>
        public static TextCommandResult Buy(TextCommandCallingArgs args)
        {
            if (!TryResolveTarget(args, 0, out var player, out var playerInfo, out var subPlot, out var err)) return err;
            TextCommandResult refusal = SubPlotEditor.CheckCanBuy(playerInfo, subPlot);
            if (refusal != null) return refusal;

            if (HasArg(args, 1) && HasArg(args, 2))
            {
                double shownPrice = System.Convert.ToDouble(args.Parsers[1].GetValue());
                double shownTax = System.Convert.ToDouble(args.Parsers[2].GetValue());
                return System.Math.Abs(subPlot.Price - shownPrice) > 0.001 || System.Math.Abs(subPlot.Tax - shownTax) > 0.001
                    ? TextCommandResult.Error("claims:subplot_offer_changed")
                    : SubPlotEditor.Buy(playerInfo, subPlot);
            }

            double offeredPrice = subPlot.Price;
            double offeredTax = subPlot.Tax;
            agreement.AgreementHandler.addNewAgreementOrReplace(new agreement.Agreement(() =>
            {
                TextCommandResult result = !claims.dataStorage.TryGetSubPlot(subPlot.Guid, out _)
                    || subPlot.Price != offeredPrice || subPlot.Tax != offeredTax
                    ? TextCommandResult.Error("claims:subplot_offer_changed")
                    : SubPlotEditor.Buy(playerInfo, subPlot);
                messages.MessageHandler.sendMsgToPlayer(player, Lang.Get(result.StatusMessage, result.MessageParams ?? new object[0]));
            }, player.PlayerUID));
            return SuccessWithParams("claims:subplot_buy_ask",
                new object[] { subPlot.GetPartName(), offeredPrice, offeredTax, claims.config.AGREEMENT_COMMAND });
        }

        public static TextCommandResult GiveBack(TextCommandCallingArgs args)
        {
            if (!TryResolveTarget(args, 0, out _, out var playerInfo, out var subPlot, out var err)) return err;
            return SubPlotEditor.GiveBack(playerInfo, subPlot);
        }

        /// <summary>/subplot tax &lt;amount&gt; [name]</summary>
        public static TextCommandResult SetTax(TextCommandCallingArgs args)
        {
            if (!TryResolveTarget(args, 1, out _, out var playerInfo, out var subPlot, out var err)) return err;
            return SubPlotTaxHelper.SetTax(playerInfo, subPlot, System.Convert.ToDouble(args.Parsers[0].GetValue()),
                auxialiry.TimeFunctions.getEpochSeconds());
        }

        public static TextCommandResult AcceptTax(TextCommandCallingArgs args)
        {
            if (!TryResolveTarget(args, 0, out _, out var playerInfo, out var subPlot, out var err)) return err;
            return SubPlotTaxHelper.Accept(playerInfo, subPlot);
        }

        public static TextCommandResult Info(TextCommandCallingArgs args)
        {
            if (!TryResolveTarget(args, 0, out _, out _, out var subPlot, out var err, ownCityOnly: false)) return err;
            return TextCommandResult.Success(SubPlotEditor.Describe(subPlot));
        }

        public static TextCommandResult List(TextCommandCallingArgs args)
        {
            if (!TryResolveCaller(args, out _, out var playerInfo, out var err)) return err;
            if (!playerInfo.hasCity()) return TextCommandResult.Error("claims:you_dont_have_city");
            var names = SubPlotEditor.OfCity(playerInfo.City)
                .Select(s => s.GetPartName() + (s.Owner != null ? " (" + s.Owner.GetPartName() + ")" : ""))
                .ToList();
            if (names.Count == 0) return TextCommandResult.Success("claims:subplot_list_empty");
            return SuccessWithParams("claims:subplot_list", new object[] { string.Join(", ", names) });
        }

        /// <summary>
        /// The sub-plot a command is about: named by the argument at <paramref name="nameIndex"/>
        /// (in the caller's city), or else the one looked at, stood in, or picked with the wand.
        /// </summary>
        private static bool TryResolveTarget(TextCommandCallingArgs args, int nameIndex, out IServerPlayer player,
                                             out PlayerInfo playerInfo, out SubPlot subPlot, out TextCommandResult err,
                                             bool ownCityOnly = true)
        {
            subPlot = null;
            if (!TryResolveCaller(args, out player, out playerInfo, out err)) return false;
            if (ownCityOnly && !playerInfo.hasCity())
            {
                err = TextCommandResult.Error("claims:you_dont_have_city");
                return false;
            }

            if (HasArg(args, nameIndex))
            {
                string name = (string)args.Parsers[nameIndex].GetValue();
                subPlot = SubPlotEditor.FindByName(playerInfo.City, name);
                if (subPlot == null) err = ErrorWithParams("claims:subplot_not_found", new object[] { name });
                return subPlot != null;
            }

            subPlot = Around(player) ?? Picked(player, playerInfo);
            if (subPlot == null)
            {
                err = TextCommandResult.Error("claims:subplot_none_here");
                return false;
            }
            return true;
        }

        /// <summary>The sub-plot at the block looked at, or else at the player's feet.</summary>
        private static SubPlot Around(IServerPlayer player)
        {
            BlockPos look = player.CurrentBlockSelection?.Position;
            if (look != null && claims.dataStorage.TryGetSubPlotAt(look.X, look.Y, look.Z, out SubPlot looked)) return looked;
            BlockPos feet = player.Entity.Pos.AsBlockPos;
            return claims.dataStorage.TryGetSubPlotAt(feet.X, feet.Y, feet.Z, out SubPlot stood) ? stood : null;
        }
    }
}
