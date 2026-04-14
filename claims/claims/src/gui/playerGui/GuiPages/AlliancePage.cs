using System;
using Cairo;
using claims.src.auxialiry;
using claims.src.gui.playerGui.GuiElements;
using claims.src.gui.playerGui.structures.cellElements;
using Vintagestory.API.Client;
using Vintagestory.API.Common;
using Vintagestory.API.Config;
using static claims.src.gui.playerGui.CANClaimsGui;

namespace claims.src.gui.playerGui.GuiPages
{
    public static class AlliancePage
    {
        public static void BuildAlliancePage(CANClaimsGui gui, ElementBounds currentBounds, ElementBounds lineBounds)
        {
            var compo = gui.SingleComposer;
            currentBounds = currentBounds.BelowCopy(0, 40);
            if (claims.clientDataStorage.clientPlayerInfo?.AllianceInfo != null)
            {
                var clientInfo = claims.clientDataStorage.clientPlayerInfo;
                var allianceTabFont = CairoFont.ButtonText().WithFontSize(20).WithOrientation(EnumTextOrientation.Left);
                TextExtents textExtents = CairoFont.ButtonText().GetTextExtents(clientInfo.AllianceInfo.Name);
                var allianceNameBounds = currentBounds.FlatCopy().WithFixedWidth(textExtents.Width + 10);

                compo.AddButton(clientInfo.AllianceInfo.Name, new ActionConsumable(() =>
                {
                    gui.CreateNewCityState = EnumUpperWindowSelectedState.SELECT_NEW_ALLIANCE_NAME;
                    gui.BuildUpperWindow();
                    return true;
                }), allianceNameBounds, EnumButtonStyle.Normal);


                currentBounds = currentBounds.BelowCopy(0, 10);
                compo.AddStaticText(Lang.Get("claims:gui-leader-name", clientInfo.AllianceInfo.LeaderName),
                    allianceTabFont,
                    currentBounds, "leaderName");

                currentBounds = currentBounds.BelowCopy();
                compo.AddStaticText(Lang.Get("claims:gui-date-created", TimeFunctions.getDateFromEpochSeconds(clientInfo.AllianceInfo.TimeStampCreated)),
                    allianceTabFont,
                    currentBounds, "createdAt");

                currentBounds = currentBounds.BelowCopy(0, 5);
                currentBounds.fixedWidth /= 2;
                currentBounds.Alignment = EnumDialogArea.LeftFixed;
                compo.AddStaticText(Lang.Get("claims:gui-alliance-cities-list", clientInfo.AllianceInfo.Cities.Count),
                    allianceTabFont,
                    currentBounds, "cities-count");

                compo.AddHoverText(StringFunctions.concatStringsWithDelim(clientInfo.AllianceInfo.Cities, ','),
                                            CairoFont.ButtonText(),
                                            (int)currentBounds.fixedWidth, currentBounds);
                //compo.AddInset(currentBounds);
                ElementBounds inviteCityButtonBounds = currentBounds.RightCopy();
                inviteCityButtonBounds.WithFixedWidth(25).WithFixedHeight(25);
                ElementBounds kickCityButtonBounds = inviteCityButtonBounds.RightCopy();
                ElementBounds uninviteCityButtonBounds = kickCityButtonBounds.RightCopy();
                compo.AddIconButton("plus", (bool t) =>
                {
                    if (t)
                    {
                        gui.CreateNewCityState = EnumUpperWindowSelectedState.INVITE_TO_ALLIANCE_NEED_NAME;
                        gui.BuildUpperWindow();
                    }
                }, inviteCityButtonBounds);

                compo.AddIconButton("line", (bool t) =>
                {
                    if (t)
                    {
                        gui.CreateNewCityState = EnumUpperWindowSelectedState.KICK_FROM_ALLIANCE_NEED_NAME;
                        gui.BuildUpperWindow();
                    }
                }, kickCityButtonBounds);

                compo.AddIconButton("eraser", (bool t) =>
                {
                    if (t)
                    {
                        gui.CreateNewCityState = EnumUpperWindowSelectedState.UNINVITE_TO_ALLIANCE;
                        gui.BuildUpperWindow();
                    }
                }, uninviteCityButtonBounds);

                currentBounds = currentBounds.BelowCopy(0, 5);

                compo.AddStaticText(Lang.Get("claims:gui-alliance-balance", clientInfo.AllianceInfo.Balance),
                       CairoFont.ButtonText(),
                       EnumTextOrientation.Left,
                       currentBounds, "allianceBalance");

                currentBounds = currentBounds.BelowCopy(0, 5);

                compo.AddStaticText(Lang.Get("claims:gui-alliance-prefix", clientInfo.AllianceInfo.Prefix),
                       CairoFont.ButtonText(),
                       EnumTextOrientation.Left,
                       currentBounds, "alliancePreifx");

                ElementBounds changeAlliancePrefixButtonBounds = currentBounds.RightCopy();
                changeAlliancePrefixButtonBounds.WithFixedWidth(25).WithFixedHeight(25);
                compo.AddIconButton("claims:soldering-iron", (bool t) =>
                {
                    if (t)
                    {
                        gui.CreateNewCityState = EnumUpperWindowSelectedState.ALLIANCE_PREFIX_NEED_NAME;
                        gui.BuildUpperWindow();
                    }
                }, changeAlliancePrefixButtonBounds);

                /*==============================================================================================*/
                /*=====================================UNDER 2 LINE=============================================*/
                /*==============================================================================================*/
                var line2Bounds = currentBounds.BelowCopy(0, 20).WithFixedHeight(5).WithFixedWidth(lineBounds.fixedWidth);
                line2Bounds.fixedX = 0;
                line2Bounds.fixedY = gui.mainBounds.fixedHeight * 0.85;
                compo.AddInset(line2Bounds);

                ElementBounds nextIconBounds = line2Bounds.BelowCopy().WithFixedSize(48, 48).WithAlignment(EnumDialogArea.LeftTop);
                nextIconBounds.fixedX = 15;
                nextIconBounds.fixedY = gui.mainBounds.fixedHeight * 0.90;



                compo.AddIconButton("claims:exit-door", new Action<bool>((b) =>
                {
                    gui.CreateNewCityState = EnumUpperWindowSelectedState.LEAVE_ALLIANCE_CONFIRM;
                    gui.BuildUpperWindow();
                    return;
                }), nextIconBounds);

                ElementBounds conflictLettersButtonBounds = nextIconBounds.RightCopy(15).WithFixedSize(48, 48);
                compo.AddIconButton("claims:envelope", (bool t) =>
                {
                    if (t)
                    {
                        gui.ConflictSourceTab = EnumSelectedTab.AllianceInfoPage;
                        gui.SelectedTab = EnumSelectedTab.ConflictLettersPage;
                        gui.BuildMainWindow();
                    }
                }, conflictLettersButtonBounds);

                ElementBounds conflictsButtonBounds = conflictLettersButtonBounds.RightCopy(15).WithFixedSize(48, 48);
                compo.AddIconButton("claims:frog-mouth-helm", (bool t) =>
                {
                    if (t)
                    {
                        gui.ConflictSourceTab = EnumSelectedTab.AllianceInfoPage;
                        gui.SelectedTab = EnumSelectedTab.ConflictsPage;
                        gui.BuildMainWindow();
                    }
                }, conflictsButtonBounds);
            }
            else
            {
                //add "new city" button which leads to additional window with input field
                //on ok send commands with name

                ElementBounds createCityBounds = currentBounds.FlatCopy();
                ElementBounds crownButtonBounds = currentBounds.FlatCopy();
                //createCityBounds.
                crownButtonBounds.fixedWidth = 48;
                crownButtonBounds.fixedHeight = 48;
                crownButtonBounds.Alignment = EnumDialogArea.LeftTop;
                crownButtonBounds.fixedY += 10;
                compo.AddIconButton("claims:queen-crown", new Action<bool>((b) =>
                {
                    gui.CreateNewCityState = EnumUpperWindowSelectedState.NEW_ALLIANCE_NEED_NAME;
                    gui.BuildUpperWindow();
                    return;
                }), crownButtonBounds);
                TextExtents textExtents = CairoFont.WhiteSmallText().GetTextExtents(Lang.Get("claims:gui-new-alliance-button"));
                compo.AddHoverText(Lang.Get("claims:gui-new-alliance-button"),
                                        CairoFont.WhiteSmallText().WithOrientation(EnumTextOrientation.Center),
                                        (int)textExtents.Width, crownButtonBounds);
                var clientInfo = claims.clientDataStorage.clientPlayerInfo;
                currentBounds.fixedWidth = lineBounds.fixedWidth;

                int numClaimsToSkip = gui.selectedClaimsPage * gui.claimsPerPage;
                ElementBounds topTextBounds = ElementBounds.Fixed(GuiStyle.ElementToDialogPadding, 40, createCityBounds.fixedWidth - 30, 30);

                ElementBounds logtextBounds = ElementBounds.Fixed(0, 0, createCityBounds.fixedWidth - 30, gui.mainBounds.fixedHeight - 250).FixedUnder(topTextBounds, 5);
                ElementBounds invitationTextBounds = createCityBounds.BelowCopy();

                invitationTextBounds.fixedHeight -= 50;
                invitationTextBounds.WithAlignment(EnumDialogArea.CenterTop);
                ElementBounds clippingBounds = logtextBounds.ForkBoundingParent();

                ElementBounds insetBounds = logtextBounds.FlatCopy().FixedGrow(6).WithFixedOffset(-3, -3);

                ElementBounds scrollbarBounds = insetBounds.CopyOffsetedSibling(logtextBounds.fixedWidth + 7).WithFixedWidth(20);

                compo.AddStaticText("To alliancies invites",
                    CairoFont.WhiteMediumText().WithOrientation(EnumTextOrientation.Center),
                    invitationTextBounds);

                gui.clippingRansksBounds = insetBounds.ForkContainingChild(3.0, 3.0, 3.0, 3.0);

                compo.BeginChildElements(invitationTextBounds)
                    .BeginClip(clippingBounds)
                    .AddInset(insetBounds, 3)
                    .AddCellList(gui.listRanksBounds = gui.clippingRansksBounds.
                    ForkContainingChild(0.0, 0.0, 0.0, -3.0).WithFixedPadding(5.0),
                    (ClientToAllianceInvitationCellElement cell, ElementBounds bounds) =>
                    {
                        return new GuiElementToAllianceInvitation(compo.Api, cell, bounds)
                        {
                            On = true
                        };
                    },
                    claims.clientDataStorage.clientPlayerInfo.CityInfo.ClientToAllianceInvitations, "toallianciesinvitescells")
                    .EndClip()
                    .AddVerticalScrollbar((float value) =>
                    {
                        ElementBounds bounds = compo.GetCellList<ClientToAllianceInvitationCellElement>("toallianciesinvitescells").Bounds;
                        bounds.fixedY = (double)(0f - value);
                        bounds.CalcWorldBounds();
                    }, scrollbarBounds, "scrollbar")
                    .EndChildElements();
                var c = compo.GetCellList<ClientToAllianceInvitationCellElement>("toallianciesinvitescells");
                c.BeforeCalcBounds();

                compo.Compose();

                compo.GetScrollbar("scrollbar").SetHeights((float)gui.clippingRansksBounds.fixedHeight, (float)gui.listRanksBounds.fixedHeight);
            }
        }
    }
}
