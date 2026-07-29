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
    /// Laid out as two labelled boxes - holders on top, permissions below - because both lists grow
    /// and the page has to stay inside the window either way. Rank-wide actions (grant to someone,
    /// delete the rank) sit in the navigation row with the back button.
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

        private const double ScrollbarWidth = 20;
        private const double ScrollbarGap = 7;

        /// <summary>
        /// Box width that leaves exactly enough room for the scrollbar and its gap, so the bar ends
        /// flush with the right edge of the page instead of floating in from it.
        /// </summary>
        private static double BoxWidth(double fullWidth) => fullWidth - ScrollbarWidth - ScrollbarGap;

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

            double fullWidth = ctx.Line.fixedWidth;

            // --- rank name, centered ---
            var titleBounds = ctx.Current.BelowCopy(0, 40).WithAlignment(EnumDialogArea.CenterTop);
            titleBounds.fixedWidth = fullWidth;
            titleBounds.fixedHeight = 30;
            compo.AddStaticText(rank.Name, ClaimsFonts.ListHeader, titleBounds, "rankName");

            // --- holders ---
            // Stops short of the box's scrollbar below it, and keeps a gap before the box starts.
            var membersLabel = titleBounds.BelowCopy(0, 10).WithAlignment(EnumDialogArea.LeftTop);
            membersLabel.fixedWidth = fullWidth - 60;
            membersLabel.fixedHeight = 22;
            compo.AddStaticText(Lang.Get("claims:gui-rank-members", rank.Citizens.Count),
                CairoFont.WhiteDetailText(), membersLabel, "members");

            var membersAnchor = BuildMemberList(ctx, compo, membersLabel, fullWidth, rank);

            // --- grant / revoke a permission ---
            var all = claims.config.AVAILABLE_CITY_PERMISSIONS;
            var grantable = all?.Where(v => !rank.Permissions.Contains(v)).Select(v => v.ToString()).ToArray() ?? new string[0];
            var revocable = all?.Where(v => rank.Permissions.Contains(v)).Select(v => v.ToString()).ToArray() ?? new string[0];

            double columnWidth = (fullWidth - 20) / 2;

            // Granting and revoking are separate permissions, so each column stands on its own.
            var perms = Player.PlayerPermissions;
            bool canGrant = perms.HasPermission(rights.EnumPlayerPermissions.CITY_ADD_PERMISSION_TO_RANK);
            bool canRevoke = perms.HasPermission(rights.EnumPlayerPermissions.CITY_REMOVE_PERMISSION_FROM_RANK);

            var addLabel = membersAnchor.BelowCopy(0, 12).WithAlignment(EnumDialogArea.LeftTop).WithFixedSize(columnWidth, 20);
            var addButton = addLabel.BelowCopy(0, 4).WithFixedSize(columnWidth, 26);

            if (canGrant)
            {
                compo.AddStaticText(Lang.Get("claims:gui-rankinfo-add-perms"), CairoFont.WhiteDetailText(), addLabel);

                var addDrop = addLabel.BelowCopy(0, 4).WithFixedSize(columnWidth, 28);
                compo.AddMultiSelectDropDown(grantable, grantable, -1, null, addDrop, AddDropKey);

                addButton = addDrop.BelowCopy(0, 6).WithFixedSize(columnWidth, 26);
                compo.AddButton(Lang.Get("claims:gui-rankinfo-add-button"), new ActionConsumable(() =>
                    ApplyPermissions(compo, AddDropKey, "/c rank addperm ")), addButton, EnumButtonStyle.Normal);
            }

            if (canRevoke)
            {
                var removeLabel = addLabel.RightCopy(20).WithFixedSize(columnWidth, 20);
                compo.AddStaticText(Lang.Get("claims:gui-rankinfo-remove-perms"), CairoFont.WhiteDetailText(), removeLabel);

                var removeDrop = removeLabel.BelowCopy(0, 4).WithFixedSize(columnWidth, 28);
                compo.AddMultiSelectDropDown(revocable, revocable, -1, null, removeDrop, RemoveDropKey);

                var removeButton = removeDrop.BelowCopy(0, 6).WithFixedSize(columnWidth, 26);
                compo.AddButton(Lang.Get("claims:gui-rankinfo-remove-button"), new ActionConsumable(() =>
                    ApplyPermissions(compo, RemoveDropKey, "/c rank removeperm ")), removeButton, EnumButtonStyle.Normal);
            }

            // --- what the rank currently grants ---
            var permsLabel = addButton.BelowCopy(0, 12).WithFixedSize(fullWidth, 20);
            compo.AddStaticText(Lang.Get("claims:gui-rankinfo-current-perms"), CairoFont.WhiteDetailText(), permsLabel);

            BuildPermissionList(ctx, compo, permsLabel, fullWidth, rank);

            var navButtons = new List<NavButton>
            {
                new NavButton("claims:fast-backward-button", () => GoTo(EnumSelectedTab.Ranks), Lang.Get("claims:gui-nav-back"))
            };

            if (perms.HasPermission(rights.EnumPlayerPermissions.CITY_SET_RANK))
            {
                navButtons.Add(new NavButton("plus", () => OpenDialog(EnumUpperWindowSelectedState.CITY_RANK_ADD,
                        args => args.First = rank.Name),
                    Lang.Get("claims:gui-add-rank-tooltip")));
            }

            if (perms.HasPermission(rights.EnumPlayerPermissions.CITY_REMOVE_RANK))
            {
                navButtons.Add(new NavButton("eraser", () => OpenDialog(EnumUpperWindowSelectedState.CITY_RANK_DELETE_CONFIRM),
                    Lang.Get("claims:gui-remove-rank-tooltip")));
            }

            NavRow.Build(Gui, ctx.Current, ctx.Line, 0, navButtons.ToArray());
        }

        /// <summary>
        /// The holders, as buttons that strip the rank from that player. The old page only listed
        /// them in a tooltip, so there was no way to remove anyone from here.
        /// </summary>
        private ElementBounds BuildMemberList(PageBuildContext ctx, GuiComposer compo, ElementBounds anchor,
            double fullWidth, CityRankCellElement rank)
        {
            // BelowCopy, not FixedUnder: the label carries its own fixedY, and FixedUnder ignored it,
            // so the box was drawn over the label instead of under it.
            var areaBounds = anchor.BelowCopy(0, 4).WithFixedSize(BoxWidth(fullWidth), MembersBoxHeight);

            ElementBounds insetBounds = ElementBounds.Fixed(0, 0, areaBounds.fixedWidth, areaBounds.fixedHeight);
            ElementBounds scrollbarBounds = insetBounds.RightCopy(ScrollbarGap).WithFixedWidth(ScrollbarWidth);
            ElementBounds clipBounds = insetBounds.ForkContainingChild(GuiStyle.HalfPadding, GuiStyle.HalfPadding, GuiStyle.HalfPadding, GuiStyle.HalfPadding);
            ElementBounds containerBounds = clipBounds.FlatCopy();

            compo.BeginChildElements(areaBounds)
                .AddInset(insetBounds, 3)
                    .BeginClip(clipBounds)
                        .AddContainer(containerBounds, MembersKey)
                    .EndClip()
                    .AddVerticalScrollbar((value) =>
                    {
                        ElementBounds bounds = compo.GetContainer(MembersKey).Bounds;
                        bounds.fixedY = 0 - value;
                        bounds.CalcWorldBounds();
                    }, scrollbarBounds, MembersScrollKey)
                .EndChildElements();

            GuiElementContainer scrollArea = compo.GetContainer(MembersKey);
            var font = CairoFont.WhiteDetailText();

            if (rank.Citizens.Count == 0)
            {
                var emptyBounds = ElementBounds.Fixed(4, 4, areaBounds.fixedWidth - 30, RowHeight);
                scrollArea.Add(new GuiElementStaticText(compo.Api,
                    Lang.Get("claims:gui-rankinfo-none-selected"), EnumTextOrientation.Left, emptyBounds, font));
            }
            else
            {
                // Two names per row; each button asks to strip the rank from that player.
                // Citizens is a set, so take an ordered snapshot to lay it out by index.
                var citizens = rank.Citizens.ToList();
                double buttonWidth = (areaBounds.fixedWidth - 40) / 2;
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

            return areaBounds;
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

        /// <summary>The permission names, filling whatever is left above the navigation row.</summary>
        private void BuildPermissionList(PageBuildContext ctx, GuiComposer compo, ElementBounds anchor,
            double fullWidth, CityRankCellElement rank)
        {
            double listHeight = Gui.mainBounds.fixedHeight * 0.85 - anchor.fixedY - 45;
            if (listHeight < RowHeight * 2) listHeight = RowHeight * 2;

            var areaBounds = anchor.BelowCopy(0, 4).WithFixedSize(BoxWidth(fullWidth), listHeight);

            ElementBounds insetBounds = ElementBounds.Fixed(0, 0, areaBounds.fixedWidth, areaBounds.fixedHeight);
            ElementBounds scrollbarBounds = insetBounds.RightCopy(ScrollbarGap).WithFixedWidth(ScrollbarWidth);
            ElementBounds clipBounds = insetBounds.ForkContainingChild(GuiStyle.HalfPadding, GuiStyle.HalfPadding, GuiStyle.HalfPadding, GuiStyle.HalfPadding);
            ElementBounds containerBounds = clipBounds.FlatCopy();

            compo.BeginChildElements(areaBounds)
                .AddInset(insetBounds, 3)
                    .BeginClip(clipBounds)
                        .AddContainer(containerBounds, PermsKey)
                    .EndClip()
                    .AddVerticalScrollbar((value) =>
                    {
                        ElementBounds bounds = compo.GetContainer(PermsKey).Bounds;
                        bounds.fixedY = 0 - value;
                        bounds.CalcWorldBounds();
                    }, scrollbarBounds, PermsScrollKey)
                .EndChildElements();

            GuiElementContainer scrollArea = compo.GetContainer(PermsKey);
            List<string> permNames = rank.Permissions.Select(v => v.ToString()).ToList();

            if (permNames.Count == 0)
            {
                scrollArea.Add(new GuiElementRichtext(compo.Api,
                    VtmlUtil.Richtextify(compo.Api, Lang.Get("claims:gui-rank-no-permissions"), CairoFont.WhiteDetailText()),
                    ElementBounds.Fixed(4, 4, areaBounds.fixedWidth - 30, RowHeight)));
            }
            else
            {
                ElementBounds rowBounds = ElementBounds.Fixed(4, 4, areaBounds.fixedWidth - 30, RowHeight);
                foreach (var name in permNames)
                {
                    scrollArea.Add(new GuiElementRichtext(compo.Api,
                        VtmlUtil.Richtextify(compo.Api, name, CairoFont.WhiteDetailText()), rowBounds));
                    rowBounds = rowBounds.BelowCopy();
                }
            }

            ctx.AfterCompose(() =>
                compo.GetScrollbar(PermsScrollKey).SetHeights((float)clipBounds.fixedHeight, RowHeight * permNames.Count + 8));
        }
    }
}
