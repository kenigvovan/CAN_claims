using System.Collections.Generic;
using System.Linq;
using claims.src.gui.playerGui.GuiElements;
using claims.src.gui.playerGui.structures.cellElements;
using claims.src.gui.playerGui.Widgets;
using Vintagestory.API.Client;
using Vintagestory.API.Common;
using Vintagestory.API.Config;

namespace claims.src.gui.playerGui.Pages
{
    /// <summary>
    /// Everything about one city rank: who holds it, and what it grants.
    ///
    /// Three cards down the page - what the rank is, who holds it, what it permits - because both
    /// lists grow and the page has to stay inside the window either way. Granting the rank to
    /// somebody and deleting it are actions on the rank, so they live in the first card; the
    /// navigation row is only the way back.
    /// </summary>
    public sealed class RankInfoPage : CANGuiPage
    {
        private const string AddDropKey = "rank-perm-add";
        private const string RemoveDropKey = "rank-perm-remove";
        private const string MembersKey = "rank-members";
        private const string MembersScrollKey = "rank-members-scrollbar";
        private const string PermsKey = "rank-perm-list";
        private const string PermsScrollKey = "rank-perm-scrollbar";

        private const int RowHeight = 26;
        private const double MembersBoxHeight = 84;

        /// <summary>Width a scroll box gives up to its scrollbar and the gap before it.</summary>
        private const double ScrollbarWidth = 20;
        private const double ScrollbarGap = 7;
        private const double ScrollbarReserve = ScrollbarWidth + ScrollbarGap;

        /// <summary>Label, dropdown and button of one permission column, plus the gaps between.</summary>
        private const double ControlsHeight = 20 + 4 + 28 + 6 + 26;

        protected override bool IsAvailable(out string reasonLangKey)
        {
            reasonLangKey = null;
            return Player.CityInfo != null && SelectedRank() != null;
        }

        private CityRankCellElement SelectedRank()
            => Player.CityInfo?.CityRanks.FirstOrDefault(rc => rc.Name.Equals(State.DialogArgs.Selected), null);

        protected override void BuildContent(PageBuildContext ctx)
        {
            var compo = ctx.Compo;
            CityRankCellElement rank = SelectedRank();
            var perms = Player.PlayerPermissions;

            var anchor = ctx.Line.BelowCopy(0, 14);
            anchor.Alignment = EnumDialogArea.LeftTop;

            var column = anchor.FlatCopy();
            column.fixedWidth = ctx.Line.fixedWidth;
            double y = column.fixedY;

            // --- what the rank is ---
            var rankRows = new List<CardRow>
            {
                new CardRow { Label = Lang.Get("claims:gui-rankinfo-label-name"), Value = rank.Name, Key = "rankName" },
                new CardRow
                {
                    Label = Lang.Get("claims:gui-rankinfo-label-holders"),
                    Value = rank.Citizens.Count.ToString(),
                    Key = "rankHolders"
                },
                new CardRow
                {
                    Label = Lang.Get("claims:gui-rankinfo-label-perms"),
                    Value = rank.Permissions.Count.ToString(),
                    Key = "rankPermCount"
                }
            };

            y = Card.RowsWithActions(compo, column, y, Lang.Get("claims:gui-rankinfo-section-rank"), rankRows, slot =>
            {
                var actions = new ActionRow(compo, slot);

                if (perms.HasPermission(rights.EnumPlayerPermissions.CITY_SET_RANK))
                {
                    actions.Add("plus", "grantRank",
                        on => { if (on) OpenDialog(EnumUpperWindowSelectedState.CITY_RANK_ADD, args => args.First = rank.Name); },
                        Lang.Get("claims:gui-add-rank-tooltip"));
                }

                if (perms.HasPermission(rights.EnumPlayerPermissions.CITY_REMOVE_RANK))
                {
                    actions.Add("eraser", "deleteRank",
                        on => { if (on) OpenDialog(EnumUpperWindowSelectedState.CITY_RANK_DELETE_CONFIRM); },
                        Lang.Get("claims:gui-remove-rank-tooltip"));
                }
            });

            // --- who holds it ---
            double membersHeight = Card.HeaderHeight + MembersBoxHeight + Card.Padding * 2;
            ElementBounds membersInner = Card.Frame(compo, column, y, membersHeight,
                Lang.Get("claims:gui-rankinfo-section-holders"));
            BuildMemberList(ctx, compo, membersInner, rank);

            y += membersHeight + Card.Gap;

            // --- what it grants, and the controls that change that ---
            bool canGrant = perms.HasPermission(rights.EnumPlayerPermissions.CITY_ADD_PERMISSION_TO_RANK);
            bool canRevoke = perms.HasPermission(rights.EnumPlayerPermissions.CITY_REMOVE_PERMISSION_FROM_RANK);
            double controlsHeight = (canGrant || canRevoke) ? ControlsHeight + Card.Gap : 0;

            // The last card takes whatever is left down to the navigation row, so the permission
            // list is as long as the window allows rather than a height picked to look about right.
            double permsHeight = Gui.mainBounds.fixedHeight * NavRow.LineHeightFraction - y - Card.Gap;
            double minimum = Card.HeaderHeight + controlsHeight + RowHeight * 2 + Card.Padding * 2;
            if (permsHeight < minimum) permsHeight = minimum;

            ElementBounds permsInner = Card.Frame(compo, column, y, permsHeight,
                Lang.Get("claims:gui-rankinfo-current-perms"));

            double listTop = permsInner.fixedY + controlsHeight;
            if (canGrant || canRevoke)
            {
                BuildPermissionControls(compo, permsInner, rank, canGrant, canRevoke);
            }

            BuildPermissionList(ctx, compo, permsInner, listTop, rank);

            NavRow.Build(Gui, ctx.Current, ctx.Line, 0,
                new NavButton("claims:fast-backward-button", () => GoTo(EnumSelectedTab.Ranks), Lang.Get("claims:gui-nav-back")));
        }

        /// <summary>
        /// The two dropdown-and-button columns at the top of the permissions card. Granting and
        /// revoking are separate permissions, so each column stands on its own.
        /// </summary>
        private void BuildPermissionControls(GuiComposer compo, ElementBounds inner, CityRankCellElement rank,
            bool canGrant, bool canRevoke)
        {
            var all = claims.config.AVAILABLE_CITY_PERMISSIONS;
            var grantable = all?.Where(v => !rank.Permissions.Contains(v)).Select(v => v.ToString()).ToArray() ?? new string[0];
            var revocable = all?.Where(v => rank.Permissions.Contains(v)).Select(v => v.ToString()).ToArray() ?? new string[0];

            double columnWidth = (inner.fixedWidth - 20) / 2;

            if (canGrant)
            {
                var addLabel = inner.FlatCopy().WithFixedSize(columnWidth, 20);
                compo.AddStaticText(Lang.Get("claims:gui-rankinfo-add-perms"),
                    CairoFont.WhiteSmallText().WithColor(ClaimsColors.Label), addLabel, "add-perms-label");

                var addDrop = addLabel.BelowCopy(0, 4).WithFixedSize(columnWidth, 28);
                compo.AddMultiSelectDropDown(grantable, grantable, -1, null, addDrop, AddDropKey);

                var addButton = addDrop.BelowCopy(0, 6).WithFixedSize(columnWidth, 26);
                compo.AddButton(Lang.Get("claims:gui-rankinfo-add-button"), new ActionConsumable(() =>
                    ApplyPermissions(compo, AddDropKey, "/c rank addperm ")), addButton, EnumButtonStyle.Normal);
            }

            if (canRevoke)
            {
                var removeLabel = inner.FlatCopy().WithFixedSize(columnWidth, 20);
                removeLabel.fixedX += columnWidth + 20;
                compo.AddStaticText(Lang.Get("claims:gui-rankinfo-remove-perms"),
                    CairoFont.WhiteSmallText().WithColor(ClaimsColors.Label), removeLabel, "remove-perms-label");

                var removeDrop = removeLabel.BelowCopy(0, 4).WithFixedSize(columnWidth, 28);
                compo.AddMultiSelectDropDown(revocable, revocable, -1, null, removeDrop, RemoveDropKey);

                var removeButton = removeDrop.BelowCopy(0, 6).WithFixedSize(columnWidth, 26);
                compo.AddButton(Lang.Get("claims:gui-rankinfo-remove-button"), new ActionConsumable(() =>
                    ApplyPermissions(compo, RemoveDropKey, "/c rank removeperm ")), removeButton, EnumButtonStyle.Normal);
            }
        }

        /// <summary>
        /// The holders, as buttons that strip the rank from that player. The old page only listed
        /// them in a tooltip, so there was no way to remove anyone from here.
        /// </summary>
        private void BuildMemberList(PageBuildContext ctx, GuiComposer compo, ElementBounds inner,
            CityRankCellElement rank)
        {
            ElementBounds clipBounds;
            GuiElementContainer scrollArea = AddScrollBox(compo, inner, inner.fixedY, MembersBoxHeight,
                MembersKey, MembersScrollKey, out clipBounds);

            var font = CairoFont.WhiteDetailText();
            double boxWidth = inner.fixedWidth - ScrollbarReserve;

            if (rank.Citizens.Count == 0)
            {
                var emptyBounds = ElementBounds.Fixed(4, 4, boxWidth - 30, RowHeight);
                scrollArea.Add(new GuiElementStaticText(compo.Api,
                    Lang.Get("claims:gui-rankinfo-none-selected"), EnumTextOrientation.Left, emptyBounds,
                    font.Clone().WithColor(ClaimsColors.Label)));
            }
            else
            {
                // Two names per row; each button asks to strip the rank from that player.
                // Citizens is a set, so take an ordered snapshot to lay it out by index.
                var citizens = rank.Citizens.ToList();
                double buttonWidth = (boxWidth - 40) / 2;
                for (int i = 0; i < citizens.Count; i++)
                {
                    string citizen = citizens[i];

                    var buttonBounds = ElementBounds.Fixed(
                        4 + (i % 2) * (buttonWidth + 8),
                        4 + (i / 2) * (RowHeight + 4),
                        buttonWidth, RowHeight);

                    scrollArea.Add(new GuiElementButtonWithAdditionalText(compo.Api, citizen, font, font,
                        new ActionConsumable(() =>
                        {
                            OpenDialog(EnumUpperWindowSelectedState.CITY_RANK_REMOVE_CONFIRM, args =>
                            {
                                args.First = rank.Name;
                                args.Second = citizen;
                            });
                            return true;
                        }), buttonBounds));
                }
            }

            int memberRows = (rank.Citizens.Count + 1) / 2;
            ctx.AfterCompose(() =>
                compo.GetScrollbar(MembersScrollKey).SetHeights((float)clipBounds.fixedHeight, (RowHeight + 4) * memberRows + 8));
        }

        /// <summary>The permission names, filling what is left of the card under the controls.</summary>
        private void BuildPermissionList(PageBuildContext ctx, GuiComposer compo, ElementBounds inner,
            double top, CityRankCellElement rank)
        {
            double height = inner.fixedY + inner.fixedHeight - top;

            ElementBounds clipBounds;
            GuiElementContainer scrollArea = AddScrollBox(compo, inner, top, height,
                PermsKey, PermsScrollKey, out clipBounds);

            double boxWidth = inner.fixedWidth - ScrollbarReserve;
            List<string> permNames = rank.Permissions.Select(v => v.ToString()).ToList();

            if (permNames.Count == 0)
            {
                scrollArea.Add(new GuiElementRichtext(compo.Api,
                    VtmlUtil.Richtextify(compo.Api, Lang.Get("claims:gui-rank-no-permissions"),
                        CairoFont.WhiteDetailText().WithColor(ClaimsColors.Label)),
                    ElementBounds.Fixed(4, 4, boxWidth - 30, RowHeight)));
            }
            else
            {
                ElementBounds rowBounds = ElementBounds.Fixed(4, 4, boxWidth - 30, RowHeight);
                foreach (var name in permNames)
                {
                    scrollArea.Add(new GuiElementRichtext(compo.Api,
                        VtmlUtil.Richtextify(compo.Api, name, CairoFont.WhiteDetailText().WithColor(ClaimsColors.Value)),
                        rowBounds));
                    rowBounds = rowBounds.BelowCopy();
                }
            }

            ctx.AfterCompose(() =>
                compo.GetScrollbar(PermsScrollKey).SetHeights((float)clipBounds.fixedHeight, RowHeight * permNames.Count + 8));
        }

        /// <summary>
        /// A clipped, scrolling area inside a card, without a frame of its own - the card is the
        /// border.
        ///
        /// The area bounds are added to the composer before anything forks from them: a bounds that
        /// is only ever forked from has no parent, and the renderer dereferences that parent while
        /// pushing the scissor for the clip.
        /// </summary>
        private static GuiElementContainer AddScrollBox(GuiComposer compo, ElementBounds inner, double top,
            double height, string containerKey, string scrollbarKey, out ElementBounds clipBounds)
        {
            var areaBounds = inner.FlatCopy().WithFixedSize(inner.fixedWidth, height);
            areaBounds.fixedY = top;

            clipBounds = ElementBounds.Fixed(0, 0, inner.fixedWidth - ScrollbarReserve, height);
            ElementBounds scrollbarBounds = clipBounds.RightCopy(ScrollbarGap).WithFixedWidth(ScrollbarWidth);
            ElementBounds containerBounds = clipBounds.FlatCopy();

            compo.BeginChildElements(areaBounds)
                    .BeginClip(clipBounds)
                        .AddContainer(containerBounds, containerKey)
                    .EndClip()
                    .AddVerticalScrollbar((value) =>
                    {
                        ElementBounds bounds = compo.GetContainer(containerKey).Bounds;
                        bounds.fixedY = 0 - value;
                        bounds.CalcWorldBounds();
                    }, scrollbarBounds, scrollbarKey)
                .EndChildElements();

            return compo.GetContainer(containerKey);
        }

        /// <summary>
        /// Sends whatever is ticked in one of the dropdowns as a single command, then clears it.
        /// </summary>
        private bool ApplyPermissions(GuiComposer compo, string dropKey, string commandPrefix)
        {
            var dropDown = compo.GetDropDown(dropKey);
            if (dropDown.SelectedValues.Length > 0)
            {
                Send(commandPrefix + State.DialogArgs.Selected + " " + string.Join(' ', dropDown.SelectedValues));
            }
            dropDown.SetSelectedValue("");
            return true;
        }
    }
}
