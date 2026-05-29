using System;
using System.Collections.Generic;
using System.Linq;
using claims.src.gui.playerGui.GuiPages;
using claims.src.gui.playerGui.structures.cellElements;
using claims.src.part.structure.plots;
using Vintagestory.API.Client;
using Vintagestory.API.Common;
using Vintagestory.API.Config;
using Vintagestory.API.MathTools;
using Vintagestory.Client.NoObf;

namespace claims.src.gui.playerGui
{
    public class CANClaimsGui : GuiDialog
    {
        public override string ToggleKeyCombinationCode => "CANClaimsGui";
        public float Width { get; private set; }
        public float Height { get; private set; }
        public EnumSelectedTab SelectedTab { get; set; }
        public enum EnumUpperWindowSelectedState
        {
            NONE, NEED_NAME, NEED_AGREE, INVITE_TO_CITY_NEED_NAME, KICK_FROM_CITY_NEED_NAME, UNINVITE_TO_CITY,
            CLAIM_CITY_PLOT_CONFIRM, UNCLAIM_CITY_PLOT_CONFIRM, PLOT_PERMISSIONS, ADD_FRIEND_NEED_NAME, REMOVE_FRIEND,
            PLOT_SET_PRICE_NEED_NUMBER, PLOT_SET_TAX, PLOT_SET_TYPE, PLOT_SET_NAME, PLOT_CLAIM, PLOT_UNCLAIM,
            CITY_TITLE_SELECT_CITIZEN, CITY_TITLE_CITIZEN_SELECTED, LEAVE_CITY_CONFIRM,
            CITY_RANK_REMOVE_CONFIRM, CITY_RANK_ADD, SELECT_NEW_CITY_NAME, ADD_CRIMINAL_NEED_NAME, REMOVE_CRIMINAL,
            CITY_PRISON_REMOVE_CELL_CONFIRM, CITY_SUMMON_NEED_NAME, ADD_PLOTSGROUP_MEMBER_NEED_NAME, REMOVE_PLOTSGROUP_MEMBER_SELECT,
            REMOVE_PLOTSGROUP_MEMBER_CONFIRM,
            CITY_PLOTSGROUP_PERMISSIONS, CITY_PLOTSGROUP_PLOT_CLAIM_CONFIRM, CITY_PLOTSGROUP_PLOT_UNCLAIM_CONFIRM,
            CITY_PLOTSGROUP_ADD_NEW_NEED_NAME, CITY_PLOTSGROUP_REMOVE_SELECT, CITY_PLOTSGROUP_REMOVE_CONFIRM,

            SELECT_NEW_ALLIANCE_NAME, INVITE_TO_ALLIANCE_NEED_NAME, KICK_FROM_ALLIANCE_NEED_NAME, UNINVITE_TO_ALLIANCE,
            LEAVE_ALLIANCE_CONFIRM, NEW_ALLIANCE_NEED_NAME, ALLIANCE_PREFIX_NEED_NAME, ALLIANCE_SEND_NEW_CONFLICT_LETTER_NEED_NAME,
            ALLIANCE_SEND_PEACE_OFFER_CONFIRM,
            CITY_PLOTS_PERMISSIONS, CITY_RANK_DELETE_CONFIRM, CITY_RANK_CREATION_NEED_NAME
        }
        public EnumUpperWindowSelectedState CreateNewCityState { get; set; } = EnumUpperWindowSelectedState.NONE;
        public string collectedNewCityName { get; set; } = "";
        public string firstValueCollected { get; set; } = "";
        public string secondValueCollected { get; set; } = "";
        public Vec3i selectedPos { get; set; } = null;
        public string selectedString { get; set; } = "";
        public string secondSelectedString { get; set; } = "";
        public enum EnumSelectedTab
        {
            City, Player, Prices, Plot, Prison, Summon, PlotsGroup, PlotsGroupReceivedInvites, Ranks, RankInfoPage, CityPlotsColorSelector, PlotsGroupInfoPage, 
            AllianceInfoPage, CitiesListPage, ConflictLettersPage, ConflictsPage, ConflictInfoPage
        }
        private Dictionary<EnumSelectedTab, Action<CANClaimsGui, ElementBounds, ElementBounds>> TabDictionary = new();
        public int selectedClaimsPage = 0;
        public int claimsPerPage = 3;
        public int selectedColor = -1;
        public int SelectedTabGroup = 0;
        public EnumSelectedTab ConflictSourceTab = EnumSelectedTab.AllianceInfoPage;
        public enum EnumSelectedWarRangesTab
        {
            APPROVED, SUGGESTIONS
        }
        public ElementBounds clippingInvitationsBounds;
        public ElementBounds listInvitationsBounds;
        public ElementBounds clippingRansksBounds;
        public ElementBounds listRanksBounds;
        public ElementBounds mainBounds;
        public CANClaimsGui(ICoreClientAPI capi) : base(capi)
        {
            Width = 500;
            Height = 600;
            SelectedTab = 0;
            TabDictionary.Add(EnumSelectedTab.PlotsGroupInfoPage, PlotsGroupPage.BuildPlotsGroupInfoPage);
            TabDictionary.Add(EnumSelectedTab.PlotsGroup, PlotsGroupPage.BuildPlotsGroupPage);
            TabDictionary.Add(EnumSelectedTab.Summon, SummonPage.BuildSummonPage);
            TabDictionary.Add(EnumSelectedTab.Prison, PrisonPage.BuildPrisonPage);
            TabDictionary.Add(EnumSelectedTab.CityPlotsColorSelector, CityPage.BuildPlotColorSelectorPage);
            TabDictionary.Add(EnumSelectedTab.Ranks, RanksPage.BuildRanksPage);
            TabDictionary.Add(EnumSelectedTab.RankInfoPage, RanksPage.BuildRanksInfoPage);
            TabDictionary.Add(EnumSelectedTab.Plot, PlotPage.BuildPlotPage);
            TabDictionary.Add(EnumSelectedTab.Prices, PricesPage.BuildPricesPage);
            TabDictionary.Add(EnumSelectedTab.Player, PlayerPage.BuildPlayerPage);
            TabDictionary.Add(EnumSelectedTab.City, CityPage.BuildCityPage);
            TabDictionary.Add(EnumSelectedTab.PlotsGroupReceivedInvites, PlotsGroupPage.BuildPlotsGroupReceivedInvitesPage);
            TabDictionary.Add(EnumSelectedTab.AllianceInfoPage, AlliancePage.BuildAlliancePage);
            TabDictionary.Add(EnumSelectedTab.CitiesListPage, CityPage.BuildCitiesListPage);
            TabDictionary.Add(EnumSelectedTab.ConflictLettersPage, ConflictPage.BuildConflictLettersPage);
            TabDictionary.Add(EnumSelectedTab.ConflictsPage, ConflictPage.BuildConflictsPage);
            TabDictionary.Add(EnumSelectedTab.ConflictInfoPage, ConflictPage.BuildConflictInfoPage);
        }
        public override void OnGuiOpened()
        {
            base.OnGuiOpened();
            BuildMainWindow();
        }
        public override void OnGuiClosed()
        {
            base.OnGuiClosed();
            this.CreateNewCityState = EnumUpperWindowSelectedState.NONE;
        }       
        public void BuildMainWindow()
        {
            int fixedY1 = 20;
            ElementBounds globalBounds = ElementStdBounds.AutosizedMainDialog.WithAlignment(EnumDialogArea.CenterMiddle);

            ElementBounds backgroundBounds = ElementBounds.Fill.WithFixedPadding(GuiStyle.ElementToDialogPadding).WithFixedSize(Width, Height);

            mainBounds = ElementBounds.FixedPos(EnumDialogArea.CenterTop, 0, 15).WithFixedSize(Width, Height);

            ElementBounds leftArrowBounds = ElementBounds.FixedPos(EnumDialogArea.LeftMiddle, 0, 0).WithFixedHeight(50).WithFixedWidth(50);

            ElementBounds rightArrowBounds = ElementBounds.FixedPos(EnumDialogArea.RightMiddle, 0, 0).WithFixedHeight(50).WithFixedWidth(50);

            ElementBounds middleBounds = ElementBounds.FixedPos(EnumDialogArea.CenterMiddle, 0, 0).WithFixedHeight(Height).WithFixedWidth(Width - 100);

            ElementBounds tabNameBounds = ElementBounds.FixedPos(EnumDialogArea.CenterFixed, 0, 0).WithFixedHeight(40).WithFixedWidth(100);

            int fixedY2 = fixedY1 + 28;

            globalBounds.WithChildren(backgroundBounds);
            backgroundBounds.BothSizing = ElementSizing.Fixed;

            backgroundBounds.WithChildren(mainBounds);
            mainBounds.WithChildren(middleBounds, leftArrowBounds, rightArrowBounds);
            middleBounds.WithChildren(leftArrowBounds);
            
            SingleComposer = Composers["canclaimsgui"] = capi.Gui.CreateCompo("canclaimsgui", globalBounds)
                                                                    .AddShadedDialogBG(backgroundBounds)
                                                                    .AddDialogTitleBar(Lang.Get("claims:gui-tab-name"), () => this.TryClose());
            ElementBounds currentBounds = mainBounds.FlatCopy().WithAlignment(EnumDialogArea.CenterTop);
            currentBounds.WithFixedSize(mainBounds.fixedWidth, 40);
            
            mainBounds.WithChildren(currentBounds);
            currentBounds.fixedY += 15;
            bool economyEnabled = !string.IsNullOrEmpty(claims.config?.SELECTED_ECONOMY_HANDLER);

            var tabIcons = new System.Collections.Generic.List<string> { "claims:qaitbay-citadel", "claims:magnifying-glass" };
            tabsOrder = new System.Collections.Generic.List<EnumSelectedTab> { EnumSelectedTab.City, EnumSelectedTab.Player };
            if (economyEnabled)
            {
                tabIcons.Add("claims:price-tag");
                tabsOrder.Add(EnumSelectedTab.Prices);
            }
            tabIcons.AddRange(new[] { "claims:flat-platform", "claims:prisoner", "claims:magic-portal", "claims:huts-village" });
            tabsOrder.AddRange(new[] { EnumSelectedTab.Plot, EnumSelectedTab.Prison, EnumSelectedTab.Summon, EnumSelectedTab.PlotsGroup });

            var tabBounds = new System.Collections.Generic.List<ElementBounds>();
            ElementBounds prev = currentBounds.FlatCopy().WithAlignment(EnumDialogArea.LeftTop).WithFixedSize(48, 48);
            prev.fixedX += 5;
            tabBounds.Add(prev);
            for (int i = 1; i < tabIcons.Count; i++)
            {
                prev = prev.RightCopy(25);
                tabBounds.Add(prev);
            }

            SingleComposer.AddIconToggleButtons(tabIcons.ToArray(),
                                                CairoFont.ButtonText(),
                                                OnTabToggled,
                                                tabBounds.ToArray(),
                                                "selectedTab");

            int selectedIdx = tabsOrder.IndexOf(SelectedTab);
            if (selectedIdx >= 0 && SingleComposer.GetToggleButton("selectedTab-" + selectedIdx) != null)
            {
                SingleComposer.GetToggleButton("selectedTab-" + selectedIdx).SetValue(true);
            }
            
            var lineBounds = currentBounds.BelowCopy(0, 20).WithFixedHeight(5);
            SingleComposer.AddInset(lineBounds);
         
            if(this.TabDictionary.TryGetValue(SelectedTab, out var tabBuilder))
            {
                tabBuilder(this, currentBounds, lineBounds);
            }

            SingleComposer.Compose();
            BuildUpperWindow();
        }
        public void BuildUpperWindow()
        {
            if (!this.IsOpened())
            {
                return;
            }
            if(CreateNewCityState == EnumUpperWindowSelectedState.NONE)
            {
                this.Composers.Remove("canclaimsgui-upper");
                return;
            }
            ElementBounds leftDlgBounds = Composers["canclaimsgui"].Bounds;

            //Composers["canclaimsgui"].Bounds.ParentBounds
            double b = leftDlgBounds.InnerHeight / RuntimeEnv.GUIScale + 10.0;

            ElementBounds bgBounds = ElementBounds.Fixed(0.0, 0.0,
                235, leftDlgBounds.InnerHeight / (double)RuntimeEnv.GUIScale - GuiStyle.ElementToDialogPadding - 20.0 + b).WithFixedPadding(GuiStyle.ElementToDialogPadding);
            ElementBounds dialogBounds = bgBounds.ForkBoundingParent(0.0, 0.0, 0.0, 0.0)
                .WithAlignment(EnumDialogArea.None)
                .WithFixedAlignmentOffset((leftDlgBounds.renderX + leftDlgBounds.OuterWidth + 10.0) / (double)RuntimeEnv.GUIScale,
                                          (leftDlgBounds.renderY) / (double)RuntimeEnv.GUIScale);
            
            bgBounds.BothSizing = ElementSizing.FitToChildren;

            dialogBounds.fixedX += leftDlgBounds.fixedWidth + 20;
            dialogBounds.fixedY = leftDlgBounds.absFixedY;

            dialogBounds.BothSizing = ElementSizing.FitToChildren;
            dialogBounds.WithChild(bgBounds);
            ElementBounds textBounds = ElementBounds.FixedPos(EnumDialogArea.LeftTop,
                                                               0,
                                                                0);
            bgBounds.WithChildren(textBounds);

            Composers["canclaimsgui-upper"] = capi.Gui.CreateCompo("canclaimsgui-upper", dialogBounds)
                                                        .AddShadedDialogBG(bgBounds, false, 5.0, 0.75f);
            var upperComposer = Composers["canclaimsgui-upper"];
            ElementBounds el = textBounds.CopyOffsetedSibling().WithFixedSize(180, 30);
            bgBounds.WithChildren(el);
            SingleComposer.AddInset(el);
            el.fixedY += 20;
            if (CreateNewCityState == EnumUpperWindowSelectedState.NEED_NAME)
            {
                upperComposer.AddStaticText(Lang.Get("claims:gui-enter-new-city-name"),
                                                                        CairoFont.WhiteDetailText(),
                                                                        el);
                ElementBounds inputNameBounds = el.BelowCopy(0, 15);
                bgBounds.WithChildren(inputNameBounds);
                upperComposer.AddTextInput(inputNameBounds,
                    (name) => collectedNewCityName = name, null, "collectedNewCityName");

                ElementBounds enterNameBounds = inputNameBounds.BelowCopy(0, 15);
                bgBounds.WithChildren(enterNameBounds);

                upperComposer.AddButton("-->", new ActionConsumable(() =>
                {
                    ClientEventManager clientEventManager = (claims.capi.World as ClientMain).eventManager;
                    clientEventManager.TriggerNewClientChatLine(GlobalConstants.CurrentChatGroup, "/city new " + collectedNewCityName, EnumChatType.Macro, "");
                    upperComposer.GetTextInput("collectedNewCityName").SetValue("");
                    collectedNewCityName = "";
                    return true;
                }), enterNameBounds, EnumButtonStyle.Normal, "create-new-city-button-enter-name");
            }
            else if(CreateNewCityState == EnumUpperWindowSelectedState.NEED_AGREE)
            {
                upperComposer.AddStaticText(Lang.Get("claims:gui-agree-city-creation", collectedNewCityName),
                CairoFont.WhiteDetailText(),
                el);

                ElementBounds enterNameBounds = el.BelowCopy(0, 15);
                bgBounds.WithChildren(enterNameBounds);

                upperComposer.AddButton("agree", new ActionConsumable(() =>
                {
                    ClientEventManager clientEventManager = (claims.capi.World as ClientMain).eventManager;
                    clientEventManager.TriggerNewClientChatLine(GlobalConstants.CurrentChatGroup, "/agree", EnumChatType.Macro, "");
                    CreateNewCityState = EnumUpperWindowSelectedState.NONE;
                    collectedNewCityName = "";
                    BuildUpperWindow();
                    return true;
                }), enterNameBounds, EnumButtonStyle.Normal, "create-new-city-button-enter-name");
            }
            else if(CreateNewCityState == EnumUpperWindowSelectedState.INVITE_TO_CITY_NEED_NAME)
            {
                upperComposer.AddStaticText("Enter player's name:",
                CairoFont.WhiteDetailText(),
                el);
                ElementBounds inputNameBounds = el.BelowCopy(0, 15);
                bgBounds.WithChildren(inputNameBounds);
                upperComposer.AddTextInput(inputNameBounds,
                    (name) => collectedNewCityName = name, null, "collectedNewCityName");

                ElementBounds enterNameBounds = inputNameBounds.BelowCopy(0, 15);
                bgBounds.WithChildren(enterNameBounds);

                upperComposer.AddButton("Invite", new ActionConsumable(() =>
                {
                    ClientEventManager clientEventManager = (claims.capi.World as ClientMain).eventManager;
                    clientEventManager.TriggerNewClientChatLine(GlobalConstants.CurrentChatGroup, "/city invite " + collectedNewCityName, EnumChatType.Macro, "");
                    upperComposer.GetTextInput("collectedNewCityName").SetValue("");
                    collectedNewCityName = "";
                    CreateNewCityState = EnumUpperWindowSelectedState.NONE;
                    BuildUpperWindow();
                    return true;
                }), enterNameBounds, EnumButtonStyle.Normal);
            }
            else if(CreateNewCityState == EnumUpperWindowSelectedState.KICK_FROM_CITY_NEED_NAME)
            {
                upperComposer.AddStaticText("Enter player's name:",
               CairoFont.WhiteDetailText(),
               el);
                ElementBounds inputNameBounds = el.BelowCopy(0, 15);
                bgBounds.WithChildren(inputNameBounds);
                
                upperComposer.AddDropDown(claims.clientDataStorage.clientPlayerInfo.CityInfo.PlayersNames.ToArray(),
                                                            claims.clientDataStorage.clientPlayerInfo.CityInfo.PlayersNames.ToArray(),  
                                                            -1,
                                                            OnSelectedNameFromDropDown,
                                                            inputNameBounds);

                ElementBounds enterNameBounds = inputNameBounds.BelowCopy(0, 15);
                bgBounds.WithChildren(enterNameBounds);

                upperComposer.AddButton("Kick", new ActionConsumable(() =>
                {
                    ClientEventManager clientEventManager = (claims.capi.World as ClientMain).eventManager;
                    clientEventManager.TriggerNewClientChatLine(GlobalConstants.CurrentChatGroup, "/city kick " + collectedNewCityName, EnumChatType.Macro, "");
                    collectedNewCityName = "";
                    CreateNewCityState = EnumUpperWindowSelectedState.NONE;
                    BuildUpperWindow();
                    return true;
                }), enterNameBounds, EnumButtonStyle.Normal);
            }
            else if (CreateNewCityState == EnumUpperWindowSelectedState.UNINVITE_TO_CITY)
            {
                upperComposer.AddStaticText(Lang.Get("claims:gui-enter-player-name"),
                 CairoFont.WhiteDetailText(),
                 el);
                ElementBounds inputNameBounds = el.BelowCopy(0, 15);
                bgBounds.WithChildren(inputNameBounds);
                upperComposer.AddTextInput(inputNameBounds,
                    (name) => collectedNewCityName = name, null, "collectedNewCityName");

                ElementBounds enterNameBounds = inputNameBounds.BelowCopy(0, 15);
                bgBounds.WithChildren(enterNameBounds);

                upperComposer.AddButton("Uninvite", new ActionConsumable(() =>
                {
                    ClientEventManager clientEventManager = (claims.capi.World as ClientMain).eventManager;
                    clientEventManager.TriggerNewClientChatLine(GlobalConstants.CurrentChatGroup, "/city uninvite " + collectedNewCityName, EnumChatType.Macro, "");
                    upperComposer.GetTextInput("collectedNewCityName").SetValue("");
                    collectedNewCityName = "";
                    CreateNewCityState = EnumUpperWindowSelectedState.NONE;
                    BuildUpperWindow();
                    return true;
                }), enterNameBounds, EnumButtonStyle.Normal);
            }
            else if (CreateNewCityState == EnumUpperWindowSelectedState.CLAIM_CITY_PLOT_CONFIRM)
            {
                upperComposer.AddStaticText("Claim current plot?",
                 CairoFont.WhiteDetailText(),
                 el);
                ElementBounds yesButtonBounds = el.BelowCopy(0, 15);
                yesButtonBounds.fixedWidth /= 2;
                bgBounds.WithChildren(yesButtonBounds);

                upperComposer.AddButton("Yes", new ActionConsumable(() =>
                {
                    ClientEventManager clientEventManager = (claims.capi.World as ClientMain).eventManager;
                    clientEventManager.TriggerNewClientChatLine(GlobalConstants.CurrentChatGroup, "/city claim", EnumChatType.Macro, "");
                    CreateNewCityState = EnumUpperWindowSelectedState.NONE;
                    BuildUpperWindow();
                    return true;
                }), yesButtonBounds, EnumButtonStyle.Normal);

                ElementBounds noButtonBounds = yesButtonBounds.RightCopy(0, 0);
                upperComposer.AddButton("No", new ActionConsumable(() =>
                {
                    CreateNewCityState = EnumUpperWindowSelectedState.NONE;
                    BuildUpperWindow();
                    return true;
                }), noButtonBounds, EnumButtonStyle.Normal);

            }
            else if (CreateNewCityState == EnumUpperWindowSelectedState.UNCLAIM_CITY_PLOT_CONFIRM)
            {
                upperComposer.AddStaticText("Unclaim current plot?",
                 CairoFont.WhiteDetailText(),
                 el);
                ElementBounds yesButtonBounds = el.BelowCopy(0, 15);
                yesButtonBounds.fixedWidth /= 2;
                bgBounds.WithChildren(yesButtonBounds);

                upperComposer.AddButton("Yes", new ActionConsumable(() =>
                {
                    ClientEventManager clientEventManager = (claims.capi.World as ClientMain).eventManager;
                    clientEventManager.TriggerNewClientChatLine(GlobalConstants.CurrentChatGroup, "/city unclaim", EnumChatType.Macro, "");
                    CreateNewCityState = EnumUpperWindowSelectedState.NONE;
                    BuildUpperWindow();
                    return true;
                }), yesButtonBounds, EnumButtonStyle.Normal);

                ElementBounds noButtonBounds = yesButtonBounds.RightCopy(0, 0);
                upperComposer.AddButton("No", new ActionConsumable(() =>
                {
                    CreateNewCityState = EnumUpperWindowSelectedState.NONE;
                    BuildUpperWindow();
                    return true;
                }), noButtonBounds, EnumButtonStyle.Normal);
            }
            else if (CreateNewCityState == EnumUpperWindowSelectedState.PLOT_PERMISSIONS)
            {
                upperComposer.AddStaticText("Plot permissions",
                 CairoFont.WhiteDetailText(),
                 el);
                ElementBounds yesButtonBounds = el.BelowCopy(0, 15);
                yesButtonBounds.fixedWidth /= 2;
                bgBounds.WithChildren(yesButtonBounds);


                ElementBounds pvpToggleTextBounds = el.BelowCopy(0, 15);
                pvpToggleTextBounds.fixedWidth = 80;
                upperComposer.AddStaticText("PVP", CairoFont.WhiteDetailText(), pvpToggleTextBounds);

                ElementBounds pvpToggleButtonBounds = pvpToggleTextBounds.RightCopy(0, 0);
                upperComposer.AddSwitch((t) => 
                                    {
                                        ClientEventManager clientEventManager = (claims.capi.World as ClientMain).eventManager;
                                        clientEventManager.TriggerNewClientChatLine(GlobalConstants.CurrentChatGroup, "/plot set pvp " + (t ? "on" : "off"), EnumChatType.Macro, "");
                                    },
                                                pvpToggleButtonBounds,
                                                "pvp-switch");
                upperComposer.GetSwitch("pvp-switch").SetValue(claims.clientDataStorage.clientPlayerInfo.CurrentPlotInfo.PermsHandler.pvpFlag);
                bgBounds.WithChildren(pvpToggleTextBounds);


                ElementBounds fireToggleTextBounds = pvpToggleTextBounds.BelowCopy(0, 15);
                fireToggleTextBounds.fixedWidth = 80;
                upperComposer.AddStaticText("Fire", CairoFont.WhiteDetailText(), fireToggleTextBounds);

                ElementBounds fireToggleButtonBounds = fireToggleTextBounds.RightCopy(0, 0);
                upperComposer.AddSwitch((t) => 
                                {
                                    ClientEventManager clientEventManager = (claims.capi.World as ClientMain).eventManager;
                                    clientEventManager.TriggerNewClientChatLine(GlobalConstants.CurrentChatGroup, "/plot set fire " + (t ? "on" : "off"), EnumChatType.Macro, "");
                                },
                                                fireToggleButtonBounds,
                                                "fire-switch");
                upperComposer.GetSwitch("fire-switch").SetValue(claims.clientDataStorage.clientPlayerInfo.CurrentPlotInfo.PermsHandler.fireFlag);
                bgBounds.WithChildren(fireToggleTextBounds);

                ElementBounds blastToggleTextBounds = fireToggleTextBounds.BelowCopy(0, 15);
                blastToggleTextBounds.fixedWidth = 80;
                upperComposer.AddStaticText("Blast", CairoFont.WhiteDetailText(), blastToggleTextBounds);

                ElementBounds blastToggleButtonBounds = blastToggleTextBounds.RightCopy(0, 0);
                upperComposer.AddSwitch((t) => 
                                 {
                                     ClientEventManager clientEventManager = (claims.capi.World as ClientMain).eventManager;
                                     clientEventManager.TriggerNewClientChatLine(GlobalConstants.CurrentChatGroup, "/plot set blast " + (!t ? "on" : "off"), EnumChatType.Macro, "");
                                 },
                                                blastToggleButtonBounds,
                                                "blast-switch");
                upperComposer.GetSwitch("blast-switch").SetValue(!claims.clientDataStorage.clientPlayerInfo.CurrentPlotInfo.PermsHandler.blastFlag);
                bgBounds.WithChildren(blastToggleButtonBounds);


                /////BUILD SWITCHES
                ElementBounds buildTextBounds = blastToggleTextBounds.BelowCopy(0, 15);
                upperComposer.AddStaticText("Build", CairoFont.WhiteDetailText(), buildTextBounds);
                bgBounds.WithChildren(buildTextBounds);

                ElementBounds friendBuildBounds = buildTextBounds.RightCopy(0, 0);
                upperComposer.AddSwitch((t) =>
                {
                    ClientEventManager clientEventManager = (claims.capi.World as ClientMain).eventManager;
                    clientEventManager.TriggerNewClientChatLine(GlobalConstants.CurrentChatGroup, "/plot set p friend build " + (t ? "on" : "off"), EnumChatType.Macro, "");
                }, friendBuildBounds, "friend-build");
                
                upperComposer.GetSwitch("friend-build").SetValue(claims.clientDataStorage.clientPlayerInfo.CurrentPlotInfo.PermsHandler.getPerm(perms.PermGroup.COMRADE, perms.type.PermType.BUILD_AND_DESTROY_PERM));
                upperComposer.AddHoverText("friend", CairoFont.WhiteDetailText(), 60, friendBuildBounds);



                ElementBounds citizenBuildBounds = friendBuildBounds.RightCopy(5, 0);
                upperComposer.AddSwitch((t) =>
                {
                    ClientEventManager clientEventManager = (claims.capi.World as ClientMain).eventManager;
                    clientEventManager.TriggerNewClientChatLine(GlobalConstants.CurrentChatGroup, "/plot set p citizen build " + (t ? "on" : "off"), EnumChatType.Macro, "");
                }, citizenBuildBounds, "citizen-build");
                upperComposer.GetSwitch("citizen-build").SetValue(claims.clientDataStorage.clientPlayerInfo.CurrentPlotInfo.PermsHandler.getPerm(perms.PermGroup.CITIZEN, perms.type.PermType.BUILD_AND_DESTROY_PERM));
                upperComposer.AddHoverText("citizen", CairoFont.WhiteDetailText(), 60, citizenBuildBounds);
                ElementBounds strangerBuildBounds = citizenBuildBounds.RightCopy(5, 0);
                upperComposer.AddSwitch((t) =>
                {
                    ClientEventManager clientEventManager = (claims.capi.World as ClientMain).eventManager;
                    clientEventManager.TriggerNewClientChatLine(GlobalConstants.CurrentChatGroup, "/plot set p stranger build " + (t ? "on" : "off"), EnumChatType.Macro, "");
                }, strangerBuildBounds, "stranger-build");
                upperComposer.GetSwitch("stranger-build").SetValue(claims.clientDataStorage.clientPlayerInfo.CurrentPlotInfo.PermsHandler.getPerm(perms.PermGroup.STRANGER, perms.type.PermType.BUILD_AND_DESTROY_PERM));
                upperComposer.AddHoverText("stranger", CairoFont.WhiteDetailText(), 60, strangerBuildBounds);

                bgBounds.WithChildren(blastToggleButtonBounds);

                ///USE SWITCHES
                ///

                ElementBounds useTextBounds = buildTextBounds.BelowCopy(0, 15);
                upperComposer.AddStaticText("Use", CairoFont.WhiteDetailText(), useTextBounds);
                bgBounds.WithChildren(useTextBounds);

                ElementBounds friendUseBounds = useTextBounds.RightCopy(0, 0);
                upperComposer.AddSwitch((t) =>
                {
                    ClientEventManager clientEventManager = (claims.capi.World as ClientMain).eventManager;
                    clientEventManager.TriggerNewClientChatLine(GlobalConstants.CurrentChatGroup, "/plot set p friend use " + (t ? "on" : "off"), EnumChatType.Macro, "");
                }, friendUseBounds, "friend-use");
                
                upperComposer.GetSwitch("friend-use").SetValue(claims.clientDataStorage.clientPlayerInfo.CurrentPlotInfo.PermsHandler.getPerm(perms.PermGroup.COMRADE, perms.type.PermType.USE_PERM));
                upperComposer.AddHoverText("friend", CairoFont.WhiteDetailText(), 60, friendUseBounds);



                ElementBounds citizenUseBounds = friendUseBounds.RightCopy(5, 0);
                upperComposer.AddSwitch((t) =>
                {
                    ClientEventManager clientEventManager = (claims.capi.World as ClientMain).eventManager;
                    clientEventManager.TriggerNewClientChatLine(GlobalConstants.CurrentChatGroup, "/plot set p citizen use " + (t ? "on" : "off"), EnumChatType.Macro, "");
                }, citizenUseBounds, "citizen-use");
                upperComposer.GetSwitch("citizen-use").SetValue(claims.clientDataStorage.clientPlayerInfo.CurrentPlotInfo.PermsHandler.getPerm(perms.PermGroup.CITIZEN, perms.type.PermType.USE_PERM));
                upperComposer.AddHoverText("citizen", CairoFont.WhiteDetailText(), 60, citizenUseBounds);
                ElementBounds strangerUseBounds = citizenUseBounds.RightCopy(5, 0);
                upperComposer.AddSwitch((t) =>
                {
                    ClientEventManager clientEventManager = (claims.capi.World as ClientMain).eventManager;
                    clientEventManager.TriggerNewClientChatLine(GlobalConstants.CurrentChatGroup, "/plot set p stranger use " + (t ? "on" : "off"), EnumChatType.Macro, "");
                }, strangerUseBounds, "stranger-use");
                upperComposer.GetSwitch("stranger-use").SetValue(claims.clientDataStorage.clientPlayerInfo.CurrentPlotInfo.PermsHandler.getPerm(perms.PermGroup.STRANGER, perms.type.PermType.USE_PERM));
                upperComposer.AddHoverText("stranger", CairoFont.WhiteDetailText(), 60, strangerUseBounds);

                ///ATTACK ANIMALS SWITCHES
                ///
                ElementBounds attackAnimalsTextBounds = useTextBounds.BelowCopy(0, 15);
                upperComposer.AddStaticText("Attack animals", CairoFont.WhiteDetailText(), attackAnimalsTextBounds);
                bgBounds.WithChildren(attackAnimalsTextBounds);

                ElementBounds friendAttackBounds = attackAnimalsTextBounds.RightCopy(0, 0);
                upperComposer.AddSwitch((t) =>
                {
                    ClientEventManager clientEventManager = (claims.capi.World as ClientMain).eventManager;
                    clientEventManager.TriggerNewClientChatLine(GlobalConstants.CurrentChatGroup, "/plot set p friend attack " + (t ? "on" : "off"), EnumChatType.Macro, "");
                }, friendAttackBounds, "friend-attack");

                upperComposer.GetSwitch("friend-attack").SetValue(claims.clientDataStorage.clientPlayerInfo.CurrentPlotInfo.PermsHandler.getPerm(perms.PermGroup.COMRADE, perms.type.PermType.ATTACK_ANIMALS_PERM));
                upperComposer.AddHoverText("friend", CairoFont.WhiteDetailText(), 60, friendAttackBounds);



                ElementBounds citizenAttackBounds = friendAttackBounds.RightCopy(5, 0);
                upperComposer.AddSwitch((t) =>
                {
                    ClientEventManager clientEventManager = (claims.capi.World as ClientMain).eventManager;
                    clientEventManager.TriggerNewClientChatLine(GlobalConstants.CurrentChatGroup, "/plot set p citizen attack " + (t ? "on" : "off"), EnumChatType.Macro, "");
                }, citizenAttackBounds, "citizen-attack");
                upperComposer.GetSwitch("citizen-attack").SetValue(claims.clientDataStorage.clientPlayerInfo.CurrentPlotInfo.PermsHandler.getPerm(perms.PermGroup.CITIZEN, perms.type.PermType.ATTACK_ANIMALS_PERM));
                upperComposer.AddHoverText("citizen", CairoFont.WhiteDetailText(), 60, citizenAttackBounds);
                ElementBounds strangerAttackBounds = citizenAttackBounds.RightCopy(5, 0);
                upperComposer.AddSwitch((t) =>
                {
                    ClientEventManager clientEventManager = (claims.capi.World as ClientMain).eventManager;
                    clientEventManager.TriggerNewClientChatLine(GlobalConstants.CurrentChatGroup, "/plot set p stranger attack " + (t ? "on" : "off"), EnumChatType.Macro, "");
                }, strangerAttackBounds, "stranger-attack");
                upperComposer.GetSwitch("stranger-attack").SetValue(claims.clientDataStorage.clientPlayerInfo.CurrentPlotInfo.PermsHandler.getPerm(perms.PermGroup.STRANGER, perms.type.PermType.ATTACK_ANIMALS_PERM));
                upperComposer.AddHoverText("stranger", CairoFont.WhiteDetailText(), 60, strangerAttackBounds);
            }
            else if (CreateNewCityState == EnumUpperWindowSelectedState.ADD_FRIEND_NEED_NAME)
            {
                upperComposer.AddStaticText("Enter player's name:",
               CairoFont.WhiteDetailText(),
               el);
                ElementBounds inputNameBounds = el.BelowCopy(0, 15);
                bgBounds.WithChildren(inputNameBounds);
                upperComposer.AddTextInput(inputNameBounds,
                    (name) => collectedNewCityName = name, null, "collectedFriendName");

                ElementBounds enterNameBounds = inputNameBounds.BelowCopy(0, 15);
                bgBounds.WithChildren(enterNameBounds);

                upperComposer.AddButton("Add", new ActionConsumable(() =>
                {
                    ClientEventManager clientEventManager = (claims.capi.World as ClientMain).eventManager;
                    clientEventManager.TriggerNewClientChatLine(GlobalConstants.CurrentChatGroup, "/citizen friend add " + collectedNewCityName, EnumChatType.Macro, "");
                    upperComposer.GetTextInput("collectedFriendName").SetValue("");
                    collectedNewCityName = "";
                    CreateNewCityState = EnumUpperWindowSelectedState.NONE;
                    BuildUpperWindow();
                    return true;
                }), enterNameBounds, EnumButtonStyle.Normal);
            }
            else if (CreateNewCityState == EnumUpperWindowSelectedState.REMOVE_FRIEND)
            {
                upperComposer.AddStaticText("Enter player's name:",
                                                               CairoFont.WhiteDetailText(),
                                                               el);
                ElementBounds inputNameBounds = el.BelowCopy(0, 15);
                bgBounds.WithChildren(inputNameBounds);

                upperComposer.AddDropDown(claims.clientDataStorage.clientPlayerInfo.Friends.ToArray(),
                                                            claims.clientDataStorage.clientPlayerInfo.Friends.ToArray(),
                                                            -1,
                                                            OnSelectedNameFromDropDown,
                                                            inputNameBounds);

                ElementBounds enterNameBounds = inputNameBounds.BelowCopy(0, 15);
                bgBounds.WithChildren(enterNameBounds);

                upperComposer.AddButton("Remove", new ActionConsumable(() =>
                {
                    ClientEventManager clientEventManager = (claims.capi.World as ClientMain).eventManager;
                    clientEventManager.TriggerNewClientChatLine(GlobalConstants.CurrentChatGroup, "/citizen friend remove " + collectedNewCityName, EnumChatType.Macro, "");
                    collectedNewCityName = "";
                    CreateNewCityState = EnumUpperWindowSelectedState.NONE;
                    BuildUpperWindow();
                    return true;
                }), enterNameBounds, EnumButtonStyle.Normal);
            }
            else if (CreateNewCityState == EnumUpperWindowSelectedState.PLOT_SET_PRICE_NEED_NUMBER)
            {
                upperComposer.AddStaticText("Enter plot's price:",
               CairoFont.WhiteDetailText(),
               el);
                ElementBounds inputNameBounds = el.BelowCopy(0, 15);
                bgBounds.WithChildren(inputNameBounds);
                upperComposer.AddNumberInput(inputNameBounds,
                    (name) => collectedNewCityName = name, null, "collectedFriendName");

                ElementBounds enterNameBounds = inputNameBounds.BelowCopy(0, 15);
                bgBounds.WithChildren(enterNameBounds);

                upperComposer.AddButton("Set price", new ActionConsumable(() =>
                {
                    ClientEventManager clientEventManager = (claims.capi.World as ClientMain).eventManager;
                    clientEventManager.TriggerNewClientChatLine(GlobalConstants.CurrentChatGroup, "/plot fs " + collectedNewCityName, EnumChatType.Macro, "");
                    upperComposer.GetTextInput("collectedFriendName").SetValue("");
                    collectedNewCityName = "";
                    CreateNewCityState = EnumUpperWindowSelectedState.NONE;
                    BuildUpperWindow();
                    return true;
                }), enterNameBounds, EnumButtonStyle.Normal);
            }
            else if (CreateNewCityState == EnumUpperWindowSelectedState.PLOT_SET_TAX)
            {
                upperComposer.AddStaticText("Enter plot's tax:",
              CairoFont.WhiteDetailText(),
              el);
                ElementBounds inputNameBounds = el.BelowCopy(0, 15);
                bgBounds.WithChildren(inputNameBounds);
                upperComposer.AddNumberInput(inputNameBounds,
                    (name) => collectedNewCityName = name, null, "collectedFriendName");

                ElementBounds enterNameBounds = inputNameBounds.BelowCopy(0, 15);
                bgBounds.WithChildren(enterNameBounds);

                upperComposer.AddButton("Set tax", new ActionConsumable(() =>
                {
                    ClientEventManager clientEventManager = (claims.capi.World as ClientMain).eventManager;
                    clientEventManager.TriggerNewClientChatLine(GlobalConstants.CurrentChatGroup, "/plot set fee " + collectedNewCityName, EnumChatType.Macro, "");
                    upperComposer.GetTextInput("collectedFriendName").SetValue("");
                    collectedNewCityName = "";
                    CreateNewCityState = EnumUpperWindowSelectedState.NONE;
                    BuildUpperWindow();
                    return true;
                }), enterNameBounds, EnumButtonStyle.Normal);
            }
            else if (CreateNewCityState == EnumUpperWindowSelectedState.PLOT_SET_TYPE)
            {
                upperComposer.AddStaticText("Select plot's type:",
                                                               CairoFont.WhiteDetailText(),
                                                               el);
                ElementBounds inputNameBounds = el.BelowCopy(0, 15);
                bgBounds.WithChildren(inputNameBounds);

                string[] plotNames = PlotInfo.plotAccessableForPlayersWithCode.Values.Select(ele => Lang.Get(ele)).ToArray();
                upperComposer.AddDropDown(PlotInfo.plotAccessableForPlayersWithCode.Keys.ToArray(),
                                                            plotNames,
                                                            -1,
                                                            OnSelectedNameFromDropDown,
                                                            inputNameBounds);

                ElementBounds enterNameBounds = inputNameBounds.BelowCopy(0, 15);
                bgBounds.WithChildren(enterNameBounds);

                upperComposer.AddButton("Set type", new ActionConsumable(() =>
                {
                    ClientEventManager clientEventManager = (claims.capi.World as ClientMain).eventManager;
                    clientEventManager.TriggerNewClientChatLine(GlobalConstants.CurrentChatGroup, "/plot set type " + collectedNewCityName, EnumChatType.Macro, "");
                    collectedNewCityName = "";
                    CreateNewCityState = EnumUpperWindowSelectedState.NONE;
                    BuildUpperWindow();
                    return true;
                }), enterNameBounds, EnumButtonStyle.Normal);
            }
            else if (CreateNewCityState == EnumUpperWindowSelectedState.PLOT_SET_NAME)
            {
                upperComposer.AddStaticText("Enter name for the plot:",
                  CairoFont.WhiteDetailText(),
                  el);
                ElementBounds inputNameBounds = el.BelowCopy(0, 15);
                bgBounds.WithChildren(inputNameBounds);
                upperComposer.AddTextInput(inputNameBounds,
                    (name) => collectedNewCityName = name, null, "collectedFriendName");

                ElementBounds enterNameBounds = inputNameBounds.BelowCopy(0, 15);
                bgBounds.WithChildren(enterNameBounds);

                upperComposer.AddButton("Set name", new ActionConsumable(() =>
                {
                    ClientEventManager clientEventManager = (claims.capi.World as ClientMain).eventManager;
                    clientEventManager.TriggerNewClientChatLine(GlobalConstants.CurrentChatGroup, "/plot set name " + collectedNewCityName, EnumChatType.Macro, "");
                    upperComposer.GetTextInput("collectedFriendName").SetValue("");
                    collectedNewCityName = "";
                    CreateNewCityState = EnumUpperWindowSelectedState.NONE;
                    BuildUpperWindow();
                    return true;
                }), enterNameBounds, EnumButtonStyle.Normal);
            }
            else if (CreateNewCityState == EnumUpperWindowSelectedState.PLOT_CLAIM)
            {
                upperComposer.AddStaticText("Claim current plot?",
                CairoFont.WhiteDetailText(),
                el);
                ElementBounds yesButtonBounds = el.BelowCopy(0, 15);
                yesButtonBounds.fixedWidth /= 2;
                bgBounds.WithChildren(yesButtonBounds);

                upperComposer.AddButton(Lang.Get("claims:gui-confirm-button"), new ActionConsumable(() =>
                {
                    ClientEventManager clientEventManager = (claims.capi.World as ClientMain).eventManager;
                    clientEventManager.TriggerNewClientChatLine(GlobalConstants.CurrentChatGroup, "/plot claim", EnumChatType.Macro, "");
                    CreateNewCityState = EnumUpperWindowSelectedState.NONE;
                    BuildUpperWindow();
                    return true;
                }), yesButtonBounds, EnumButtonStyle.Normal);

                ElementBounds noButtonBounds = yesButtonBounds.RightCopy(0, 0);
                upperComposer.AddButton(Lang.Get("claims:gui-decline-button"), new ActionConsumable(() =>
                {
                    CreateNewCityState = EnumUpperWindowSelectedState.NONE;
                    BuildUpperWindow();
                    return true;
                }), noButtonBounds, EnumButtonStyle.Normal);
            }
            else if (CreateNewCityState == EnumUpperWindowSelectedState.PLOT_UNCLAIM)
            {
                upperComposer.AddStaticText("Unclaim current plot?",
                CairoFont.WhiteDetailText(),
                el);
                ElementBounds yesButtonBounds = el.BelowCopy(0, 15);
                yesButtonBounds.fixedWidth /= 2;
                bgBounds.WithChildren(yesButtonBounds);

                upperComposer.AddButton(Lang.Get("claims:gui-confirm-button"), new ActionConsumable(() =>
                {
                    ClientEventManager clientEventManager = (claims.capi.World as ClientMain).eventManager;
                    clientEventManager.TriggerNewClientChatLine(GlobalConstants.CurrentChatGroup, "/plot unclaim", EnumChatType.Macro, "");
                    CreateNewCityState = EnumUpperWindowSelectedState.NONE;
                    BuildUpperWindow();
                    return true;
                }), yesButtonBounds, EnumButtonStyle.Normal);

                ElementBounds noButtonBounds = yesButtonBounds.RightCopy(0, 0);
                upperComposer.AddButton(Lang.Get("claims:gui-decline-button"), new ActionConsumable(() =>
                {
                    CreateNewCityState = EnumUpperWindowSelectedState.NONE;
                    BuildUpperWindow();
                    return true;
                }), noButtonBounds, EnumButtonStyle.Normal);
            }
            else if (CreateNewCityState == EnumUpperWindowSelectedState.LEAVE_CITY_CONFIRM)
            {
                upperComposer.AddStaticText(Lang.Get("claims:gui-leave-city-confirm-button"),
                CairoFont.WhiteDetailText(),
                el);
                ElementBounds yesButtonBounds = el.BelowCopy(0, 15);
                yesButtonBounds.fixedWidth /= 2;
                bgBounds.WithChildren(yesButtonBounds);

                upperComposer.AddButton(Lang.Get("claims:gui-confirm-button"), new ActionConsumable(() =>
                {
                    ClientEventManager clientEventManager = (claims.capi.World as ClientMain).eventManager;
                    clientEventManager.TriggerNewClientChatLine(GlobalConstants.CurrentChatGroup, "/city leave", EnumChatType.Macro, "");
                    CreateNewCityState = EnumUpperWindowSelectedState.NONE;
                    BuildUpperWindow();
                    return true;
                }), yesButtonBounds, EnumButtonStyle.Normal);

                ElementBounds noButtonBounds = yesButtonBounds.RightCopy(0, 0);
                upperComposer.AddButton(Lang.Get("claims:gui-decline-button"), new ActionConsumable(() =>
                {
                    CreateNewCityState = EnumUpperWindowSelectedState.NONE;
                    BuildUpperWindow();
                    return true;
                }), noButtonBounds, EnumButtonStyle.Normal);
            }
            else if (CreateNewCityState == EnumUpperWindowSelectedState.CITY_RANK_REMOVE_CONFIRM)
            {
                el.fixedWidth += 40;
                upperComposer.AddStaticText(Lang.Get("claims:gui-strip-rank-from-player", secondValueCollected, firstValueCollected),
                                                                            CairoFont.WhiteDetailText(),
                                                                            el);
                ElementBounds yesButtonBounds = el.BelowCopy(0, 15);
                yesButtonBounds.fixedWidth /= 2;
                bgBounds.WithChildren(yesButtonBounds);

                upperComposer.AddButton(Lang.Get("claims:gui-confirm-button"), new ActionConsumable(() =>
                {
                    ClientEventManager clientEventManager = (claims.capi.World as ClientMain).eventManager;
                    clientEventManager.TriggerNewClientChatLine(GlobalConstants.CurrentChatGroup, "/city rank remove " + firstValueCollected + " " +  secondValueCollected, EnumChatType.Macro, "");
                    CreateNewCityState = EnumUpperWindowSelectedState.NONE;
                    var cell = claims.clientDataStorage.clientPlayerInfo.CityInfo.CityRanks.FirstOrDefault(c => c.Name ==  firstValueCollected);
                    if(cell != null)
                    {
                        cell.Citizens.Remove(secondValueCollected);
                        BuildMainWindow();
                    }
                    BuildUpperWindow();
                    return true;
                }), yesButtonBounds, EnumButtonStyle.Normal);

                ElementBounds noButtonBounds = yesButtonBounds.RightCopy(0, 0);
                upperComposer.AddButton("No", new ActionConsumable(() =>
                {
                    CreateNewCityState = EnumUpperWindowSelectedState.NONE;
                    BuildUpperWindow();
                    return true;
                }), noButtonBounds, EnumButtonStyle.Normal);
            }
            else if (CreateNewCityState == EnumUpperWindowSelectedState.CITY_RANK_ADD)
            {
                el.fixedWidth += 40;
                upperComposer.AddStaticText("Add rank " + firstValueCollected,
                CairoFont.WhiteDetailText(),
                el);
              

                ElementBounds inputNameBounds = el.BelowCopy(0, 15);
                bgBounds.WithChildren(inputNameBounds);

                upperComposer.AddDropDown(claims.clientDataStorage.clientPlayerInfo.CityInfo.PlayersNames.ToArray(),
                                                           claims.clientDataStorage.clientPlayerInfo.CityInfo.PlayersNames.ToArray(),
                                                           -1,
                                                           OnSelectedNameFromDropDown,
                                                           inputNameBounds);

                ElementBounds yesButtonBounds = inputNameBounds.BelowCopy(0, 15);
                yesButtonBounds.fixedWidth /= 2;
                bgBounds.WithChildren(yesButtonBounds);

                upperComposer.AddButton(Lang.Get("claims:gui-add-button"), new ActionConsumable(() =>
                {
                    ClientEventManager clientEventManager = (claims.capi.World as ClientMain).eventManager;
                    clientEventManager.TriggerNewClientChatLine(GlobalConstants.CurrentChatGroup, "/city rank add " + firstValueCollected + " " + collectedNewCityName, EnumChatType.Macro, "");
                    CreateNewCityState = EnumUpperWindowSelectedState.NONE;
                    BuildUpperWindow();
                    return true;
                }), yesButtonBounds, EnumButtonStyle.Normal);

                ElementBounds noButtonBounds = yesButtonBounds.RightCopy(0, 0);
                upperComposer.AddButton(Lang.Get("claims:gui-close-button"), new ActionConsumable(() =>
                {
                    CreateNewCityState = EnumUpperWindowSelectedState.NONE;
                    BuildUpperWindow();
                    return true;
                }), noButtonBounds, EnumButtonStyle.Normal);
            }
            else if (CreateNewCityState == EnumUpperWindowSelectedState.SELECT_NEW_CITY_NAME)
            {
                upperComposer.AddStaticText(Lang.Get("claims:gui-enter-new-city-name"),
                  CairoFont.WhiteDetailText(),
                  el);
                ElementBounds inputNameBounds = el.BelowCopy(0, 15);
                bgBounds.WithChildren(inputNameBounds);
                upperComposer.AddTextInput(inputNameBounds,
                    (name) => collectedNewCityName = name, null, "collectedFriendName");

                ElementBounds enterNameBounds = inputNameBounds.BelowCopy(0, 15);
                bgBounds.WithChildren(enterNameBounds);

                upperComposer.AddButton(Lang.Get("claims:gui-set-name-button"), new ActionConsumable(() =>
                {
                    ClientEventManager clientEventManager = (claims.capi.World as ClientMain).eventManager;
                    clientEventManager.TriggerNewClientChatLine(GlobalConstants.CurrentChatGroup, "/city set name " + collectedNewCityName, EnumChatType.Macro, "");
                    upperComposer.GetTextInput("collectedFriendName").SetValue("");
                    collectedNewCityName = "";
                    CreateNewCityState = EnumUpperWindowSelectedState.NONE;
                    BuildUpperWindow();
                    return true;
                }), enterNameBounds, EnumButtonStyle.Normal);
            }
            else if (CreateNewCityState == EnumUpperWindowSelectedState.ADD_CRIMINAL_NEED_NAME)
            {
                upperComposer.AddStaticText(Lang.Get("claims:gui-enter-player-name"),
                CairoFont.WhiteDetailText(),
                el);
                ElementBounds inputNameBounds = el.BelowCopy(0, 15);
                bgBounds.WithChildren(inputNameBounds);
                upperComposer.AddTextInput(inputNameBounds,
                    (name) => collectedNewCityName = name, null, "collectedCriminalName");

                ElementBounds enterNameBounds = inputNameBounds.BelowCopy(0, 15);
                bgBounds.WithChildren(enterNameBounds);

                upperComposer.AddButton(Lang.Get("claims:gui-add-button"), new ActionConsumable(() =>
                {
                    ClientEventManager clientEventManager = (claims.capi.World as ClientMain).eventManager;
                    clientEventManager.TriggerNewClientChatLine(GlobalConstants.CurrentChatGroup, "/city criminal add " + collectedNewCityName, EnumChatType.Macro, "");
                    upperComposer.GetTextInput("collectedCriminalName").SetValue("");
                    collectedNewCityName = "";
                    CreateNewCityState = EnumUpperWindowSelectedState.NONE;
                    BuildUpperWindow();
                    return true;
                }), enterNameBounds, EnumButtonStyle.Normal);
            }
            else if (CreateNewCityState == EnumUpperWindowSelectedState.REMOVE_CRIMINAL)
            {
                upperComposer.AddStaticText(Lang.Get("claims:gui-enter-player-name"),
                                                               CairoFont.WhiteDetailText(),
                                                               el);
                ElementBounds inputNameBounds = el.BelowCopy(0, 15);
                bgBounds.WithChildren(inputNameBounds);

                upperComposer.AddDropDown(claims.clientDataStorage.clientPlayerInfo.CityInfo.Criminals.ToArray(),
                                                            claims.clientDataStorage.clientPlayerInfo.CityInfo.Criminals.ToArray(),
                                                            -1,
                                                            OnSelectedNameFromDropDown,
                                                            inputNameBounds);

                ElementBounds enterNameBounds = inputNameBounds.BelowCopy(0, 15);
                bgBounds.WithChildren(enterNameBounds);

                upperComposer.AddButton(Lang.Get("claims:gui-remove-button"), new ActionConsumable(() =>
                {
                    ClientEventManager clientEventManager = (claims.capi.World as ClientMain).eventManager;
                    clientEventManager.TriggerNewClientChatLine(GlobalConstants.CurrentChatGroup, "/city criminal remove " + collectedNewCityName, EnumChatType.Macro, "");
                    collectedNewCityName = "";
                    CreateNewCityState = EnumUpperWindowSelectedState.NONE;
                    BuildUpperWindow();
                    return true;
                }), enterNameBounds, EnumButtonStyle.Normal);
            }
            else if (CreateNewCityState == EnumUpperWindowSelectedState.CITY_PRISON_REMOVE_CELL_CONFIRM)
            {
                el.fixedWidth += 40;
                upperComposer.AddStaticText(Lang.Get("claims:gui-prison-cell-remove-confirm", firstValueCollected),
                                                                    CairoFont.WhiteDetailText(),
                                                                    el);
                ElementBounds yesButtonBounds = el.BelowCopy(0, 15);
                yesButtonBounds.fixedWidth /= 2;
                bgBounds.WithChildren(yesButtonBounds);

                upperComposer.AddButton(Lang.Get("claims:gui-confirm-button"), new ActionConsumable(() =>
                {
                    ClientEventManager clientEventManager = (claims.capi.World as ClientMain).eventManager;
                    clientEventManager.TriggerNewClientChatLine(GlobalConstants.CurrentChatGroup,
                        string.Format("/c prison cremovecell {0} {1} {2}", this.selectedPos.X, this.selectedPos.Y, this.selectedPos.Z),
                        EnumChatType.Macro, "");
                    CreateNewCityState = EnumUpperWindowSelectedState.NONE;
                    var cell = claims.clientDataStorage.clientPlayerInfo.CityInfo.PrisonCells.FirstOrDefault(c => c.SpawnPosition == this.selectedPos);
                    if (cell != null)
                    {
                        claims.clientDataStorage.clientPlayerInfo.CityInfo.PrisonCells.Remove(cell);
                        BuildMainWindow();
                    }
                    BuildUpperWindow();
                    return true;
                }), yesButtonBounds, EnumButtonStyle.Normal);

                ElementBounds noButtonBounds = yesButtonBounds.RightCopy(0, 0);
                upperComposer.AddButton(Lang.Get("claims:gui-decline-button"), new ActionConsumable(() =>
                {
                    CreateNewCityState = EnumUpperWindowSelectedState.NONE;
                    BuildUpperWindow();
                    return true;
                }), noButtonBounds, EnumButtonStyle.Normal);
            }
            else if(CreateNewCityState == EnumUpperWindowSelectedState.ADD_PLOTSGROUP_MEMBER_NEED_NAME)
            {
                upperComposer.AddStaticText(Lang.Get("claims:gui-enter-player-name"),
                CairoFont.WhiteDetailText(),
                el);
                ElementBounds inputNameBounds = el.BelowCopy(0, 15);
                bgBounds.WithChildren(inputNameBounds);
                upperComposer.AddTextInput(inputNameBounds,
                    (name) => collectedNewCityName = name, null, "collectedPlayerName");

                ElementBounds enterNameBounds = inputNameBounds.BelowCopy(0, 15);
                bgBounds.WithChildren(enterNameBounds);
                PlotsGroupCellElement cell = claims.clientDataStorage.clientPlayerInfo.CityInfo.PlotsGroupCells.FirstOrDefault(gr => gr.Guid.Equals(this.selectedString), null);
                if(cell != null)
                {
                    upperComposer.AddButton(Lang.Get("claims:gui-invite-button"), new ActionConsumable(() =>
                    {
                        ClientEventManager clientEventManager = (claims.capi.World as ClientMain).eventManager;
                        clientEventManager.TriggerNewClientChatLine(GlobalConstants.CurrentChatGroup,
                            string.Format("/c plotsgroup add {0} {1}", cell.Name, collectedNewCityName), EnumChatType.Macro, "");
                        upperComposer.GetTextInput("collectedPlayerName").SetValue("");
                        collectedNewCityName = "";
                        CreateNewCityState = EnumUpperWindowSelectedState.NONE;
                        BuildUpperWindow();
                        return true;
                    }), enterNameBounds, EnumButtonStyle.Normal);
                }
            }
            else if(CreateNewCityState == EnumUpperWindowSelectedState.REMOVE_PLOTSGROUP_MEMBER_SELECT)
            {
                el.fixedWidth += 40;
                PlotsGroupCellElement cell = claims.clientDataStorage.clientPlayerInfo.CityInfo.PlotsGroupCells.FirstOrDefault(gr => gr.Guid.Equals(this.selectedString), null);
                if (cell != null)
                {
                    upperComposer.AddStaticText(
                        string.Format("Kick from plots group {0}", cell.Name),
                    CairoFont.WhiteDetailText(),
                    el);

                    ElementBounds inputNameBounds = el.BelowCopy(0, 15);
                    bgBounds.WithChildren(inputNameBounds);

                    upperComposer.AddDropDown(cell.PlayersNames.ToArray(),
                                                                cell.PlayersNames.ToArray(),
                                                               -1,
                                                               OnSelectedNameFromDropDown,
                                                               inputNameBounds);

                    ElementBounds yesButtonBounds = inputNameBounds.BelowCopy(0, 15);
                    yesButtonBounds.fixedWidth /= 2;
                    bgBounds.WithChildren(yesButtonBounds);

                    upperComposer.AddButton(Lang.Get("claims:gui-kick-button"), new ActionConsumable(() =>
                    {
                        CreateNewCityState = EnumUpperWindowSelectedState.REMOVE_PLOTSGROUP_MEMBER_CONFIRM;
                        BuildUpperWindow();
                        return true;
                    }), yesButtonBounds, EnumButtonStyle.Normal);

                    ElementBounds noButtonBounds = yesButtonBounds.RightCopy(0, 0);
                    upperComposer.AddButton(Lang.Get("claims:gui-close-button"), new ActionConsumable(() =>
                    {
                        CreateNewCityState = EnumUpperWindowSelectedState.NONE;
                        BuildUpperWindow();
                        return true;
                    }), noButtonBounds, EnumButtonStyle.Normal);
                }
            }
            else if(CreateNewCityState == EnumUpperWindowSelectedState.REMOVE_PLOTSGROUP_MEMBER_CONFIRM)
            {
                el.fixedWidth += 40;
                var cell = claims.clientDataStorage.clientPlayerInfo.CityInfo.PlotsGroupCells.FirstOrDefault(gr => gr.Guid.Equals(this.selectedString), null);
                if (cell != null)
                {
                    upperComposer.AddStaticText(Lang.Get("claims:gui-kick-player-from-plotsgroup", cell.Name, this.collectedNewCityName),
                                                                                CairoFont.WhiteDetailText(),
                                                                                el);
                    ElementBounds yesButtonBounds = el.BelowCopy(0, 15);
                    yesButtonBounds.fixedWidth /= 2;
                    bgBounds.WithChildren(yesButtonBounds);

                    upperComposer.AddButton(Lang.Get("claims:gui-confirm-button"), new ActionConsumable(() =>
                    {
                        ClientEventManager clientEventManager = (claims.capi.World as ClientMain).eventManager;
                        clientEventManager.TriggerNewClientChatLine(GlobalConstants.CurrentChatGroup, 
                           string.Format("/c plotsgroup kick {0} {1}", cell.Name, collectedNewCityName), EnumChatType.Macro, "");
                        CreateNewCityState = EnumUpperWindowSelectedState.NONE;
                        BuildUpperWindow();
                        return true;
                    }), yesButtonBounds, EnumButtonStyle.Normal);

                    ElementBounds noButtonBounds = yesButtonBounds.RightCopy(0, 0);
                    upperComposer.AddButton("No", new ActionConsumable(() =>
                    {
                        CreateNewCityState = EnumUpperWindowSelectedState.NONE;
                        BuildUpperWindow();
                        return true;
                    }), noButtonBounds, EnumButtonStyle.Normal);
                }
            }
            else if (CreateNewCityState == EnumUpperWindowSelectedState.CITY_PLOTSGROUP_PERMISSIONS)
            {
                upperComposer.AddStaticText("Plot group permissions",
                CairoFont.WhiteDetailText(),
                el);
                ElementBounds yesButtonBounds = el.BelowCopy(0, 15);
                yesButtonBounds.fixedWidth /= 2;
                bgBounds.WithChildren(yesButtonBounds);

                var cell = claims.clientDataStorage.clientPlayerInfo.CityInfo.PlotsGroupCells.FirstOrDefault(gr => gr.Guid.Equals(this.selectedString), null);

                ElementBounds pvpToggleTextBounds = el.BelowCopy(0, 15);
                pvpToggleTextBounds.fixedWidth = 80;
                upperComposer.AddStaticText("PVP", CairoFont.WhiteDetailText(), pvpToggleTextBounds);

                ElementBounds pvpToggleButtonBounds = pvpToggleTextBounds.RightCopy(0, 0);
                upperComposer.AddSwitch((t) =>
                {
                    ClientEventManager clientEventManager = (claims.capi.World as ClientMain).eventManager;
                    clientEventManager.TriggerNewClientChatLine(GlobalConstants.CurrentChatGroup,
                        string.Format("/c plotsgroup set pvp {0} {1}", cell?.Name, (t ? "on" : "off")), EnumChatType.Macro, "");
                    cell.PermsHandler.setPvp(t);
                },
                                                pvpToggleButtonBounds,
                                                "pvp-switch");
                upperComposer.GetSwitch("pvp-switch").SetValue(cell.PermsHandler.pvpFlag);
                bgBounds.WithChildren(pvpToggleTextBounds);


                ElementBounds fireToggleTextBounds = pvpToggleTextBounds.BelowCopy(0, 15);
                fireToggleTextBounds.fixedWidth = 80;
                upperComposer.AddStaticText("Fire", CairoFont.WhiteDetailText(), fireToggleTextBounds);

                ElementBounds fireToggleButtonBounds = fireToggleTextBounds.RightCopy(0, 0);
                upperComposer.AddSwitch((t) =>
                {
                    ClientEventManager clientEventManager = (claims.capi.World as ClientMain).eventManager;
                    clientEventManager.TriggerNewClientChatLine(GlobalConstants.CurrentChatGroup,
                       string.Format("/c plotsgroup set fire {0} {1}", cell?.Name, (t ? "on" : "off")), EnumChatType.Macro, "");
                    cell.PermsHandler.setFire(t);
                },
                                                fireToggleButtonBounds,
                                                "fire-switch");
                upperComposer.GetSwitch("fire-switch").SetValue(cell.PermsHandler.fireFlag);
                bgBounds.WithChildren(fireToggleTextBounds);

                ElementBounds blastToggleTextBounds = fireToggleTextBounds.BelowCopy(0, 15);
                blastToggleTextBounds.fixedWidth = 80;
                upperComposer.AddStaticText("Blast", CairoFont.WhiteDetailText(), blastToggleTextBounds);

                ElementBounds blastToggleButtonBounds = blastToggleTextBounds.RightCopy(0, 0);
                upperComposer.AddSwitch((t) =>
                {
                    ClientEventManager clientEventManager = (claims.capi.World as ClientMain).eventManager;
                    clientEventManager.TriggerNewClientChatLine(GlobalConstants.CurrentChatGroup,
                        string.Format("/c plotsgroup set blast {0} {1}", cell?.Name, (!t ? "on" : "off")), EnumChatType.Macro, "");
                    cell.PermsHandler.setBlast(!t);
                },
                                                blastToggleButtonBounds,
                                                "blast-switch");
                upperComposer.GetSwitch("blast-switch").SetValue(!cell.PermsHandler.blastFlag);
                bgBounds.WithChildren(blastToggleButtonBounds);


                /////BUILD SWITCHES
                ElementBounds buildTextBounds = blastToggleTextBounds.BelowCopy(0, 15);
                upperComposer.AddStaticText("Build", CairoFont.WhiteDetailText(), buildTextBounds);
                bgBounds.WithChildren(buildTextBounds);


                ElementBounds citizenBuildBounds = buildTextBounds.RightCopy(0, 0);
                upperComposer.AddSwitch((t) =>
                {
                    ClientEventManager clientEventManager = (claims.capi.World as ClientMain).eventManager;
                    clientEventManager.TriggerNewClientChatLine(GlobalConstants.CurrentChatGroup,
                        string.Format("/c plotsgroup set p {0} citizen build {1}", cell?.Name ?? "_", (t ? "on" : "off")), EnumChatType.Macro, "");
                    cell.PermsHandler.setPerm(perms.PermGroup.CITIZEN, perms.type.PermType.BUILD_AND_DESTROY_PERM, t);
                }, citizenBuildBounds, "citizen-build");
                upperComposer.GetSwitch("citizen-build")
                    .SetValue(cell.PermsHandler.getPerm(perms.PermGroup.CITIZEN, perms.type.PermType.BUILD_AND_DESTROY_PERM));
                upperComposer.AddHoverText("citizen", CairoFont.WhiteDetailText(), 60, citizenBuildBounds);

                bgBounds.WithChildren(blastToggleButtonBounds);

                ///USE SWITCHES
                ///

                ElementBounds useTextBounds = buildTextBounds.BelowCopy(0, 15);
                upperComposer.AddStaticText("Use", CairoFont.WhiteDetailText(), useTextBounds);
                bgBounds.WithChildren(useTextBounds);


                ElementBounds citizenUseBounds = useTextBounds.RightCopy(0, 0);
                upperComposer.AddSwitch((t) =>
                {
                    ClientEventManager clientEventManager = (claims.capi.World as ClientMain).eventManager;
                    clientEventManager.TriggerNewClientChatLine(GlobalConstants.CurrentChatGroup,
                        string.Format("/c plotsgroup set p {0} citizen use {1}", cell?.Name ?? "_", (t ? "on" : "off")), EnumChatType.Macro, "");
                    cell.PermsHandler.setPerm(perms.PermGroup.CITIZEN, perms.type.PermType.USE_PERM, t);
                }, citizenUseBounds, "citizen-use");
                upperComposer.GetSwitch("citizen-use")
                    .SetValue(cell.PermsHandler.getPerm(perms.PermGroup.CITIZEN, perms.type.PermType.USE_PERM));
                upperComposer.AddHoverText("citizen", CairoFont.WhiteDetailText(), 60, citizenUseBounds);

            }
            else if (CreateNewCityState == EnumUpperWindowSelectedState.CITY_SUMMON_NEED_NAME)
            {
                upperComposer.AddStaticText(Lang.Get("claims:gui-enter-summon-point-name"),
                               CairoFont.WhiteDetailText(),
                               el);
                ElementBounds inputNameBounds = el.BelowCopy(0, 15);
                bgBounds.WithChildren(inputNameBounds);
                upperComposer.AddTextInput(inputNameBounds,
                    (name) => collectedNewCityName = name, null, "collectedSummonPointName");

                ElementBounds enterNameBounds = inputNameBounds.BelowCopy(0, 15);
                bgBounds.WithChildren(enterNameBounds);

                upperComposer.AddButton(Lang.Get("claims:gui-set-button"), new ActionConsumable(() =>
                {
                    ClientEventManager clientEventManager = (claims.capi.World as ClientMain).eventManager;
                    clientEventManager.TriggerNewClientChatLine(GlobalConstants.CurrentChatGroup, string.Format("/city summon set cname {0} {1} {2} {3}",
                        this.selectedPos.X, this.selectedPos.Y, this.selectedPos.Z, collectedNewCityName), EnumChatType.Macro, "");
                    upperComposer.GetTextInput("collectedSummonPointName").SetValue("");
                    collectedNewCityName = "";
                    CreateNewCityState = EnumUpperWindowSelectedState.NONE;
                    BuildUpperWindow();
                    return true;
                }), enterNameBounds, EnumButtonStyle.Normal);
            }
            else if(CreateNewCityState == EnumUpperWindowSelectedState.CITY_PLOTSGROUP_PLOT_CLAIM_CONFIRM)
            {
                el.fixedWidth += 40;
                var cell = claims.clientDataStorage.clientPlayerInfo.CityInfo.PlotsGroupCells.FirstOrDefault(gr => gr.Guid.Equals(this.selectedString), null);
                if (cell != null)
                {
                    upperComposer.AddStaticText(Lang.Get("claims:gui-claim-plot-plotsgroup", cell.Name),
                                                                                CairoFont.WhiteDetailText(),
                                                                                el);
                    ElementBounds yesButtonBounds = el.BelowCopy(0, 15);
                    yesButtonBounds.fixedWidth /= 2;
                    bgBounds.WithChildren(yesButtonBounds);

                    upperComposer.AddButton(Lang.Get("claims:gui-confirm-button"), new ActionConsumable(() =>
                    {
                        ClientEventManager clientEventManager = (claims.capi.World as ClientMain).eventManager;
                        clientEventManager.TriggerNewClientChatLine(GlobalConstants.CurrentChatGroup,
                           string.Format("/c plotsgroup plotadd {0}", cell.Name), EnumChatType.Macro, "");
                        CreateNewCityState = EnumUpperWindowSelectedState.NONE;
                        BuildUpperWindow();
                        return true;
                    }), yesButtonBounds, EnumButtonStyle.Normal);

                    ElementBounds noButtonBounds = yesButtonBounds.RightCopy(0, 0);
                    upperComposer.AddButton("No", new ActionConsumable(() =>
                    {
                        CreateNewCityState = EnumUpperWindowSelectedState.NONE;
                        BuildUpperWindow();
                        return true;
                    }), noButtonBounds, EnumButtonStyle.Normal);
                }
            }
            else if(CreateNewCityState == EnumUpperWindowSelectedState.CITY_PLOTSGROUP_PLOT_UNCLAIM_CONFIRM)
            {
                el.fixedWidth += 40;
                var cell = claims.clientDataStorage.clientPlayerInfo.CityInfo.PlotsGroupCells.FirstOrDefault(gr => gr.Guid.Equals(this.selectedString), null);
                if (cell != null)
                {
                    upperComposer.AddStaticText(Lang.Get("claims:gui-unclaim-plot-plotsgroup", cell.Name),
                                                                                CairoFont.WhiteDetailText(),
                                                                                el);
                    ElementBounds yesButtonBounds = el.BelowCopy(0, 15);
                    yesButtonBounds.fixedWidth /= 2;
                    bgBounds.WithChildren(yesButtonBounds);

                    upperComposer.AddButton(Lang.Get("claims:gui-confirm-button"), new ActionConsumable(() =>
                    {
                        ClientEventManager clientEventManager = (claims.capi.World as ClientMain).eventManager;
                        clientEventManager.TriggerNewClientChatLine(GlobalConstants.CurrentChatGroup,
                           string.Format("/c plotsgroup plotremove {0}", cell.Name), EnumChatType.Macro, "");
                        CreateNewCityState = EnumUpperWindowSelectedState.NONE;
                        BuildUpperWindow();
                        return true;
                    }), yesButtonBounds, EnumButtonStyle.Normal);

                    ElementBounds noButtonBounds = yesButtonBounds.RightCopy(0, 0);
                    upperComposer.AddButton("No", new ActionConsumable(() =>
                    {
                        CreateNewCityState = EnumUpperWindowSelectedState.NONE;
                        BuildUpperWindow();
                        return true;
                    }), noButtonBounds, EnumButtonStyle.Normal);
                }
            }
            else if(CreateNewCityState == EnumUpperWindowSelectedState.CITY_PLOTSGROUP_ADD_NEW_NEED_NAME)
            {
                upperComposer.AddStaticText(Lang.Get("claims:gui-enter-plotsgroup-name"),
                               CairoFont.WhiteDetailText(),
                               el);
                ElementBounds inputNameBounds = el.BelowCopy(0, 15);
                bgBounds.WithChildren(inputNameBounds);
                upperComposer.AddTextInput(inputNameBounds,
                    (name) => collectedNewCityName = name, null, "collectedNewPlotsGroupName");

                ElementBounds enterNameBounds = inputNameBounds.BelowCopy(0, 15);
                bgBounds.WithChildren(enterNameBounds);

                upperComposer.AddButton(Lang.Get("claims:gui-add-button"), new ActionConsumable(() =>
                {
                    ClientEventManager clientEventManager = (claims.capi.World as ClientMain).eventManager;
                    clientEventManager.TriggerNewClientChatLine(GlobalConstants.CurrentChatGroup,
                        string.Format("/c plotsgroup create {0}",
                        this.collectedNewCityName), EnumChatType.Macro, "");
                    upperComposer.GetTextInput("collectedNewPlotsGroupName").SetValue("");
                    collectedNewCityName = "";
                    CreateNewCityState = EnumUpperWindowSelectedState.NONE;
                    BuildUpperWindow();
                    return true;
                }), enterNameBounds, EnumButtonStyle.Normal);
            }
            else if (CreateNewCityState == EnumUpperWindowSelectedState.CITY_PLOTSGROUP_REMOVE_SELECT)
            {
                el.fixedWidth += 40;

                upperComposer.AddStaticText(
                    string.Format("Select plots group to remove"),
                CairoFont.WhiteDetailText(),
                el);

                ElementBounds inputNameBounds = el.BelowCopy(0, 15);
                bgBounds.WithChildren(inputNameBounds);

                var plotsGroupsArray = claims.clientDataStorage.clientPlayerInfo.CityInfo.PlotsGroupCells.Select(gr => gr.Name).ToArray();
                upperComposer.AddDropDown(plotsGroupsArray,
                                                            plotsGroupsArray,
                                                            -1,
                                                            OnSelectedNameFromDropDown,
                                                            inputNameBounds);

                ElementBounds yesButtonBounds = inputNameBounds.BelowCopy(0, 15);
                yesButtonBounds.fixedWidth /= 2;
                bgBounds.WithChildren(yesButtonBounds);

                upperComposer.AddButton(Lang.Get("claims:gui-remove-button"), new ActionConsumable(() =>
                {                   
                    CreateNewCityState = EnumUpperWindowSelectedState.CITY_PLOTSGROUP_REMOVE_CONFIRM;
                    BuildUpperWindow();
                    return true;
                }), yesButtonBounds, EnumButtonStyle.Normal);

                ElementBounds noButtonBounds = yesButtonBounds.RightCopy(0, 0);
                upperComposer.AddButton(Lang.Get("claims:gui-close-button"), new ActionConsumable(() =>
                {
                    CreateNewCityState = EnumUpperWindowSelectedState.NONE;
                    BuildUpperWindow();
                    return true;
                }), noButtonBounds, EnumButtonStyle.Normal);                
            }
            else if (CreateNewCityState == EnumUpperWindowSelectedState.CITY_PLOTSGROUP_REMOVE_CONFIRM)
            {
                el.fixedWidth += 40;
                var cell = claims.clientDataStorage.clientPlayerInfo.CityInfo.PlotsGroupCells.FirstOrDefault(gr => gr.Name.Equals(this.collectedNewCityName), null);
                if (cell != null)
                {
                    upperComposer.AddStaticText(
                        string.Format("Remove group {0}", this.secondValueCollected),
                    CairoFont.WhiteDetailText(),
                    el);

                    ElementBounds yesButtonBounds = el.BelowCopy(0, 15);
                    yesButtonBounds.fixedWidth /= 2;
                    bgBounds.WithChildren(yesButtonBounds);

                    upperComposer.AddButton(Lang.Get("claims:gui-confirm-button"), new ActionConsumable(() =>
                    {
                        ClientEventManager clientEventManager = (claims.capi.World as ClientMain).eventManager;
                        clientEventManager.TriggerNewClientChatLine(GlobalConstants.CurrentChatGroup,
                           string.Format("/c plotsgroup delete {0}", cell.Name), EnumChatType.Macro, "");
                        CreateNewCityState = EnumUpperWindowSelectedState.NONE;
                        BuildUpperWindow();
                        return true;
                    }), yesButtonBounds, EnumButtonStyle.Normal);

                    ElementBounds noButtonBounds = yesButtonBounds.RightCopy(0, 0);
                    upperComposer.AddButton("No", new ActionConsumable(() =>
                    {
                        CreateNewCityState = EnumUpperWindowSelectedState.NONE;
                        BuildUpperWindow();
                        return true;
                    }), noButtonBounds, EnumButtonStyle.Normal);
                }
            }
            else if (CreateNewCityState == EnumUpperWindowSelectedState.NEW_ALLIANCE_NEED_NAME)
            {
                upperComposer.AddStaticText(Lang.Get("claims:gui-enter-alliance-name"),
                               CairoFont.WhiteDetailText(),
                               el);
                ElementBounds inputNameBounds = el.BelowCopy(0, 15);
                bgBounds.WithChildren(inputNameBounds);
                upperComposer.AddTextInput(inputNameBounds,
                    (name) => collectedNewCityName = name, null, "collectedNewPlotsGroupName");

                ElementBounds enterNameBounds = inputNameBounds.BelowCopy(0, 15);
                bgBounds.WithChildren(enterNameBounds);

                upperComposer.AddButton(Lang.Get("claims:gui-create-button"), new ActionConsumable(() =>
                {
                    ClientEventManager clientEventManager = (claims.capi.World as ClientMain).eventManager;
                    clientEventManager.TriggerNewClientChatLine(GlobalConstants.CurrentChatGroup,
                        string.Format("/a create {0}",
                        this.collectedNewCityName), EnumChatType.Macro, "");
                    upperComposer.GetTextInput("collectedNewPlotsGroupName").SetValue("");
                    collectedNewCityName = "";
                    CreateNewCityState = EnumUpperWindowSelectedState.NONE;
                    BuildUpperWindow();
                    return true;
                }), enterNameBounds, EnumButtonStyle.Normal);

            }
            else if (CreateNewCityState == EnumUpperWindowSelectedState.INVITE_TO_ALLIANCE_NEED_NAME)
            {
                upperComposer.AddStaticText(Lang.Get("claims:gui-enter-alliance-name"),
                               CairoFont.WhiteDetailText(),
                               el);
                ElementBounds inputNameBounds = el.BelowCopy(0, 15);
                bgBounds.WithChildren(inputNameBounds);
                upperComposer.AddTextInput(inputNameBounds,
                    (name) => collectedNewCityName = name, null, "collectedNewPlotsGroupName");

                ElementBounds enterNameBounds = inputNameBounds.BelowCopy(0, 15);
                bgBounds.WithChildren(enterNameBounds);

                upperComposer.AddButton(Lang.Get("claims:gui-add-button"), new ActionConsumable(() =>
                {
                    ClientEventManager clientEventManager = (claims.capi.World as ClientMain).eventManager;
                    clientEventManager.TriggerNewClientChatLine(GlobalConstants.CurrentChatGroup,
                        string.Format("/a invite {0}",
                        this.collectedNewCityName), EnumChatType.Macro, "");
                    upperComposer.GetTextInput("collectedNewPlotsGroupName").SetValue("");
                    collectedNewCityName = "";
                    CreateNewCityState = EnumUpperWindowSelectedState.NONE;
                    BuildUpperWindow();
                    return true;
                }), enterNameBounds, EnumButtonStyle.Normal);

            }
            else if (CreateNewCityState == EnumUpperWindowSelectedState.KICK_FROM_ALLIANCE_NEED_NAME)
            {
                upperComposer.AddStaticText(Lang.Get("claims:gui-enter-alliance-name"),
                               CairoFont.WhiteDetailText(),
                               el);
                ElementBounds inputNameBounds = el.BelowCopy(0, 15);
                bgBounds.WithChildren(inputNameBounds);
                upperComposer.AddTextInput(inputNameBounds,
                    (name) => collectedNewCityName = name, null, "collectedNewPlotsGroupName");

                ElementBounds enterNameBounds = inputNameBounds.BelowCopy(0, 15);
                bgBounds.WithChildren(enterNameBounds);

                upperComposer.AddButton(Lang.Get("claims:gui-remove-button"), new ActionConsumable(() =>
                {
                    ClientEventManager clientEventManager = (claims.capi.World as ClientMain).eventManager;
                    clientEventManager.TriggerNewClientChatLine(GlobalConstants.CurrentChatGroup,
                        string.Format("/a kick {0}",
                        this.collectedNewCityName), EnumChatType.Macro, "");
                    upperComposer.GetTextInput("collectedNewPlotsGroupName").SetValue("");
                    collectedNewCityName = "";
                    CreateNewCityState = EnumUpperWindowSelectedState.NONE;
                    BuildUpperWindow();
                    return true;
                }), enterNameBounds, EnumButtonStyle.Normal);

            }
            else if (CreateNewCityState == EnumUpperWindowSelectedState.SELECT_NEW_ALLIANCE_NAME)
            {
                upperComposer.AddStaticText(Lang.Get("claims:gui-enter-alliance-name"),
                               CairoFont.WhiteDetailText(),
                               el);
                ElementBounds inputNameBounds = el.BelowCopy(0, 15);
                bgBounds.WithChildren(inputNameBounds);
                upperComposer.AddTextInput(inputNameBounds,
                    (name) => collectedNewCityName = name, null, "collectedNewPlotsGroupName");

                ElementBounds enterNameBounds = inputNameBounds.BelowCopy(0, 15);
                bgBounds.WithChildren(enterNameBounds);

                upperComposer.AddButton(Lang.Get("claims:gui-set-button"), new ActionConsumable(() =>
                {
                    ClientEventManager clientEventManager = (claims.capi.World as ClientMain).eventManager;
                    clientEventManager.TriggerNewClientChatLine(GlobalConstants.CurrentChatGroup,
                        string.Format("/a set name {0}",
                        this.collectedNewCityName), EnumChatType.Macro, "");
                    upperComposer.GetTextInput("collectedNewPlotsGroupName").SetValue("");
                    collectedNewCityName = "";
                    CreateNewCityState = EnumUpperWindowSelectedState.NONE;
                    BuildUpperWindow();
                    return true;
                }), enterNameBounds, EnumButtonStyle.Normal);

            }
            else if (CreateNewCityState == EnumUpperWindowSelectedState.ALLIANCE_PREFIX_NEED_NAME)
            {
                upperComposer.AddStaticText(Lang.Get("claims:gui-enter-alliance-prefix"),
                               CairoFont.WhiteDetailText(),
                               el);
                ElementBounds inputNameBounds = el.BelowCopy(0, 15);
                bgBounds.WithChildren(inputNameBounds);
                upperComposer.AddTextInput(inputNameBounds,
                    (name) => collectedNewCityName = name, null, "collectedNewPlotsGroupName");

                ElementBounds enterNameBounds = inputNameBounds.BelowCopy(0, 15);
                bgBounds.WithChildren(enterNameBounds);

                upperComposer.AddButton(Lang.Get("claims:gui-set-button"), new ActionConsumable(() =>
                {
                    ClientEventManager clientEventManager = (claims.capi.World as ClientMain).eventManager;
                    clientEventManager.TriggerNewClientChatLine(GlobalConstants.CurrentChatGroup,
                        string.Format("/a set prefix {0}",
                        this.collectedNewCityName), EnumChatType.Macro, "");
                    upperComposer.GetTextInput("collectedNewPlotsGroupName").SetValue("");
                    collectedNewCityName = "";
                    CreateNewCityState = EnumUpperWindowSelectedState.NONE;
                    BuildUpperWindow();
                    return true;
                }), enterNameBounds, EnumButtonStyle.Normal);

            }
            else if (CreateNewCityState == EnumUpperWindowSelectedState.LEAVE_ALLIANCE_CONFIRM)
            {
                upperComposer.AddStaticText(Lang.Get("claims:gui-leave-alliance-confirm-button"),
                CairoFont.WhiteDetailText(),
                el);
                ElementBounds yesButtonBounds = el.BelowCopy(0, 15);
                yesButtonBounds.fixedWidth /= 2;
                bgBounds.WithChildren(yesButtonBounds);


                upperComposer.AddButton(Lang.Get("claims:gui-confirm-button"), new ActionConsumable(() =>
                {
                    ClientEventManager clientEventManager = (claims.capi.World as ClientMain).eventManager;
                    clientEventManager.TriggerNewClientChatLine(GlobalConstants.CurrentChatGroup, "/alliance leave", EnumChatType.Macro, "");
                    CreateNewCityState = EnumUpperWindowSelectedState.NONE;
                    BuildUpperWindow();
                    return true;
                }), yesButtonBounds, EnumButtonStyle.Normal);

                ElementBounds noButtonBounds = yesButtonBounds.RightCopy(0, 0);
                upperComposer.AddButton(Lang.Get("claims:gui-decline-button"), new ActionConsumable(() =>
                {
                    CreateNewCityState = EnumUpperWindowSelectedState.NONE;
                    BuildUpperWindow();
                    return true;
                }), noButtonBounds, EnumButtonStyle.Normal);
            }
            else if (CreateNewCityState == EnumUpperWindowSelectedState.ALLIANCE_SEND_NEW_CONFLICT_LETTER_NEED_NAME)
            {
                upperComposer.AddStaticText(Lang.Get("claims:name_of_alliance_to_send_conflict_letter"),
                CairoFont.WhiteDetailText(),
                el);

                ElementBounds inputNameBounds = el.BelowCopy(0, 15);
                bgBounds.WithChildren(inputNameBounds);
                upperComposer.AddTextInput(inputNameBounds,
                    (name) => collectedNewCityName = name, null, "collectedNewPlotsGroupName");

                ElementBounds enterNameBounds = inputNameBounds.BelowCopy(0, 15);
                bgBounds.WithChildren(enterNameBounds);

                upperComposer.AddButton(Lang.Get("claims:gui-confirm-button"), new ActionConsumable(() =>
                {
                    ClientEventManager clientEventManager = (claims.capi.World as ClientMain).eventManager;
                    clientEventManager.TriggerNewClientChatLine(GlobalConstants.CurrentChatGroup,
                        string.Format("/a conflict declare {0}",
                        this.collectedNewCityName), EnumChatType.Macro, "");
                    upperComposer.GetTextInput("collectedNewPlotsGroupName").SetValue("");
                    collectedNewCityName = "";
                    CreateNewCityState = EnumUpperWindowSelectedState.NONE;
                    BuildUpperWindow();
                    return true;
                }), enterNameBounds, EnumButtonStyle.Normal);
            }
            else if (CreateNewCityState == EnumUpperWindowSelectedState.ALLIANCE_SEND_PEACE_OFFER_CONFIRM)
            {
                var cell = claims.clientDataStorage.clientPlayerInfo.CityInfo.ClientConflictCellElements.FirstOrDefault(c => c.Guid == this.selectedString);
                if (cell != null)
                {
                    upperComposer.AddStaticText(Lang.Get("claims:gui_send_peace_offer", this.secondSelectedString),
                    CairoFont.WhiteDetailText(),
                    el);
                    ElementBounds yesButtonBounds = el.BelowCopy(0, 15);
                    yesButtonBounds.fixedWidth /= 2;
                    bgBounds.WithChildren(yesButtonBounds);


                    upperComposer.AddButton(Lang.Get("claims:gui-confirm-button"), new ActionConsumable(() =>
                    {
                        ClientEventManager clientEventManager = (claims.capi.World as ClientMain).eventManager;
                        clientEventManager.TriggerNewClientChatLine(GlobalConstants.CurrentChatGroup, "/alliance conflict offerstop " + this.secondSelectedString, EnumChatType.Macro, "");
                        CreateNewCityState = EnumUpperWindowSelectedState.NONE;
                        BuildUpperWindow();
                        return true;
                    }), yesButtonBounds, EnumButtonStyle.Normal);

                    ElementBounds noButtonBounds = yesButtonBounds.RightCopy(0, 0);
                    upperComposer.AddButton(Lang.Get("claims:gui-decline-button"), new ActionConsumable(() =>
                    {
                        CreateNewCityState = EnumUpperWindowSelectedState.NONE;
                        BuildUpperWindow();
                        return true;
                    }), noButtonBounds, EnumButtonStyle.Normal);
                }
            }
            else if (CreateNewCityState == EnumUpperWindowSelectedState.CITY_PLOTS_PERMISSIONS)
            {
                upperComposer.AddStaticText("City permissions",
                 CairoFont.WhiteDetailText(),
                 el);
                ElementBounds yesButtonBounds = el.BelowCopy(0, 15);
                yesButtonBounds.fixedWidth /= 2;
                bgBounds.WithChildren(yesButtonBounds);


                ElementBounds pvpToggleTextBounds = el.BelowCopy(0, 15);
                pvpToggleTextBounds.fixedWidth = 80;
                upperComposer.AddStaticText("PVP", CairoFont.WhiteDetailText(), pvpToggleTextBounds);

                ElementBounds pvpToggleButtonBounds = pvpToggleTextBounds.RightCopy(0, 0);
                upperComposer.AddSwitch((t) =>
                {
                    ClientEventManager clientEventManager = (claims.capi.World as ClientMain).eventManager;
                    clientEventManager.TriggerNewClientChatLine(GlobalConstants.CurrentChatGroup, "/city set pvp " + (t ? "on" : "off"), EnumChatType.Macro, "");
                },
                                                pvpToggleButtonBounds,
                                                "pvp-switch");
                upperComposer.GetSwitch("pvp-switch").SetValue(claims.clientDataStorage.clientPlayerInfo.CityInfo.PermsHandler.pvpFlag);
                bgBounds.WithChildren(pvpToggleTextBounds);


                ElementBounds fireToggleTextBounds = pvpToggleTextBounds.BelowCopy(0, 15);
                fireToggleTextBounds.fixedWidth = 80;
                upperComposer.AddStaticText("Fire", CairoFont.WhiteDetailText(), fireToggleTextBounds);

                ElementBounds fireToggleButtonBounds = fireToggleTextBounds.RightCopy(0, 0);
                upperComposer.AddSwitch((t) =>
                {
                    ClientEventManager clientEventManager = (claims.capi.World as ClientMain).eventManager;
                    clientEventManager.TriggerNewClientChatLine(GlobalConstants.CurrentChatGroup, "/city set fire " + (t ? "on" : "off"), EnumChatType.Macro, "");
                },
                                                fireToggleButtonBounds,
                                                "fire-switch");
                upperComposer.GetSwitch("fire-switch").SetValue(claims.clientDataStorage.clientPlayerInfo.CityInfo.PermsHandler.fireFlag);
                bgBounds.WithChildren(fireToggleTextBounds);

                ElementBounds blastToggleTextBounds = fireToggleTextBounds.BelowCopy(0, 15);
                blastToggleTextBounds.fixedWidth = 80;
                upperComposer.AddStaticText("Blast", CairoFont.WhiteDetailText(), blastToggleTextBounds);

                ElementBounds blastToggleButtonBounds = blastToggleTextBounds.RightCopy(0, 0);
                upperComposer.AddSwitch((t) =>
                {
                    ClientEventManager clientEventManager = (claims.capi.World as ClientMain).eventManager;
                    clientEventManager.TriggerNewClientChatLine(GlobalConstants.CurrentChatGroup, "/city set blast " + (!t ? "on" : "off"), EnumChatType.Macro, "");
                },
                                                blastToggleButtonBounds,
                                                "blast-switch");
                upperComposer.GetSwitch("blast-switch").SetValue(!claims.clientDataStorage.clientPlayerInfo.CityInfo.PermsHandler.blastFlag);
                bgBounds.WithChildren(blastToggleButtonBounds);


                /////BUILD SWITCHES
                ElementBounds buildTextBounds = blastToggleTextBounds.BelowCopy(0, 15);
                upperComposer.AddStaticText("Build", CairoFont.WhiteDetailText(), buildTextBounds);
                bgBounds.WithChildren(buildTextBounds);

                ElementBounds friendBuildBounds = buildTextBounds.RightCopy(0, 0);
                upperComposer.AddSwitch((t) =>
                {
                    ClientEventManager clientEventManager = (claims.capi.World as ClientMain).eventManager;
                    clientEventManager.TriggerNewClientChatLine(GlobalConstants.CurrentChatGroup, "/city set p friend build " + (t ? "on" : "off"), EnumChatType.Macro, "");
                }, friendBuildBounds, "friend-build");

                upperComposer.GetSwitch("friend-build").SetValue(claims.clientDataStorage.clientPlayerInfo.CityInfo.PermsHandler.getPerm(perms.PermGroup.COMRADE, perms.type.PermType.BUILD_AND_DESTROY_PERM));
                upperComposer.AddHoverText("friend", CairoFont.WhiteDetailText(), 60, friendBuildBounds);



                ElementBounds citizenBuildBounds = friendBuildBounds.RightCopy(5, 0);
                upperComposer.AddSwitch((t) =>
                {
                    ClientEventManager clientEventManager = (claims.capi.World as ClientMain).eventManager;
                    clientEventManager.TriggerNewClientChatLine(GlobalConstants.CurrentChatGroup, "/city set p citizen build " + (t ? "on" : "off"), EnumChatType.Macro, "");
                }, citizenBuildBounds, "citizen-build");
                upperComposer.GetSwitch("citizen-build").SetValue(claims.clientDataStorage.clientPlayerInfo.CityInfo.PermsHandler.getPerm(perms.PermGroup.CITIZEN, perms.type.PermType.BUILD_AND_DESTROY_PERM));
                upperComposer.AddHoverText("citizen", CairoFont.WhiteDetailText(), 60, citizenBuildBounds);
                ElementBounds strangerBuildBounds = citizenBuildBounds.RightCopy(5, 0);
                upperComposer.AddSwitch((t) =>
                {
                    ClientEventManager clientEventManager = (claims.capi.World as ClientMain).eventManager;
                    clientEventManager.TriggerNewClientChatLine(GlobalConstants.CurrentChatGroup, "/city set p stranger build " + (t ? "on" : "off"), EnumChatType.Macro, "");
                }, strangerBuildBounds, "stranger-build");
                upperComposer.GetSwitch("stranger-build").SetValue(claims.clientDataStorage.clientPlayerInfo.CityInfo.PermsHandler.getPerm(perms.PermGroup.STRANGER, perms.type.PermType.BUILD_AND_DESTROY_PERM));
                upperComposer.AddHoverText("stranger", CairoFont.WhiteDetailText(), 60, strangerBuildBounds);

                bgBounds.WithChildren(blastToggleButtonBounds);

                ///USE SWITCHES
                ///

                ElementBounds useTextBounds = buildTextBounds.BelowCopy(0, 15);
                upperComposer.AddStaticText("Use", CairoFont.WhiteDetailText(), useTextBounds);
                bgBounds.WithChildren(useTextBounds);

                ElementBounds friendUseBounds = useTextBounds.RightCopy(0, 0);
                upperComposer.AddSwitch((t) =>
                {
                    ClientEventManager clientEventManager = (claims.capi.World as ClientMain).eventManager;
                    clientEventManager.TriggerNewClientChatLine(GlobalConstants.CurrentChatGroup, "/city set p friend use " + (t ? "on" : "off"), EnumChatType.Macro, "");
                }, friendUseBounds, "friend-use");

                upperComposer.GetSwitch("friend-use").SetValue(claims.clientDataStorage.clientPlayerInfo.CityInfo.PermsHandler.getPerm(perms.PermGroup.COMRADE, perms.type.PermType.USE_PERM));
                upperComposer.AddHoverText("friend", CairoFont.WhiteDetailText(), 60, friendUseBounds);



                ElementBounds citizenUseBounds = friendUseBounds.RightCopy(5, 0);
                upperComposer.AddSwitch((t) =>
                {
                    ClientEventManager clientEventManager = (claims.capi.World as ClientMain).eventManager;
                    clientEventManager.TriggerNewClientChatLine(GlobalConstants.CurrentChatGroup, "/city set p citizen use " + (t ? "on" : "off"), EnumChatType.Macro, "");
                }, citizenUseBounds, "citizen-use");
                upperComposer.GetSwitch("citizen-use").SetValue(claims.clientDataStorage.clientPlayerInfo.CityInfo.PermsHandler.getPerm(perms.PermGroup.CITIZEN, perms.type.PermType.USE_PERM));
                upperComposer.AddHoverText("citizen", CairoFont.WhiteDetailText(), 60, citizenUseBounds);
                ElementBounds strangerUseBounds = citizenUseBounds.RightCopy(5, 0);
                upperComposer.AddSwitch((t) =>
                {
                    ClientEventManager clientEventManager = (claims.capi.World as ClientMain).eventManager;
                    clientEventManager.TriggerNewClientChatLine(GlobalConstants.CurrentChatGroup, "/city set p stranger use " + (t ? "on" : "off"), EnumChatType.Macro, "");
                }, strangerUseBounds, "stranger-use");
                upperComposer.GetSwitch("stranger-use").SetValue(claims.clientDataStorage.clientPlayerInfo.CityInfo.PermsHandler.getPerm(perms.PermGroup.STRANGER, perms.type.PermType.USE_PERM));
                upperComposer.AddHoverText("stranger", CairoFont.WhiteDetailText(), 60, strangerUseBounds);

                ///ATTACK ANIMALS SWITCHES
                ///
                ElementBounds attackAnimalsTextBounds = useTextBounds.BelowCopy(0, 15);
                upperComposer.AddStaticText("Attack animals", CairoFont.WhiteDetailText(), attackAnimalsTextBounds);
                bgBounds.WithChildren(attackAnimalsTextBounds);

                ElementBounds friendAttackBounds = attackAnimalsTextBounds.RightCopy(0, 0);
                upperComposer.AddSwitch((t) =>
                {
                    ClientEventManager clientEventManager = (claims.capi.World as ClientMain).eventManager;
                    clientEventManager.TriggerNewClientChatLine(GlobalConstants.CurrentChatGroup, "/city set p friend attack " + (t ? "on" : "off"), EnumChatType.Macro, "");
                }, friendAttackBounds, "friend-attack");

                upperComposer.GetSwitch("friend-attack").SetValue(claims.clientDataStorage.clientPlayerInfo.CityInfo.PermsHandler.getPerm(perms.PermGroup.COMRADE, perms.type.PermType.ATTACK_ANIMALS_PERM));
                upperComposer.AddHoverText("friend", CairoFont.WhiteDetailText(), 60, friendAttackBounds);



                ElementBounds citizenAttackBounds = friendAttackBounds.RightCopy(5, 0);
                upperComposer.AddSwitch((t) =>
                {
                    ClientEventManager clientEventManager = (claims.capi.World as ClientMain).eventManager;
                    clientEventManager.TriggerNewClientChatLine(GlobalConstants.CurrentChatGroup, "/city set p citizen attack " + (t ? "on" : "off"), EnumChatType.Macro, "");
                }, citizenAttackBounds, "citizen-attack");
                upperComposer.GetSwitch("citizen-attack").SetValue(claims.clientDataStorage.clientPlayerInfo.CityInfo.PermsHandler.getPerm(perms.PermGroup.CITIZEN, perms.type.PermType.ATTACK_ANIMALS_PERM));
                upperComposer.AddHoverText("citizen", CairoFont.WhiteDetailText(), 60, citizenAttackBounds);
                ElementBounds strangerAttackBounds = citizenAttackBounds.RightCopy(5, 0);
                upperComposer.AddSwitch((t) =>
                {
                    ClientEventManager clientEventManager = (claims.capi.World as ClientMain).eventManager;
                    clientEventManager.TriggerNewClientChatLine(GlobalConstants.CurrentChatGroup, "/city set p stranger attack " + (t ? "on" : "off"), EnumChatType.Macro, "");
                }, strangerAttackBounds, "stranger-attack");
                upperComposer.GetSwitch("stranger-attack").SetValue(claims.clientDataStorage.clientPlayerInfo.CityInfo.PermsHandler.getPerm(perms.PermGroup.STRANGER, perms.type.PermType.ATTACK_ANIMALS_PERM));
                upperComposer.AddHoverText("stranger", CairoFont.WhiteDetailText(), 60, strangerAttackBounds);
            }
            else if (CreateNewCityState == EnumUpperWindowSelectedState.CITY_RANK_DELETE_CONFIRM)
            {
                el.fixedWidth += 40;
                var cell = claims.clientDataStorage.clientPlayerInfo.CityInfo.CityRanks.FirstOrDefault(gr => gr.Name.Equals(this.selectedString), null);
                if (cell != null)
                {
                    upperComposer.AddStaticText(
                        string.Format("Remove city rank {0}?", this.selectedString),
                    CairoFont.WhiteDetailText(),
                    el);

                    ElementBounds yesButtonBounds = el.BelowCopy(0, 15);
                    yesButtonBounds.fixedWidth /= 2;
                    bgBounds.WithChildren(yesButtonBounds);

                    upperComposer.AddButton(Lang.Get("claims:gui-confirm-button"), new ActionConsumable(() =>
                    {
                        ClientEventManager clientEventManager = (claims.capi.World as ClientMain).eventManager;
                        clientEventManager.TriggerNewClientChatLine(GlobalConstants.CurrentChatGroup,
                        string.Format("/c rank delete {0}",
                        this.selectedString), EnumChatType.Macro, "");
                        CreateNewCityState = EnumUpperWindowSelectedState.NONE;
                        BuildUpperWindow();
                        return true;
                    }), yesButtonBounds, EnumButtonStyle.Normal);

                    ElementBounds noButtonBounds = yesButtonBounds.RightCopy(0, 0);
                    upperComposer.AddButton("No", new ActionConsumable(() =>
                    {
                        CreateNewCityState = EnumUpperWindowSelectedState.NONE;
                        BuildUpperWindow();
                        return true;
                    }), noButtonBounds, EnumButtonStyle.Normal);
                }
            }
            else if (CreateNewCityState == EnumUpperWindowSelectedState.CITY_RANK_CREATION_NEED_NAME)
            {
                upperComposer.AddStaticText(Lang.Get("claims:gui-enter-rank-name"),
                               CairoFont.WhiteDetailText(),
                               el);
                ElementBounds inputNameBounds = el.BelowCopy(0, 15);
                bgBounds.WithChildren(inputNameBounds);
                upperComposer.AddTextInput(inputNameBounds,
                    (name) => collectedNewCityName = name, null, "collectedNewPlotsGroupName");

                ElementBounds enterNameBounds = inputNameBounds.BelowCopy(0, 15);
                bgBounds.WithChildren(enterNameBounds);

                upperComposer.AddButton(Lang.Get("claims:gui-add-button"), new ActionConsumable(() =>
                {
                    ClientEventManager clientEventManager = (claims.capi.World as ClientMain).eventManager;
                    clientEventManager.TriggerNewClientChatLine(GlobalConstants.CurrentChatGroup,
                        string.Format("/c rank create {0}",
                        this.collectedNewCityName), EnumChatType.Macro, "");
                    upperComposer.GetTextInput("collectedNewPlotsGroupName").SetValue("");
                    collectedNewCityName = "";
                    selectedString = "";
                    CreateNewCityState = EnumUpperWindowSelectedState.NONE;
                    BuildUpperWindow();
                    return true;
                }), enterNameBounds, EnumButtonStyle.Normal);

            }
            upperComposer.AddDialogTitleBar(Lang.Get(""), 
                () => 
                { 
                    CreateNewCityState = EnumUpperWindowSelectedState.NONE;
                    
                    BuildUpperWindow();
                }
            ).Compose();
        }
        private System.Collections.Generic.List<EnumSelectedTab> tabsOrder = new();
        public void OnTabToggled(int tabIndex)
        {
            if (tabIndex < 0 || tabIndex >= tabsOrder.Count) return;
            SelectedTab = tabsOrder[tabIndex];
            BuildMainWindow();
        }
        public void OnSelectedNameFromDropDown(string code, bool selected)
        {
            collectedNewCityName = code;
        }       
        public void SelectRangeAndFill()
        {
            var tabs = SingleComposer.GetHorizontalTabs("groupTabs");
            if (tabs != null)
            {

                var cell = claims.clientDataStorage.clientPlayerInfo.CityInfo.ClientConflictCellElements.FirstOrDefault(c => c.Guid == selectedString);
                if (cell == null)
                {
                    return;
                }
                if (tabs.activeElement == 0)
                {
                    ConflictPage.FillWarRangeArrays(cell.WarRanges);
                }
                else
                {
                    if (claims.clientDataStorage.clientPlayerInfo.AllianceInfo.Name.Equals(cell.FirstPartyName))
                    {
                        ConflictPage.FillTwoWarRangesArrays(cell.FirstWarRanges, cell.SecondWarRanges);
                    }
                    else
                    {
                        ConflictPage.FillTwoWarRangesArrays(cell.SecondWarRanges, cell.FirstWarRanges);
                    }
                }
                BuildMainWindow();
            }

        }
    }
}
