using System;
using Cairo;
using claims.src.auxialiry;
using claims.src.gui.playerGui.GuiElements;
using claims.src.gui.playerGui.structures.cellElements;
using Vintagestory.API.Client;
using Vintagestory.API.Common;
using Vintagestory.API.Config;
using Vintagestory.Client.NoObf;
using static claims.src.gui.playerGui.CANClaimsGui;

namespace claims.src.gui.playerGui.GuiPages
{
    public static class CityPage
    {
        public static void BuildCityPage(CANClaimsGui gui, ElementBounds currentBounds, ElementBounds lineBounds)
        {
            var compo = gui.SingleComposer;
            currentBounds = currentBounds.BelowCopy(0, 40);
            if (claims.clientDataStorage.clientPlayerInfo?.CityInfo != null && claims.clientDataStorage.clientPlayerInfo?.CityInfo.Name != "")
            {
                var clientInfo = claims.clientDataStorage.clientPlayerInfo;
                var cityTabFont = CairoFont.ButtonText().WithFontSize(20).WithOrientation(EnumTextOrientation.Left);
                TextExtents textExtents = CairoFont.ButtonText().GetTextExtents(clientInfo.CityInfo.Name);
                var cityNameBounds = currentBounds.FlatCopy().WithFixedWidth(textExtents.Width + 10);

                compo.AddButton(clientInfo.CityInfo.Name, new ActionConsumable(() =>
                {
                    gui.CreateNewCityState = EnumUpperWindowSelectedState.SELECT_NEW_CITY_NAME;
                    gui.BuildUpperWindow();
                    return true;
                }), cityNameBounds, EnumButtonStyle.Normal);


                currentBounds = currentBounds.BelowCopy(0, 10);
                compo.AddStaticText(Lang.Get("claims:gui-mayor-name", clientInfo.CityInfo.MayorName),
                    cityTabFont,
                    currentBounds, "mayorName");

                currentBounds = currentBounds.BelowCopy();
                compo.AddStaticText(Lang.Get("claims:gui-date-created", TimeFunctions.getDateFromEpochSeconds(clientInfo.CityInfo.TimeStampCreated)),
                    cityTabFont,
                    currentBounds, "createdAt");

                currentBounds = currentBounds.BelowCopy();
                currentBounds.fixedWidth /= 2;
                currentBounds.WithAlignment(EnumDialogArea.LeftTop);

                clientInfo.CityInfo.MaxCountPlots.TryGetValue("base", out int baseAmount);
                clientInfo.CityInfo.MaxCountPlots.TryGetValue("bonus", out int bonus);
                clientInfo.CityInfo.MaxCountPlots.TryGetValue("alliance", out int alliance);
                string langVal;
                if (bonus > 0 && alliance > 0)
                {
                    langVal = Lang.Get("claims:gui-claimed-max-plots-with-bonus-alliance",
                        clientInfo.CityInfo.CountPlots, baseAmount + bonus + alliance, bonus, alliance);
                }
                else if (bonus > 0)
                {
                    langVal = Lang.Get("claims:gui-claimed-max-plots-with-bonus",
                        clientInfo.CityInfo.CountPlots, baseAmount + bonus + alliance, bonus);
                }
                else if (alliance > 0)
                {
                    langVal = Lang.Get("claims:gui-claimed-max-plots-with-alliance",
                        clientInfo.CityInfo.CountPlots, baseAmount + bonus + alliance, alliance);
                }
                else
                {
                    langVal = Lang.Get("claims:gui-claimed-max-plots", clientInfo.CityInfo.CountPlots, baseAmount);
                }
                compo.AddStaticText(langVal,
                    cityTabFont,
                    currentBounds, "claimedPlotsToMax");

                ElementBounds claimCityPlotButtonBounds = currentBounds.RightCopy();
                claimCityPlotButtonBounds.WithFixedWidth(25).WithFixedHeight(25);
                ElementBounds unclaimCityPlotButtonBounds = claimCityPlotButtonBounds.RightCopy();
                compo.AddIconButton("plus", (bool t) =>
                {
                    if (t)
                    {
                        gui.CreateNewCityState = EnumUpperWindowSelectedState.CLAIM_CITY_PLOT_CONFIRM;
                        gui.BuildUpperWindow();
                    }
                }, claimCityPlotButtonBounds);

                compo.AddIconButton("line", (bool t) =>
                {
                    if (t)
                    {
                        gui.CreateNewCityState = EnumUpperWindowSelectedState.UNCLAIM_CITY_PLOT_CONFIRM;
                        gui.BuildUpperWindow();
                    }
                }, unclaimCityPlotButtonBounds);

                /*==============================================================================================*/
                /*=====================================CITY PERMISSIONS=========================================*/
                /*==============================================================================================*/
                ElementBounds cityPermBounds = unclaimCityPlotButtonBounds.RightCopy();
                ElementBounds showPermissionsButtonBounds = currentBounds.RightCopy();
                cityPermBounds.WithFixedSize(25, 25);
                compo.AddIconButton("medal", (bool t) =>
                {
                    if (t)
                    {
                        gui.CreateNewCityState = EnumUpperWindowSelectedState.CITY_PLOTS_PERMISSIONS;
                        gui.BuildUpperWindow();
                    }
                    else
                    {
                        gui.CreateNewCityState = EnumUpperWindowSelectedState.NONE;

                        gui.BuildUpperWindow();
                    }
                }, cityPermBounds, "setPermissions");
                compo.GetToggleButton("setPermissions").Toggleable = true;



                currentBounds = currentBounds.BelowCopy(0, 25);

                compo.AddStaticText(Lang.Get("claims:gui-city-population", clientInfo.CityInfo.PlayersNames.Count),
                    cityTabFont,
                    currentBounds, "population");

                compo.AddHoverText(StringFunctions.concatStringsWithDelim(clientInfo.CityInfo.PlayersNames, ','),
                                            CairoFont.ButtonText(),
                                            (int)currentBounds.fixedWidth, currentBounds);
                //compo.AddInset(currentBounds);
                ElementBounds addCitizenButtonBounds = currentBounds.RightCopy();
                addCitizenButtonBounds.WithFixedWidth(25).WithFixedHeight(25);
                ElementBounds removeCitizenButtonBounds = addCitizenButtonBounds.RightCopy();
                ElementBounds uninviteCitizenButtonBounds = removeCitizenButtonBounds.RightCopy();
                compo.AddIconButton("plus", (bool t) =>
                {
                    if (t)
                    {
                        gui.CreateNewCityState = EnumUpperWindowSelectedState.INVITE_TO_CITY_NEED_NAME;
                        gui.BuildUpperWindow();
                    }
                }, addCitizenButtonBounds);

                compo.AddIconButton("line", (bool t) =>
                {
                    if (t)
                    {
                        gui.CreateNewCityState = EnumUpperWindowSelectedState.KICK_FROM_CITY_NEED_NAME;
                        gui.BuildUpperWindow();
                    }
                }, removeCitizenButtonBounds);

                compo.AddIconButton("eraser", (bool t) =>
                {
                    if (t)
                    {
                        gui.CreateNewCityState = EnumUpperWindowSelectedState.UNINVITE_TO_CITY;
                        gui.BuildUpperWindow();
                    }
                }, uninviteCitizenButtonBounds);

                if (claims.config.SELECTED_ECONOMY_HANDLER == "VIRTUAL_MONEY")
                {
                    currentBounds = currentBounds.BelowCopy(0, 5);

                    if (claims.clientDataStorage.clientPlayerInfo.PlayerPermissions.HasPermission(rights.EnumPlayerPermissions.CITY_SEE_BALANCE))
                    {
                        compo.AddStaticText(Lang.Get("claims:gui-city-balance", clientInfo.CityInfo.CityBalance),
                            cityTabFont,
                            currentBounds, "cityBalance");
                    }
                }
                if (claims.config.GUI_SHOW_DEBT && clientInfo.CityInfo.CityDebt > 0)
                {
                    currentBounds = currentBounds.BelowCopy(0, 5);
                    compo.AddStaticText(Lang.Get("claims:gui-city-debt", clientInfo.CityInfo.CityDebt),
                        cityTabFont,
                        currentBounds, "cityDebt");
                }

                if (clientInfo.CityInfo.CityDayPayment > 0)
                {
                    currentBounds = currentBounds.BelowCopy(0, 5);
                    compo.AddStaticText(Lang.Get("claims:gui-city-payment", clientInfo.CityInfo.CityDayPayment),
                        cityTabFont,
                        currentBounds, "cityPayment");
                }


                /*==============================================================================================*/
                /*=====================================UNDER 2 LINE=============================================*/
                /*==============================================================================================*/
                var line2Bounds = currentBounds.BelowCopy(0, 20).WithFixedHeight(5).WithFixedWidth(lineBounds.fixedWidth);
                line2Bounds.fixedX = 0;
                line2Bounds.fixedY = gui.mainBounds.fixedHeight * 0.85;
                compo.AddInset(line2Bounds);

                ElementBounds nextIconBounds = line2Bounds.BelowCopy().WithFixedSize(48, 48).WithAlignment(EnumDialogArea.LeftTop);
                nextIconBounds.fixedX = 0;
                nextIconBounds.fixedY = gui.mainBounds.fixedHeight * 0.90;



                compo.AddIconButton("claims:exit-door", new Action<bool>((b) =>
                {
                    gui.CreateNewCityState = EnumUpperWindowSelectedState.LEAVE_CITY_CONFIRM;
                    gui.BuildUpperWindow();
                    return;
                }), nextIconBounds);

                nextIconBounds = nextIconBounds.RightCopy(20);

                if (claims.clientDataStorage.clientPlayerInfo.PlayerPermissions.HasPermission(rights.EnumPlayerPermissions.CITY_REMOVE_RANK) ||
                    claims.clientDataStorage.clientPlayerInfo.PlayerPermissions.HasPermission(rights.EnumPlayerPermissions.CITY_SET_RANK))
                {
                    compo.AddIconButton("claims:achievement", new Action<bool>((b) =>
                    {
                        gui.SelectedTab = EnumSelectedTab.Ranks;
                        gui.BuildMainWindow();
                        return;
                    }), nextIconBounds);
                    nextIconBounds = nextIconBounds.RightCopy(20);
                }

                if (claims.clientDataStorage.clientPlayerInfo.PlayerPermissions.HasPermission(rights.EnumPlayerPermissions.CITY_SET_PLOTS_COLOR))
                {
                    compo.AddIconButton("claims:large-paint-brush", new Action<bool>((b) =>
                    {
                        gui.SelectedTab = EnumSelectedTab.CityPlotsColorSelector;
                        gui.BuildMainWindow();
                        return;
                    }), nextIconBounds);
                    nextIconBounds = nextIconBounds.RightCopy(20);
                }
                compo.AddIconButton("claims:vertical-banner", new Action<bool>((b) =>
                {
                    gui.SelectedTab = EnumSelectedTab.AllianceInfoPage;
                    gui.BuildMainWindow();
                    return;
                }), nextIconBounds);
                nextIconBounds = nextIconBounds.RightCopy(20);

                compo.AddIconButton("claims:envelope", (bool t) =>
                {
                    if (t)
                    {
                        gui.ConflictSourceTab = EnumSelectedTab.City;
                        gui.SelectedTab = EnumSelectedTab.ConflictLettersPage;
                        gui.BuildMainWindow();
                    }
                }, nextIconBounds);
                nextIconBounds = nextIconBounds.RightCopy(20);

                compo.AddIconButton("claims:frog-mouth-helm", (bool t) =>
                {
                    if (t)
                    {
                        gui.ConflictSourceTab = EnumSelectedTab.City;
                        gui.SelectedTab = EnumSelectedTab.ConflictsPage;
                        gui.BuildMainWindow();
                    }
                }, nextIconBounds);
                nextIconBounds = nextIconBounds.RightCopy(20);
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
                    gui.CreateNewCityState = EnumUpperWindowSelectedState.NEED_NAME;
                    if (gui.CreateNewCityState != EnumUpperWindowSelectedState.NONE)
                    {
                        gui.BuildUpperWindow();
                    }
                    return;
                }), crownButtonBounds);
                TextExtents textExtents = CairoFont.WhiteSmallText().GetTextExtents(Lang.Get("claims:gui-new-city-button"));
                compo.AddHoverText(Lang.Get("claims:gui-new-city-button"),
                                        CairoFont.WhiteSmallText().WithOrientation(EnumTextOrientation.Center),
                                        (int)textExtents.Width, crownButtonBounds);

                if (claims.clientDataStorage.clientPlayerInfo.ReceivedInvitations.Count > 0)
                {
                    int numClaimsToSkip = gui.selectedClaimsPage * gui.claimsPerPage;
                    ElementBounds topTextBounds = ElementBounds.Fixed(GuiStyle.ElementToDialogPadding, 40, createCityBounds.fixedWidth - 30, 30);

                    ElementBounds logtextBounds = ElementBounds.Fixed(0, 0, createCityBounds.fixedWidth - 30, gui.mainBounds.fixedHeight - 230).FixedUnder(topTextBounds, 5);
                    ElementBounds invitationTextBounds = createCityBounds.BelowCopy();
                    invitationTextBounds.fixedHeight -= 50;
                    invitationTextBounds.WithAlignment(EnumDialogArea.CenterTop);
                    ElementBounds clippingBounds = logtextBounds.ForkBoundingParent();

                    ElementBounds insetBounds = logtextBounds.FlatCopy().FixedGrow(6).WithFixedOffset(-3, -3);

                    ElementBounds scrollbarBounds = insetBounds.CopyOffsetedSibling(logtextBounds.fixedWidth + 7).WithFixedWidth(20);

                    compo.AddStaticText("Invitations" + " [" + claims.clientDataStorage.clientPlayerInfo.ReceivedInvitations.Count + "]",
                        CairoFont.WhiteMediumText().WithOrientation(EnumTextOrientation.Center),
                        invitationTextBounds);


                    gui.clippingInvitationsBounds = insetBounds.ForkContainingChild(3.0, 3.0, 3.0, 3.0);

                    compo.BeginChildElements(invitationTextBounds.BelowCopy())
                    .BeginClip(clippingBounds)
                    .AddInset(insetBounds, 3)
                    .AddCellList(gui.listInvitationsBounds = gui.clippingInvitationsBounds.ForkContainingChild(0.0, 0.0, 0.0, -3.0).WithFixedPadding(5.0),
                    (ClientToCityInvitation cell, ElementBounds bounds) =>
                    {
                        return new GuiElementCityInvitation(compo.Api, cell, bounds)
                        {
                            //"claims:textures/icons/warning.svg")
                            On = true,
                            OnMouseDownOnCellLeft = new Action<int>(OnClickCellLeft),
                            OnMouseDownOnCellMiddle = new Action<int>(OnClickCellMiddle),
                            OnMouseDownOnCellRight = new Action<int>(OnClickCellRight)
                        };
                    }, claims.clientDataStorage.clientPlayerInfo.ReceivedInvitations, "modstable")
                    .EndClip()
                    .AddVerticalScrollbar((float value) =>
                    {
                        ElementBounds bounds = compo.GetCellList<ClientToCityInvitation>("modstable").Bounds;
                        bounds.fixedY = (double)(0f - value);
                        bounds.CalcWorldBounds();
                    }, scrollbarBounds, "scrollbar")
                //.AddSmallButton("Close", OnButtonClose, closeButtonBounds)
                .EndChildElements()

                .Compose();

                    compo.GetScrollbar("scrollbar").SetHeights((float)gui.clippingInvitationsBounds.fixedHeight, (float)gui.listInvitationsBounds.fixedHeight);
                }
            }
        }
        public static void BuildPlotColorSelectorPage(CANClaimsGui gui, ElementBounds currentBounds, ElementBounds lineBounds)
        {
            var compo = gui.SingleComposer;
            if (claims.clientDataStorage.clientPlayerInfo.CityInfo == null)
            {
                compo.Compose();
                return;
            }
            currentBounds = currentBounds.BelowCopy(0, 40);
            var colorSelectTabFont = CairoFont.ButtonText().WithFontSize(20).WithOrientation(EnumTextOrientation.Left);
            TextExtents textExtents = colorSelectTabFont.GetTextExtents("Plots color: ");
            currentBounds.fixedWidth = textExtents.Width;
            currentBounds.Alignment = EnumDialogArea.LeftTop;

            compo.AddStaticText("Plots color: ",
                                           colorSelectTabFont, currentBounds);

            ElementBounds bounds = currentBounds.RightCopy(10, 5).WithFixedSize(24, 24);

            compo.AddColorListPicker(new int[] { claims.clientDataStorage.clientPlayerInfo.CityInfo.PlotsColor }
                                                            , OnColorPicked,
                                                           bounds, 100);

            currentBounds = currentBounds.BelowCopy();
            currentBounds.fixedWidth = lineBounds.fixedWidth * 0.66;
            compo.AddStaticText("Select color: ",
                                          colorSelectTabFont, currentBounds);

            ElementBounds selectColorBounds = currentBounds.FlatCopy();
            selectColorBounds.fixedWidth = lineBounds.fixedWidth * 0.66;
            compo.AddColorListPicker(claims.config.PLOT_COLORS == null ? new int[] { 0, 8888888 } : claims.config.PLOT_COLORS
                                                           , OnColorPicked,
                                                          bounds = selectColorBounds.BelowCopy(0.0, 0.0, 0.0, 0.0).WithFixedSize((double)25, (double)25), (int)selectColorBounds.fixedWidth, "picker-2");

            ElementBounds colorSelectedButton = bounds.BelowCopy().WithFixedSize(48, 48);
            colorSelectedButton.fixedX = 0;
            colorSelectedButton.fixedY += 20;
            compo.AddButton("Select", new ActionConsumable(() =>
            {
                if (gui.selectedColor == -1)
                {
                    return true;
                }
                ClientEventManager clientEventManager = (claims.capi.World as ClientMain).eventManager;
                clientEventManager.TriggerNewClientChatLine(GlobalConstants.CurrentChatGroup, "/city set colorint " + gui.selectedColor, EnumChatType.Macro, "");
                gui.selectedColor = -1;
                return true;
            }),
                             colorSelectedButton);
        }
        public static void BuildCitiesListPage(CANClaimsGui gui, ElementBounds currentBounds, ElementBounds lineBounds)
        {
            var compo = gui.SingleComposer;
            var criminalsTabFont = CairoFont.ButtonText().WithFontSize(20).WithOrientation(EnumTextOrientation.Left);
            currentBounds.fixedWidth /= 2;
            currentBounds.WithAlignment(EnumDialogArea.LeftTop);

            var clientInfo = claims.clientDataStorage.clientPlayerInfo;
            currentBounds.fixedWidth = lineBounds.fixedWidth;

            currentBounds = currentBounds.BelowCopy(0, 0);
            ElementBounds createCityBounds = currentBounds.FlatCopy();
            int numClaimsToSkip = gui.selectedClaimsPage * gui.claimsPerPage;
            ElementBounds topTextBounds = ElementBounds.Fixed(GuiStyle.ElementToDialogPadding, 40, createCityBounds.fixedWidth - 30, 30);

            ElementBounds logtextBounds = ElementBounds.Fixed(0, 0, createCityBounds.fixedWidth - 30, gui.mainBounds.fixedHeight - 250).FixedUnder(topTextBounds, 5);
            ElementBounds invitationTextBounds = createCityBounds.BelowCopy();

            invitationTextBounds.fixedHeight -= 50;
            invitationTextBounds.WithAlignment(EnumDialogArea.CenterTop);
            ElementBounds clippingBounds = logtextBounds.ForkBoundingParent();

            ElementBounds insetBounds = logtextBounds.FlatCopy().FixedGrow(6).WithFixedOffset(-3, -3);

            ElementBounds scrollbarBounds = insetBounds.CopyOffsetedSibling(logtextBounds.fixedWidth + 7).WithFixedWidth(20);

            compo.AddStaticText("Cities",
                CairoFont.WhiteMediumText().WithOrientation(EnumTextOrientation.Center),
                invitationTextBounds);

            gui.clippingRansksBounds = insetBounds.ForkContainingChild(3.0, 3.0, 3.0, 3.0);

            compo.BeginChildElements(invitationTextBounds)
                .BeginClip(clippingBounds)
                .AddInset(insetBounds, 3)
                .AddCellList(gui.listRanksBounds = gui.clippingRansksBounds.
                ForkContainingChild(0.0, 0.0, 0.0, -3.0).WithFixedPadding(5.0),
                (ClientCityInfoCellElement cell, ElementBounds bounds) =>
                {
                    return new GuiElementCityStatCell(compo.Api, cell, bounds)
                    {
                        On = true
                    };
                },
                claims.clientDataStorage.clientPlayerInfo.AllCitiesList, "citystatcells")
                .EndClip()
                .AddVerticalScrollbar((float value) =>
                {
                    ElementBounds bounds = compo.GetCellList<ClientCityInfoCellElement>("citystatcells").Bounds;
                    bounds.fixedY = (double)(0f - value);
                    bounds.CalcWorldBounds();
                }, scrollbarBounds, "scrollbar")
                .EndChildElements();
            compo.GetCellList<ClientCityInfoCellElement>("citystatcells").BeforeCalcBounds(); ;

            compo.Compose();

            compo.GetScrollbar("scrollbar").SetHeights((float)gui.clippingRansksBounds.fixedHeight, (float)gui.listRanksBounds.fixedHeight);
        }
        public static void OnColorPicked(int index)
        {
            claims.CANCityGui.selectedColor = claims.config.PLOT_COLORS[index];
        }
        private static void OnClickCellLeft(int cellIndex)
        {

        }
        private static void OnClickCellMiddle(int cellIndex)
        {
            ClientEventManager clientEventManager = (claims.capi.World as ClientMain).eventManager;
            clientEventManager.TriggerNewClientChatLine(GlobalConstants.CurrentChatGroup, "/accept " + claims.clientDataStorage.clientPlayerInfo?.ReceivedInvitations[cellIndex].CityName, EnumChatType.Macro, "");
        }
        private static void OnClickCellRight(int cellIndex)
        {
            ClientEventManager clientEventManager = (claims.capi.World as ClientMain).eventManager;
            clientEventManager.TriggerNewClientChatLine(GlobalConstants.CurrentChatGroup, "/deny " + claims.clientDataStorage.clientPlayerInfo?.ReceivedInvitations[cellIndex].CityName, EnumChatType.Macro, "");
        }
    }
}
