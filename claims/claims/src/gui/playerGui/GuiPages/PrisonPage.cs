using claims.src.auxialiry;
using claims.src.gui.playerGui.GuiElements;
using claims.src.gui.playerGui.structures.cellElements;
using claims.src.rights;
using Vintagestory.API.Client;
using Vintagestory.API.Config;
using static claims.src.gui.playerGui.CANClaimsGui;

namespace claims.src.gui.playerGui.GuiPages
{
    public static class PrisonPage
    {
        public static void BuildPrisonPage(CANClaimsGui gui, ElementBounds currentBounds, ElementBounds lineBounds)
        {
            var compo = gui.SingleComposer;
            var criminalsTabFont = CairoFont.ButtonText().WithFontSize(20).WithOrientation(EnumTextOrientation.Left);
            currentBounds = currentBounds.BelowCopy(0, 30);
            currentBounds.fixedY += 25;
            currentBounds.fixedWidth /= 2;
            currentBounds.WithAlignment(EnumDialogArea.LeftTop);

            var clientInfo = claims.clientDataStorage.clientPlayerInfo;
            if (clientInfo.CityInfo == null)
            {
                compo.Compose();
                return;
            }
            compo.AddStaticText(Lang.Get("claims:gui-criminals", clientInfo.CityInfo.Criminals.Count),
                        criminalsTabFont,
                        currentBounds, "criminals");

            compo.AddHoverText(StringFunctions.concatStringsWithDelim(clientInfo.CityInfo.Criminals, ','),
                                        criminalsTabFont.WithOrientation(EnumTextOrientation.Center),
                                        (int)currentBounds.fixedWidth, currentBounds);

            ElementBounds addCriminalBounds = currentBounds.RightCopy();
            addCriminalBounds.WithFixedWidth(25).WithFixedHeight(25);
            ElementBounds removeFriendBounds = addCriminalBounds.RightCopy();
            var perms = clientInfo.PlayerPermissions;
            if (perms.HasPermission(EnumPlayerPermissions.CITY_ADD_CRIMINAL))
            {
                compo.AddInset(addCriminalBounds);
                compo.AddIconButton("plus", (bool t) =>
                {
                    if (t)
                    {
                        gui.CreateNewCityState = EnumUpperWindowSelectedState.ADD_CRIMINAL_NEED_NAME;
                        gui.BuildUpperWindow();
                    }
                }, addCriminalBounds);
                compo.AddHoverText(Lang.Get("claims:gui-prison-add-criminal-tooltip"),
                    CairoFont.SmallButtonText(), (int)currentBounds.fixedWidth / 2, addCriminalBounds);
            }
            if (perms.HasPermission(EnumPlayerPermissions.CITY_REMOVE_CRIMINAL))
            {
                compo.AddInset(removeFriendBounds);
                compo.AddIconButton("line", (bool t) =>
                {
                    if (t)
                    {
                        gui.CreateNewCityState = EnumUpperWindowSelectedState.REMOVE_CRIMINAL;
                        gui.BuildUpperWindow();
                    }
                }, removeFriendBounds);
                compo.AddHoverText(Lang.Get("claims:gui-prison-remove-criminal-tooltip"),
                    CairoFont.SmallButtonText(), (int)currentBounds.fixedWidth / 2, removeFriendBounds);
            }

            currentBounds.fixedWidth = lineBounds.fixedWidth;

            ElementBounds createCityBounds = currentBounds.FlatCopy();
            int numClaimsToSkip = gui.selectedClaimsPage * gui.claimsPerPage;
            ElementBounds topTextBounds = ElementBounds.Fixed(GuiStyle.ElementToDialogPadding, 40, createCityBounds.fixedWidth - 30, 30);

            ElementBounds logtextBounds = ElementBounds.Fixed(0, 0, createCityBounds.fixedWidth - 30, gui.mainBounds.fixedHeight - 300).FixedUnder(topTextBounds, 5);
            //logtextBounds.fixedHeight = 150;
            ElementBounds invitationTextBounds = createCityBounds.BelowCopy();

            invitationTextBounds.fixedHeight -= 50;
            invitationTextBounds.WithAlignment(EnumDialogArea.CenterTop);
            ElementBounds clippingBounds = logtextBounds.ForkBoundingParent();

            ElementBounds insetBounds = logtextBounds.FlatCopy().FixedGrow(6).WithFixedOffset(-3, -3);

            ElementBounds scrollbarBounds = insetBounds.CopyOffsetedSibling(logtextBounds.fixedWidth + 7).WithFixedWidth(20);

            compo.AddStaticText(Lang.Get("claims:gui-prison-cells-title"),
                CairoFont.WhiteMediumText().WithOrientation(EnumTextOrientation.Center),
                invitationTextBounds);

            gui.clippingRansksBounds = insetBounds.ForkContainingChild(3.0, 3.0, 3.0, 3.0);

            compo.BeginChildElements(invitationTextBounds)
            .BeginClip(clippingBounds)
            .AddInset(insetBounds, 3)
            .AddCellList(gui.listRanksBounds = gui.clippingRansksBounds.ForkContainingChild(0.0, 0.0, 0.0, -3.0).WithFixedPadding(5.0),
            (PrisonCellElement cell, ElementBounds bounds) =>
            {
                return new GuiElementCityPrisonCell(compo.Api, cell, bounds)
                {

                    On = true
                };
            }, claims.clientDataStorage.clientPlayerInfo.CityInfo.PrisonCells, "prisoncellscells")
            .EndClip()
            .AddVerticalScrollbar((float value) =>
            {
                ElementBounds bounds = compo.GetCellList<PrisonCellElement>("prisoncellscells").Bounds;
                bounds.fixedY = (double)(0f - value);
                bounds.CalcWorldBounds();
            }, scrollbarBounds, "scrollbar")
            .EndChildElements();
            compo.GetCellList<PrisonCellElement>("prisoncellscells").BeforeCalcBounds();

            compo.Compose();

            compo.GetScrollbar("scrollbar").SetHeights((float)gui.clippingRansksBounds.fixedHeight, (float)gui.listRanksBounds.fixedHeight);
        }
    }
}
