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
        private const double InputHeight = 28;
        private const double RowGap = 6;

        /// <summary>One line of the flag grid, which runs two flags to a row.</summary>
        private const double FlagRowHeight = 29;
        private const int FlagColumns = 2;

        private AdminPageState Admin => State.Admin;

        protected override void BuildAdminContent(PageBuildContext ctx)
        {
            var compo = ctx.Compo;

            // No page title of its own: the admin tab strip already names the page, and every
            // vertical pixel is needed - the old layout ran past the window bottom.
            var column = ctx.Current.BelowCopy(0, 5);
            column.fixedWidth = ctx.Line.fixedWidth;
            column.WithAlignment(EnumDialogArea.LeftTop);

            double y = column.fixedY;

            y = BuildCreateCard(compo, column, y);

            var allCityNames = AdminClientState.CityFlags.Keys.OrderBy(n => n).ToArray();
            if (allCityNames.Length == 0)
            {
                Hint(compo, column, y, Lang.Get("claims:gui-admin-section-city"),
                    Lang.Get("claims:gui-admin-no-city-data"), "admin-no-city-data");
                return;
            }

            // The dropdown alone is unusable once a server has a few dozen cities, so it is narrowed
            // by a filter. Applying it rebuilds the window: a text input cannot do that per keystroke
            // without losing focus.
            string filter = (Admin.CityFilter ?? "").ToLowerInvariant();
            var cityNames = filter.Length == 0
                ? allCityNames
                : allCityNames.Where(n => n.ToLowerInvariant().Contains(filter)).ToArray();

            AdminCityFlagsItem flags = null;
            bool picked = !string.IsNullOrEmpty(Admin.SelectedCity)
                       && AdminClientState.CityFlags.TryGetValue(Admin.SelectedCity, out flags);

            y = BuildPickCard(compo, column, y, cityNames, picked ? flags : null);

            if (cityNames.Length == 0 || !picked) return;

            y = BuildFlagsCard(compo, column, y, flags);
            y = BuildOperationsCard(compo, column, y);
            BuildActionsCard(compo, column, y);
        }

        /// <summary>Founding a city out of nothing, and asking the server for fresh data.</summary>
        private double BuildCreateCard(GuiComposer compo, ElementBounds column, double y)
        {
            string createCaption = Lang.Get("claims:gui-admin-create");
            string refreshCaption = Lang.Get("claims:gui-admin-refresh");

            double innerWidth = column.fixedWidth - Card.Padding * 2;
            double rowHeight = Math.Max(InputHeight,
                ButtonRow.HeightFor(innerWidth, 210, createCaption, refreshCaption));

            ElementBounds inner = Card.Frame(compo, column, y, Card.HeaderHeight + rowHeight + Card.Padding * 2,
                Lang.Get("claims:gui-admin-section-cities"));

            var nameInput = inner.FlatCopy().WithFixedSize(200, InputHeight);
            compo.AddTextInput(nameInput, v => Admin.NewCityName = v, null, "admin-new-city");
            compo.GetTextInput("admin-new-city").SetValue(Admin.NewCityName);

            var createRow = new ButtonRow(compo, nameInput, inner.fixedWidth, startX: 210);
            createRow.Add(createCaption, () =>
            {
                if (Admin.NewCityName.Length == 0) return true;
                Send("/cadmin city new " + Admin.NewCityName);
                Admin.NewCityName = "";
                Gui.BuildMainWindow();
                return true;
            }, Lang.Get("claims:gui-admin-create-tooltip"));

            createRow.Add(refreshCaption, () =>
            {
                claims.clientChannel.SendPacket(new SavedPlotsPacket { type = PacketsContentEnum.ADMIN_REQUEST_CITY_FLAGS });
                return true;
            }, Lang.Get("claims:gui-admin-refresh-cities-tooltip"));

            return y + Card.HeaderHeight + rowHeight + Card.Padding * 2 + Card.Gap;
        }

        /// <summary>Narrowing the list down and picking one city out of it.</summary>
        private double BuildPickCard(GuiComposer compo, ElementBounds column, double y,
                                     string[] cityNames, AdminCityFlagsItem flags)
        {
            string filterCaption = Lang.Get("claims:gui-admin-filter");

            double innerWidth = column.fixedWidth - Card.Padding * 2;
            double filterRowHeight = Math.Max(InputHeight, ButtonRow.HeightFor(innerWidth, 150, filterCaption));

            // Filter row, then either the dropdown or the reason there is none, then the stats line.
            double bodyHeight = filterRowHeight + RowGap + InputHeight;
            if (cityNames.Length == 0) bodyHeight = filterRowHeight + RowGap + 22;
            else if (flags != null) bodyHeight += RowGap + 22;
            else bodyHeight += RowGap + 22;

            ElementBounds inner = Card.Frame(compo, column, y, Card.HeaderHeight + bodyHeight + Card.Padding * 2,
                Lang.Get("claims:gui-admin-section-city"));

            var filterInput = inner.FlatCopy().WithFixedSize(140, InputHeight);
            compo.AddTextInput(filterInput, v => Admin.CityFilter = v, null, "admin-city-filter");
            compo.GetTextInput("admin-city-filter").SetValue(Admin.CityFilter ?? "");
            Tooltip.Add(compo, filterCaption, filterInput, "tip-admin-city-filter");

            var filterRow = new ButtonRow(compo, filterInput, inner.fixedWidth, startX: 150);
            filterRow.Add(filterCaption, () =>
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

            double rowY = inner.fixedY + filterRowHeight + RowGap;

            if (cityNames.Length == 0)
            {
                var noneBounds = inner.FlatCopy().WithFixedHeight(22);
                noneBounds.fixedY = rowY;
                compo.AddStaticText(Lang.Get("claims:gui-admin-no-cities-match"),
                    CairoFont.WhiteSmallText().WithColor(ClaimsColors.Label), noneBounds, "admin-no-match");

                return y + Card.HeaderHeight + bodyHeight + Card.Padding * 2 + Card.Gap;
            }

            var dropBounds = inner.FlatCopy().WithFixedSize(200, InputHeight);
            dropBounds.fixedY = rowY;
            compo.AddDropDown(cityNames, cityNames, Math.Max(0, Array.IndexOf(cityNames, Admin.SelectedCity)),
                (code, selected) =>
                {
                    Admin.SelectedCity = code;
                    Admin.ConfirmDelete = false;
                    Gui.BuildMainWindow();
                }, dropBounds, "admin-city-pick");

            var statsBounds = inner.FlatCopy().WithFixedHeight(22);
            statsBounds.fixedY = rowY + InputHeight + RowGap;

            if (flags == null)
            {
                compo.AddStaticText(Lang.Get("claims:gui-admin-select-city-hint"),
                    CairoFont.WhiteSmallText().WithColor(ClaimsColors.Label), statsBounds, "admin-pick-hint");
            }
            else
            {
                string stats = Lang.Get("claims:gui-admin-city-stats", flags.CitizenCount, flags.PlotCount);
                if (flags.HasBalance) stats += Lang.Get("claims:gui-admin-city-stats-balance", flags.Balance);
                compo.AddStaticText(stats,
                    CairoFont.WhiteSmallText().WithColor(ClaimsColors.Value), statsBounds, "admin-city-stats");
            }

            return y + Card.HeaderHeight + bodyHeight + Card.Padding * 2 + Card.Gap;
        }

        /// <summary>The city's own switches, two per row: five stacked were a third of the window.</summary>
        private double BuildFlagsCard(GuiComposer compo, ElementBounds column, double y, AdminCityFlagsItem flags)
        {
            var flagDefs = new (string Key, string Label, bool Value, Action<bool> Apply)[]
            {
                ("pvp", Lang.Get("claims:gui-admin-pvp"), flags.Pvp, v => flags.Pvp = v),
                ("fire", Lang.Get("claims:gui-admin-fire-spread"), flags.Fire, v => flags.Fire = v),
                ("blast", Lang.Get("claims:gui-admin-explosions"), flags.Blast, v => flags.Blast = v),
                ("technical", Lang.Get("claims:gui-admin-flag-technical"), flags.Technical, v => flags.Technical = v),
                ("open", Lang.Get("claims:gui-admin-flag-open"), flags.Open, v => flags.Open = v),
            };

            int rows = (flagDefs.Length + FlagColumns - 1) / FlagColumns;
            double height = Card.HeaderHeight + rows * FlagRowHeight + Card.Padding * 2;

            ElementBounds inner = Card.Frame(compo, column, y, height, Lang.Get("claims:gui-admin-section-flags"));

            double columnWidth = inner.fixedWidth / FlagColumns;

            for (int i = 0; i < flagDefs.Length; i++)
            {
                var def = flagDefs[i];

                var labelBounds = inner.FlatCopy().WithFixedSize(columnWidth - 40, 25);
                labelBounds.fixedX += (i % FlagColumns) * columnWidth;
                labelBounds.fixedY = inner.fixedY + (i / FlagColumns) * FlagRowHeight;
                compo.AddStaticText(def.Label,
                    CairoFont.WhiteSmallText().WithColor(ClaimsColors.Label), labelBounds, "admin-flaglabel-" + def.Key);

                var switchBounds = labelBounds.RightCopy(5, 0).WithFixedSize(25, 25);
                string switchKey = "admin-city-" + def.Key;
                compo.AddSwitch((on) =>
                {
                    def.Apply(on);
                    Send("/cadmin city set " + def.Key + " " + Admin.SelectedCity + " " + (on ? "on" : "off"));
                }, switchBounds, switchKey);
                compo.GetSwitch(switchKey).SetValue(def.Value);
            }

            return y + height + Card.Gap;
        }

        /// <summary>Everything that takes a typed value: name, player, numbers.</summary>
        private double BuildOperationsCard(GuiComposer compo, ElementBounds column, double y)
        {
            double innerWidth = column.fixedWidth - Card.Padding * 2;

            string renameCaption = Lang.Get("claims:gui-admin-rename");
            string mayorCaption = Lang.Get("claims:gui-admin-set-mayor");
            string addCaption = Lang.Get("claims:gui-admin-add-to-city");
            string kickCaption = Lang.Get("claims:gui-admin-kick-from-city");
            string bonusCaption = Lang.Get("claims:gui-admin-bonus-claims");
            string feeCaption = Lang.Get("claims:gui-admin-join-fee");
            string radiusCaption = Lang.Get("claims:gui-admin-radius-claim");

            const double textOpInput = 150;
            const double numberInput = 70;

            double renameHeight = ButtonRow.HeightFor(innerWidth, textOpInput + 10, renameCaption);
            double playerHeight = ButtonRow.HeightFor(innerWidth, textOpInput + 10, mayorCaption, addCaption, kickCaption);
            double radiusHeight = ButtonRow.HeightFor(innerWidth, textOpInput + 10, radiusCaption);

            // Bonus claims and join fee share one row when both fit - both are short numbers, and
            // stacking every operation vertically is what pushed the page past the window bottom.
            bool feeFitsOnSameRow = numberInput + 8 + ButtonWidth(bonusCaption) + 16
                                  + numberInput + 8 + ButtonWidth(feeCaption) <= innerWidth;
            double numbersHeight = feeFitsOnSameRow ? InputHeight : InputHeight * 2 + RowGap;

            double body = renameHeight + RowGap + playerHeight + RowGap
                        + numbersHeight + RowGap + radiusHeight;

            ElementBounds inner = Card.Frame(compo, column, y, Card.HeaderHeight + body + Card.Padding * 2,
                Lang.Get("claims:gui-admin-section-operations"));

            double rowY = inner.fixedY;

            rowY = AddTextOp(compo, inner, rowY, "admin-rename", Admin.RenameTo, v => Admin.RenameTo = v,
                renameHeight, renameCaption, () => "/cadmin city set name " + Admin.SelectedCity + " " + Admin.RenameTo);

            rowY = AddTextOp(compo, inner, rowY, "admin-player", Admin.PlayerName, v => Admin.PlayerName = v,
                playerHeight, mayorCaption, () => "/cadmin city set mayor " + Admin.SelectedCity + " " + Admin.PlayerName,
                addCaption, () => "/cadmin city add " + Admin.SelectedCity + " " + Admin.PlayerName,
                kickCaption, () => "/cadmin city kick " + Admin.SelectedCity + " " + Admin.PlayerName);

            var bonusInput = inner.FlatCopy().WithFixedSize(numberInput, InputHeight);
            bonusInput.fixedY = rowY;
            compo.AddTextInput(bonusInput, v => Admin.BonusClaims = v, null, "admin-bonus");
            compo.GetTextInput("admin-bonus").SetValue(Admin.BonusClaims ?? "");

            var bonusButton = bonusInput.RightCopy(8).WithFixedSize(ButtonWidth(bonusCaption), InputHeight);
            AddCommandButton(compo, bonusButton, bonusCaption,
                () => "/cadmin city set bonusclaims " + Admin.SelectedCity + " " + Admin.BonusClaims);

            var feeInput = feeFitsOnSameRow
                ? bonusButton.RightCopy(16).WithFixedSize(numberInput, InputHeight)
                : bonusInput.BelowCopy(0, RowGap).WithFixedSize(numberInput, InputHeight);
            compo.AddTextInput(feeInput, v => Admin.CityFee = v, null, "admin-fee");
            compo.GetTextInput("admin-fee").SetValue(Admin.CityFee ?? "");

            var feeButton = feeInput.RightCopy(8).WithFixedSize(ButtonWidth(feeCaption), InputHeight);
            AddCommandButton(compo, feeButton, feeCaption,
                () => "/cadmin city set fee " + Admin.SelectedCity + " " + Admin.CityFee);

            rowY += numbersHeight + RowGap;

            AddTextOp(compo, inner, rowY, "admin-radius", Admin.ClaimRadius, v => Admin.ClaimRadius = v,
                radiusHeight, radiusCaption, () => "/cadmin city radiusclaim " + Admin.SelectedCity + " " + Admin.ClaimRadius);

            return y + Card.HeaderHeight + body + Card.Padding * 2 + Card.Gap;
        }

        /// <summary>Claiming plots wholesale, and getting rid of the city entirely.</summary>
        private void BuildActionsCard(GuiComposer compo, ElementBounds column, double y)
        {
            string claimCaption = Lang.Get("claims:gui-admin-claim");
            string unclaimCaption = Lang.Get("claims:gui-admin-unclaim");
            string deleteCaption = Lang.Get(Admin.ConfirmDelete
                ? "claims:gui-admin-confirm-delete" : "claims:gui-admin-delete-city");

            double innerWidth = column.fixedWidth - Card.Padding * 2;
            double height = ButtonRow.HeightFor(innerWidth, 0, claimCaption, unclaimCaption, deleteCaption);

            ElementBounds inner = Card.Frame(compo, column, y, Card.HeaderHeight + height + Card.Padding * 2,
                Lang.Get("claims:gui-admin-section-actions"));

            var actionRow = new ButtonRow(compo, inner.FlatCopy().WithFixedHeight(ButtonRow.ButtonHeight), inner.fixedWidth);
            actionRow.Add(claimCaption, () =>
            {
                Send("/cadmin city claim " + Admin.SelectedCity);
                return true;
            });
            actionRow.Add(unclaimCaption, () =>
            {
                Send("/cadmin city unclaim " + Admin.SelectedCity);
                return true;
            });
            actionRow.Add(deleteCaption, () =>
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

        /// <summary>A card holding nothing but an explanation of why there is nothing to show.</summary>
        private static void Hint(GuiComposer compo, ElementBounds column, double y, string title, string text, string key)
        {
            const double hintHeight = 24;
            ElementBounds inner = Card.Frame(compo, column, y,
                Card.HeaderHeight + hintHeight + Card.Padding * 2, title);

            compo.AddStaticText(text, CairoFont.WhiteSmallText().WithColor(ClaimsColors.Label),
                inner.FlatCopy().WithFixedHeight(hintHeight), key);
        }

        /// <summary>
        /// A text field with one to three buttons acting on whatever was typed into it. Returns the
        /// y the next row starts at.
        /// </summary>
        private double AddTextOp(GuiComposer compo, ElementBounds inner, double y, string key,
            string value, Action<string> store,
            double rowHeight, string caption, Func<string> command,
            string extraCaption = null, Func<string> extraCommand = null,
            string thirdCaption = null, Func<string> thirdCommand = null)
        {
            const double inputWidth = 150;

            var inputBounds = inner.FlatCopy().WithFixedSize(inputWidth, InputHeight);
            inputBounds.fixedY = y;
            compo.AddTextInput(inputBounds, store, null, key);
            compo.GetTextInput(key).SetValue(value ?? "");

            var row = new ButtonRow(compo, inputBounds, inner.fixedWidth, startX: inputWidth + 10);
            row.Add(caption, () => { Send(command()); return true; });

            if (extraCaption != null) row.Add(extraCaption, () => { Send(extraCommand()); return true; });
            if (thirdCaption != null) row.Add(thirdCaption, () => { Send(thirdCommand()); return true; });

            return y + rowHeight + RowGap;
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
