using System.Globalization;
using claims.src.gui.playerGui.Widgets;
using Vintagestory.API.Client;
using Vintagestory.API.Common;
using Vintagestory.API.Config;

namespace claims.src.gui.playerGui.Pages
{
    /// <summary>
    /// Start and end wars by hand, override the next battle window, and edit the war settings the
    /// server accepts at runtime.
    /// </summary>
    public sealed class AdminWarPage : AdminPageBase
    {
        private AdminPageState Admin => State.Admin;

        protected override void BuildAdminContent(PageBuildContext ctx)
        {
            var compo = ctx.Compo;

            // No page title of its own: the admin tab strip above already names the page, and the
            // window has no vertical room to spare.
            var currentBounds = ctx.Current.BelowCopy(0, 5);
            currentBounds.fixedWidth = ctx.Line.fixedWidth;
            currentBounds.WithAlignment(EnumDialogArea.LeftTop);

            // --- the two sides ---
            var firstBounds = currentBounds.FlatCopy().WithFixedSize(150, 28);
            compo.AddTextInput(firstBounds, v => Admin.RenameTo = v, null, "admin-war-first");
            compo.GetTextInput("admin-war-first").SetValue(Admin.RenameTo);

            var secondBounds = firstBounds.RightCopy(10).WithFixedSize(150, 28);
            compo.AddTextInput(secondBounds, v => Admin.PlayerName = v, null, "admin-war-second");
            compo.GetTextInput("admin-war-second").SetValue(Admin.PlayerName);

            var startBounds = AddCommand(compo, firstBounds.BelowCopy(0, 8).WithFixedHeight(28), "claims:gui-admin-force-start-war",
                () => "/cadmin startwar " + Admin.RenameTo + " " + Admin.PlayerName,
                "claims:gui-admin-force-start-war-tooltip");

            AddCommand(compo, startBounds.RightCopy(10), "claims:gui-admin-force-end-war",
                () => "/cadmin endwar " + Admin.RenameTo + " " + Admin.PlayerName,
                "claims:gui-admin-force-end-war-tooltip");

            // --- battle window override ---
            var overrideBounds = startBounds.BelowCopy(0, 12);
            compo.AddStaticText(Lang.Get("claims:gui-admin-override-battle"), CairoFont.WhiteDetailText(), overrideBounds.FlatCopy().WithFixedWidth(ctx.Line.fixedWidth));

            var startInBounds = overrideBounds.BelowCopy(0, 6).WithFixedSize(80, 28);
            compo.AddTextInput(startInBounds, v => Admin.BonusClaims = v, null, "admin-war-startin");
            compo.GetTextInput("admin-war-startin").SetValue(Admin.BonusClaims);

            var durationBounds = startInBounds.RightCopy(10).WithFixedSize(80, 28);
            compo.AddTextInput(durationBounds, v => Admin.CityFee = v, null, "admin-war-duration");
            compo.GetTextInput("admin-war-duration").SetValue(Admin.CityFee);

            AddCommand(compo, durationBounds.RightCopy(10), "claims:gui-admin-set-battle-date",
                () => "/cadmin setbattledate " + Admin.RenameTo + " " + Admin.PlayerName
                      + " " + Admin.BonusClaims + " " + Admin.CityFee,
                "claims:gui-admin-set-battle-date-tooltip");

            // --- active conflicts ---
            var conflictAnchor = AddActiveConflicts(ctx, compo, startInBounds);

            // --- runtime settings, one scrollable block ---
            BuildConfigList(ctx, compo, conflictAnchor);
        }

        /// <summary>
        /// Picking a conflict fills both party fields, so the commands above act on it without the
        /// admin retyping two names. Returns the anchor the settings list continues from.
        /// </summary>
        private ElementBounds AddActiveConflicts(PageBuildContext ctx, GuiComposer compo, ElementBounds anchor)
        {
            var labelBounds = anchor.BelowCopy(0, 10).WithFixedSize(ctx.Line.fixedWidth, 22);

            var conflicts = claims.clientDataStorage?.clientPlayerInfo?.CityInfo?.ClientConflictCellElements;
            if (conflicts == null || conflicts.Count == 0)
            {
                compo.AddStaticText(Lang.Get("claims:gui-admin-no-active-conflicts"), CairoFont.WhiteDetailText(), labelBounds);
                return labelBounds;
            }

            compo.AddStaticText(Lang.Get("claims:gui-admin-active-conflicts"), CairoFont.WhiteDetailText(), labelBounds);

            var values = new string[conflicts.Count];
            var names = new string[conflicts.Count];
            for (int i = 0; i < conflicts.Count; i++)
            {
                var c = conflicts[i];
                values[i] = i.ToString(CultureInfo.InvariantCulture);
                names[i] = c.FirstPartyName + " " + Lang.Get("claims:gui-admin-vs") + " " + c.SecondPartyName
                           + "  [" + c.FirstScore + ":" + c.SecondScore + "]";
            }

            var pickBounds = labelBounds.BelowCopy(0, 4).WithFixedSize(ctx.Line.fixedWidth - 40, 28);
            compo.AddDropDown(values, names, -1, (code, selected) =>
            {
                if (!selected) return;
                int index;
                if (!int.TryParse(code, NumberStyles.Integer, CultureInfo.InvariantCulture, out index)) return;
                if (index < 0 || index >= conflicts.Count) return;

                Admin.RenameTo = conflicts[index].FirstPartyName;
                Admin.PlayerName = conflicts[index].SecondPartyName;
                Gui.BuildMainWindow();
            }, pickBounds, "admin-war-conflict");
            compo.AddHoverText(Lang.Get("claims:gui-admin-use-tooltip"), CairoFont.SmallButtonText(), 250, pickBounds);

            return pickBounds;
        }

        private void BuildConfigList(PageBuildContext ctx, GuiComposer compo, ElementBounds anchor)
        {
            const int rowHeight = 30;

            // The edit row sits ABOVE the list. Below it the list would have to share the last
            // pixels of the window and both used to spill past the edge.
            //
            // The values in the list are read-only; editing goes through this one field plus one
            // button, because fifty inline inputs would not fit the dialog and every one of them
            // would be its own composer element rebuilt on each packet.
            var keyBounds = anchor.BelowCopy(0, 10).WithFixedSize(170, 28);
            compo.AddTextInput(keyBounds, v => Admin.ClaimRadius = v, null, "warcfg-key");
            compo.GetTextInput("warcfg-key").SetValue(Admin.ClaimRadius);
            compo.AddHoverText(Lang.Get("claims:gui-admin-warcfg-key-hint"), CairoFont.SmallButtonText(), 250, keyBounds);

            var valueBounds = keyBounds.RightCopy(10).WithFixedSize(90, 28);
            compo.AddTextInput(valueBounds, v => Admin.NewCityName = v, null, "warcfg-value");
            compo.GetTextInput("warcfg-value").SetValue(Admin.NewCityName);

            AddCommand(compo, valueBounds.RightCopy(10), "claims:gui-admin-warcfg-apply",
                () => "/cadmin setcfg " + Admin.ClaimRadius + " " + Admin.NewCityName, null);

            // What is left of the window belongs to the list. The reserve covers everything stacked
            // above it - strip, inputs, buttons, conflict picker, edit row - plus a bottom margin.
            double listHeight = Gui.mainBounds.fixedHeight - 449;

            // Built the same way the shared list widget builds its lists, and off the edit row rather
            // than off a bounds of its own: ElementBounds.Fixed(...).FixedUnder(...) had no parent, so
            // the whole block was laid out in the dialog's coordinates and covered the inputs above.
            ElementBounds listArea = keyBounds.BelowCopy(0, 8).WithFixedSize(ctx.Line.fixedWidth - 40, listHeight);
            ElementBounds clippingBounds = listArea.ForkBoundingParent();
            ElementBounds insetBounds = listArea.FlatCopy().FixedGrow(6).WithFixedOffset(-3, -3);
            ElementBounds scrollbarBounds = insetBounds.CopyOffsetedSibling(listArea.fixedWidth + 7).WithFixedWidth(20);
            ElementBounds clipInner = insetBounds.ForkContainingChild(3, 3, 3, 3);
            ElementBounds containerBounds = clipInner.ForkContainingChild(0, 0, 0, -3).WithFixedPadding(5);

            compo.BeginClip(clippingBounds)
                    .AddInset(insetBounds, 3)
                    .AddContainer(containerBounds, "warcfg-content")
                 .EndClip()
                 .AddVerticalScrollbar((value) =>
                 {
                     ElementBounds bounds = compo.GetContainer("warcfg-content").Bounds;
                     bounds.fixedY = 5 - value;
                     bounds.CalcWorldBounds();
                 }, scrollbarBounds, "warcfg-scrollbar");

            GuiElementContainer scrollArea = compo.GetContainer("warcfg-content");
            ElementBounds rowBounds = ElementBounds.Fixed(0, 0, listArea.fixedWidth - 30, rowHeight);

            int rendered = 0;
            foreach (var row in WarConfigTable.Rows)
            {
                var font = row.Kind == EnumCfgKind.Group
                    ? CairoFont.WhiteMediumText().WithFontSize(18)
                    : CairoFont.WhiteDetailText();

                string label = Lang.Get(row.LabelLangKey);
                if (row.Kind != EnumCfgKind.Group) label += ":  " + CurrentValue(row);

                scrollArea.Add(new GuiElementRichtext(compo.Api,
                    VtmlUtil.Richtextify(compo.Api, label, font), rowBounds));

                rowBounds = rowBounds.BelowCopy();
                rendered++;
            }

            ctx.AfterCompose(() =>
                compo.GetScrollbar("warcfg-scrollbar").SetHeights((float)clipInner.fixedHeight, rowHeight * rendered));
        }

        private static string CurrentValue(WarCfgRow row)
        {
            switch (row.Kind)
            {
                case EnumCfgKind.Flag: return row.GetFlag() ? "on" : "off";
                case EnumCfgKind.Int: return row.GetInt().ToString(CultureInfo.InvariantCulture);
                case EnumCfgKind.Double: return row.GetDouble().ToString(CultureInfo.InvariantCulture);
                default: return "";
            }
        }

        /// <summary>
        /// A command button sized to its own caption - a fixed width clipped the longer translations
        /// and made neighbouring captions overlap. Returns the bounds actually used.
        /// </summary>
        private ElementBounds AddCommand(GuiComposer compo, ElementBounds bounds, string labelKey, System.Func<string> command, string tooltipKey)
        {
            string caption = Lang.Get(labelKey);
            var target = bounds.FlatCopy().WithFixedWidth(ButtonWidth(caption));

            compo.AddButton(caption, new ActionConsumable(() =>
            {
                Send(command());
                return true;
            }), target, EnumButtonStyle.Normal);

            if (tooltipKey != null)
            {
                compo.AddHoverText(Lang.Get(tooltipKey), CairoFont.SmallButtonText(), 250, target);
            }

            return target;
        }
    }
}
