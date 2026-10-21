using claims.src.commands;
using claims.src.messages;
using Vintagestory.API.Client;
using Vintagestory.API.Common;
using Vintagestory.API.Config;
using Vintagestory.API.MathTools;
using Vintagestory.API.Server;

namespace claims.src.items
{
    /// <summary>
    /// The sub-plot wand, with four modes on the tool-mode wheel:
    /// - corners: left click the first, right click the second - the selection /subplot pos1|pos2 make;
    /// - add / cut: the same two corners, and the second one puts the box into the picked sub-plot or
    ///   takes it out;
    /// - pick: a click on a block inside a sub-plot makes it the one commands without a name mean.
    /// Breaks and places nothing. The client only stops the default action; the server does the rest.
    /// </summary>
    public class ItemSubPlotWand : Item
    {
        private const int ModeCorners = 0, ModeAdd = 1, ModeCut = 2, ModePick = 3;
        private static readonly (string Code, string Icon)[] Modes =
        {
            ("corners", "convergence-target"), ("add", "expander"), ("cut", "contract"), ("pick", "magnifying-glass")
        };

        private SkillItem[] toolModes;

        public override void OnLoaded(ICoreAPI api)
        {
            base.OnLoaded(api);
            // A right click goes to a door or a chest before the held item; with sneak held, this
            // makes the wand go first, so such a block can be a corner too. Not read from JSON.
            HeldPriorityInteract = true;

            if (api is not ICoreClientAPI capi) return;
            toolModes = new SkillItem[Modes.Length];
            for (int i = 0; i < Modes.Length; i++)
            {
                toolModes[i] = new SkillItem
                {
                    Code = new AssetLocation(Modes[i].Code),
                    Name = Lang.Get("claims:wandmode-" + Modes[i].Code)
                }.WithIcon(capi, capi.Gui.LoadSvgWithPadding(new AssetLocation("claims:textures/icons/" + Modes[i].Icon + ".svg"),
                                                               48, 48, 5, ColorUtil.WhiteArgb));
            }
        }

        public override void OnUnloaded(ICoreAPI api)
        {
            if (toolModes != null)
            {
                foreach (SkillItem mode in toolModes) mode?.Dispose();
            }
            base.OnUnloaded(api);
        }

        public override SkillItem[] GetToolModes(ItemSlot slot, IClientPlayer forPlayer, BlockSelection blockSel) => toolModes;

        public override int GetToolMode(ItemSlot slot, IPlayer byPlayer, BlockSelection blockSelection) =>
            slot.Itemstack?.Attributes.GetInt("toolMode", ModeCorners) ?? ModeCorners;

        public override void SetToolMode(ItemSlot slot, IPlayer byPlayer, BlockSelection blockSelection, int toolMode)
        {
            slot.Itemstack?.Attributes.SetInt("toolMode", toolMode);
        }

        public override void OnHeldAttackStart(ItemSlot slot, EntityAgent byEntity, BlockSelection blockSel,
                                               EntitySelection entitySel, ref EnumHandHandling handling)
        {
            if (blockSel == null) return;
            handling = EnumHandHandling.PreventDefaultAction;
            Use(slot, byEntity, blockSel, 0);
        }

        public override void OnHeldInteractStart(ItemSlot slot, EntityAgent byEntity, BlockSelection blockSel,
                                                 EntitySelection entitySel, bool firstEvent, ref EnumHandHandling handling)
        {
            if (blockSel == null || !firstEvent) return;
            handling = EnumHandHandling.PreventDefaultAction;
            Use(slot, byEntity, blockSel, 1);
        }

        // A held left click keeps hitting: it must not wear the block down meanwhile.
        public override bool OnHeldAttackStep(float secondsPassed, ItemSlot slot, EntityAgent byEntity,
                                              BlockSelection blockSelection, EntitySelection entitySel) => false;

        private void Use(ItemSlot slot, EntityAgent byEntity, BlockSelection blockSel, int button)
        {
            if (byEntity.World.Side != EnumAppSide.Server) return;
            if ((byEntity as EntityPlayer)?.Player is not IServerPlayer player) return;

            int mode = GetToolMode(slot, player, blockSel);
            if (mode == ModePick)
            {
                Tell(player, SubPlotCommand.PickAt(player, blockSel.Position));
                return;
            }
            var purpose = mode == ModeAdd ? SubPlotCommand.CornerPurpose.Add
                        : mode == ModeCut ? SubPlotCommand.CornerPurpose.Cut
                        : SubPlotCommand.CornerPurpose.Create;
            Tell(player, SubPlotCommand.SetCorner(player, button, blockSel.Position, purpose));
            // Add and cut act once the second corner is down.
            if (button == 1 && (mode == ModeAdd || mode == ModeCut))
            {
                Tell(player, SubPlotCommand.ApplyBox(player, mode == ModeAdd));
            }
        }

        private static void Tell(IServerPlayer player, TextCommandResult result)
        {
            if (string.IsNullOrEmpty(result.StatusMessage)) return;
            MessageHandler.sendMsgToPlayer(player, Lang.Get(result.StatusMessage, result.MessageParams ?? new object[0]));
        }

        public override WorldInteraction[] GetHeldInteractionHelp(ItemSlot inSlot)
        {
            return new[]
            {
                new WorldInteraction { ActionLangCode = "claims:heldhelp-subplot-corner1", MouseButton = EnumMouseButton.Left },
                new WorldInteraction { ActionLangCode = "claims:heldhelp-subplot-corner2", MouseButton = EnumMouseButton.Right },
                new WorldInteraction { ActionLangCode = "claims:heldhelp-subplot-corner2-usable", MouseButton = EnumMouseButton.Right, HotKeyCode = "shift" },
                new WorldInteraction { ActionLangCode = "claims:heldhelp-subplot-modes", HotKeyCode = "toolmodeselect" },
            };
        }
    }
}
