using System;
using System.Linq;
using claims.src.gui.playerGui.Widgets;
using claims.src.network.packets;
using Vintagestory.API.Client;
using Vintagestory.API.Common;
using Vintagestory.API.Config;

namespace claims.src.gui.playerGui.Pages
{
    /// <summary>
    /// Admin operations on any city: create, rename, hand out membership, adjust claims and fee,
    /// flip its flags, delete it.
    /// </summary>
    public sealed class AdminCitiesPage : AdminPageBase
    {
        private AdminPageState Admin => State.Admin;

        protected override void BuildAdminContent(PageBuildContext ctx)
        {
            var compo = ctx.Compo;

            // No page title of its own: the admin tab strip already names the page, and every
            // vertical pixel is needed - the old layout ran past the window bottom.
            var currentBounds = ctx.Current.BelowCopy(0, 5);
            currentBounds.fixedWidth = ctx.Line.fixedWidth;
            currentBounds.WithAlignment(EnumDialogArea.LeftTop);

            // --- create a city ---
            currentBounds = currentBounds.FlatCopy().WithFixedSize(200, 28);
            compo.AddTextInput(currentBounds, v => Admin.NewCityName = v, null, "admin-new-city");
            compo.GetTextInput("admin-new-city").SetValue(Admin.NewCityName);

            var createRow = new ButtonRow(compo, currentBounds, ctx.Line.fixedWidth, startX: 210);
            createRow.Add(Lang.Get("claims:gui-admin-create"), () =>
            {
                if (Admin.NewCityName.Length == 0) return true;
                Send("/cadmin city new " + Admin.NewCityName);
                Admin.NewCityName = "";
                Gui.BuildMainWindow();
                return true;
            }, Lang.Get("claims:gui-admin-create-tooltip"));

            createRow.Add(Lang.Get("claims:gui-admin-refresh"), () =>
            {
                claims.clientChannel.SendPacket(new SavedPlotsPacket { type = PacketsContentEnum.ADMIN_REQUEST_CITY_FLAGS });
                return true;
            }, Lang.Get("claims:gui-admin-refresh-cities-tooltip"));

            // --- pick a city ---
            var allCityNames = AdminClientState.CityFlags.Keys.OrderBy(n => n).ToArray();
            currentBounds = createRow.Bounds.BelowCopy(0, 8).WithFixedSize(200, 28);

            if (allCityNames.Length == 0)
            {
                compo.AddStaticText(Lang.Get("claims:gui-admin-no-city-data"), CairoFont.WhiteDetailText(), currentBounds);
                return;
            }

            // The dropdown alone is unusable once a server has a few dozen cities, so it is narrowed
            // by a filter. Applying it rebuilds the window: a text input cannot do that per keystroke
            // without losing focus.
            string filter = (Admin.CityFilter ?? "").ToLowerInvariant();
            var cityNames = filter.Length == 0
                ? allCityNames
                : allCityNames.Where(n => n.ToLowerInvariant().Contains(filter)).ToArray();

            var filterInput = currentBounds.RightCopy(10).WithFixedSize(140, 28);
            compo.AddTextInput(filterInput, v => Admin.CityFilter = v, null, "admin-city-filter");
            compo.GetTextInput("admin-city-filter").SetValue(Admin.CityFilter ?? "");
            Tooltip.Add(compo, Lang.Get("claims:gui-admin-filter"), filterInput, "tip-admin-city-filter");

            var filterRow = new ButtonRow(compo, filterInput, ctx.Line.fixedWidth - filterInput.fixedX, startX: 150);
            filterRow.Add(Lang.Get("claims:gui-admin-filter"), () =>
            {
                // Selecting a city that the new filter hides would leave the page acting on an
                // invisible entry, so the pick is cleared when it falls out of the list.
                if (Admin.CityFilter?.Length > 0 && Admin.SelectedCity?.Length > 0 &&
                    !Admin.SelectedCity.ToLowerInvariant().Contains(Admin.CityFilter.ToLowerInvariant()))
                {
                    Admin.SelectedCity = "";
                }
                Gui.BuildMainWindow();
                return true;
            });

            if (cityNames.Length == 0)
            {
                compo.AddStaticText(Lang.Get("claims:gui-admin-no-cities-match"), CairoFont.WhiteDetailText(),
                    currentBounds.BelowCopy(0, 10).WithFixedWidth(ctx.Line.fixedWidth));
                return;
            }

            compo.AddDropDown(cityNames, cityNames, Math.Max(0, Array.IndexOf(cityNames, Admin.SelectedCity)),
                (code, selected) =>
                {
                    Admin.SelectedCity = code;
                    Admin.ConfirmDelete = false;
                    Gui.BuildMainWindow();
                }, currentBounds, "admin-city-pick");

            if (string.IsNullOrEmpty(Admin.SelectedCity) || !AdminClientState.CityFlags.TryGetValue(Admin.SelectedCity, out AdminCityFlagsItem flags))
            {
                compo.AddStaticText(Lang.Get("claims:gui-admin-select-city-hint"),
                    CairoFont.WhiteDetailText(), currentBounds.BelowCopy(0, 10));
                return;
            }

            // --- stats ---
            currentBounds = currentBounds.BelowCopy(0, 6).WithFixedSize(ctx.Line.fixedWidth, 22);
            string stats = Lang.Get("claims:gui-admin-city-stats", flags.CitizenCount, flags.PlotCount);
            if (flags.HasBalance) stats += Lang.Get("claims:gui-admin-city-stats-balance", flags.Balance);
            compo.AddStaticText(stats, CairoFont.WhiteDetailText(), currentBounds);

            // --- flags, two per row: five stacked rows were a third of the window ---
            var flagDefs = new (string Key, string Label, bool Value, Action<bool> Apply)[]
            {
                ("pvp", Lang.Get("claims:gui-admin-pvp"), flags.Pvp, v => flags.Pvp = v),
                ("fire", Lang.Get("claims:gui-admin-fire-spread"), flags.Fire, v => flags.Fire = v),
                ("blast", Lang.Get("claims:gui-admin-explosions"), flags.Blast, v => flags.Blast = v),
                ("technical", Lang.Get("claims:gui-admin-flag-technical"), flags.Technical, v => flags.Technical = v),
                ("open", Lang.Get("claims:gui-admin-flag-open"), flags.Open, v => flags.Open = v),
            };

            var flagGridStart = currentBounds.BelowCopy(0, 6).WithFixedSize(150, 25);
            int flagRows = (flagDefs.Length + 1) / 2;
            for (int i = 0; i < flagDefs.Length; i++)
            {
                var def = flagDefs[i];

                var labelBounds = flagGridStart.FlatCopy();
                labelBounds.fixedX += (i % 2) * 235;
                labelBounds.fixedY += (i / 2) * 29;
                compo.AddStaticText(def.Label, CairoFont.WhiteDetailText(), labelBounds);

                var switchBounds = labelBounds.RightCopy(5, 0).WithFixedSize(25, 25);
                string switchKey = "admin-city-" + def.Key;
                compo.AddSwitch((on) =>
                {
                    def.Apply(on);
                    Send("/cadmin city set " + def.Key + " " + Admin.SelectedCity + " " + (on ? "on" : "off"));
                }, switchBounds, switchKey);
                compo.GetSwitch(switchKey).SetValue(def.Value);
            }

            // Anchor for the rows below: the last row of the grid.
            currentBounds = flagGridStart.FlatCopy();
            currentBounds.fixedY += (flagRows - 1) * 29;

            // --- text operations ---
            currentBounds = AddTextOp(compo, currentBounds, ctx.Line.fixedWidth, "admin-rename", Admin.RenameTo, v => Admin.RenameTo = v,
                "claims:gui-admin-rename", () => "/cadmin city set name " + Admin.SelectedCity + " " + Admin.RenameTo);

            currentBounds = AddTextOp(compo, currentBounds, ctx.Line.fixedWidth, "admin-player", Admin.PlayerName, v => Admin.PlayerName = v,
                "claims:gui-admin-set-mayor", () => "/cadmin city set mayor " + Admin.SelectedCity + " " + Admin.PlayerName,
                extraLabelKey: "claims:gui-admin-add-to-city", extraCommand: () => "/cadmin city add " + Admin.SelectedCity + " " + Admin.PlayerName,
                thirdLabelKey: "claims:gui-admin-kick-from-city", thirdCommand: () => "/cadmin city kick " + Admin.SelectedCity + " " + Admin.PlayerName);

            // Bonus claims and join fee share one row when both fit - both are short numbers, and
            // stacking every operation vertically is what pushed the page past the window bottom.
            string bonusCaption = Lang.Get("claims:gui-admin-bonus-claims");
            string feeCaption = Lang.Get("claims:gui-admin-join-fee");
            double bonusWidth = ButtonWidth(bonusCaption);
            double feeWidth = ButtonWidth(feeCaption);
            bool feeFitsOnSameRow = 70 + 8 + bonusWidth + 16 + 70 + 8 + feeWidth <= ctx.Line.fixedWidth;

            var bonusInput = currentBounds.BelowCopy(0, 6).WithFixedSize(70, 28);
            compo.AddTextInput(bonusInput, v => Admin.BonusClaims = v, null, "admin-bonus");
            compo.GetTextInput("admin-bonus").SetValue(Admin.BonusClaims ?? "");
            var bonusButton = bonusInput.RightCopy(8).WithFixedSize(bonusWidth, 28);
            AddCommandButton(compo, bonusButton, bonusCaption,
                () => "/cadmin city set bonusclaims " + Admin.SelectedCity + " " + Admin.BonusClaims);

            var feeInput = feeFitsOnSameRow
                ? bonusButton.RightCopy(16).WithFixedSize(70, 28)
                : bonusInput.BelowCopy(0, 6).WithFixedSize(70, 28);
            compo.AddTextInput(feeInput, v => Admin.CityFee = v, null, "admin-fee");
            compo.GetTextInput("admin-fee").SetValue(Admin.CityFee ?? "");
            var feeButton = feeInput.RightCopy(8).WithFixedSize(feeWidth, 28);
            AddCommandButton(compo, feeButton, feeCaption,
                () => "/cadmin city set fee " + Admin.SelectedCity + " " + Admin.CityFee);

            currentBounds = feeInput;

            currentBounds = AddTextOp(compo, currentBounds, ctx.Line.fixedWidth, "admin-radius", Admin.ClaimRadius, v => Admin.ClaimRadius = v,
                "claims:gui-admin-radius-claim", () => "/cadmin city radiusclaim " + Admin.SelectedCity + " " + Admin.ClaimRadius);

            // --- plot and lifecycle buttons ---
            var actionRow = new ButtonRow(compo, currentBounds.BelowCopy(0, 10), ctx.Line.fixedWidth);
            actionRow.Add(Lang.Get("claims:gui-admin-claim"), () =>
            {
                Send("/cadmin city claim " + Admin.SelectedCity);
                return true;
            });
            actionRow.Add(Lang.Get("claims:gui-admin-unclaim"), () =>
            {
                Send("/cadmin city unclaim " + Admin.SelectedCity);
                return true;
            });
            actionRow.Add(Lang.Get(Admin.ConfirmDelete ? "claims:gui-admin-confirm-delete" : "claims:gui-admin-delete-city"), () =>
            {
                // Deleting a city is asked twice - the first press only arms the button.
                if (!Admin.ConfirmDelete)
                {
                    Admin.ConfirmDelete = true;
                    Gui.BuildMainWindow();
                    return true;
                }
                Send("/cadmin city delete " + Admin.SelectedCity);
                Admin.ConfirmDelete = false;
                Admin.SelectedCity = "";
                Gui.BuildMainWindow();
                return true;
            });
        }

        /// <summary>A text field with one to three buttons acting on whatever was typed into it.</summary>
        private ElementBounds AddTextOp(GuiComposer compo, ElementBounds bounds, double maxWidth, string key,
            string value, Action<string> store,
            string labelKey, Func<string> command,
            string extraLabelKey = null, Func<string> extraCommand = null,
            string thirdLabelKey = null, Func<string> thirdCommand = null)
        {
            const double inputWidth = 150;

            var inputBounds = bounds.BelowCopy(0, 6).WithFixedSize(inputWidth, 28);
            compo.AddTextInput(inputBounds, store, null, key);
            compo.GetTextInput(key).SetValue(value ?? "");

            var row = new ButtonRow(compo, inputBounds, maxWidth, startX: inputWidth + 10);
            row.Add(Lang.Get(labelKey), () => { Send(command()); return true; });

            if (extraLabelKey != null) row.Add(Lang.Get(extraLabelKey), () => { Send(extraCommand()); return true; });
            if (thirdLabelKey != null) row.Add(Lang.Get(thirdLabelKey), () => { Send(thirdCommand()); return true; });

            // The row may have wrapped, so anchor whatever comes next below its last line.
            return row.Bounds.fixedY > inputBounds.fixedY ? row.Bounds : inputBounds;
        }

        private void AddCommandButton(GuiComposer compo, ElementBounds bounds, string caption, Func<string> command)
        {
            compo.AddButton(caption, new ActionConsumable(() =>
            {
                Send(command());
                return true;
            }), bounds, EnumButtonStyle.Normal);
        }
    }
}
