using System;
using System.Collections.Generic;
using System.Linq;
using claims.src.gui.playerGui.Dialogs;
using claims.src.gui.playerGui.Pages;
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
        // Deliberately not the registered hotkey code ("canclaimsgui"): claims.OnHotKeySkillDialog
        // already toggles this dialog by hand, and matching the code would make the engine toggle it
        // a second time on the same keypress.
        public override string ToggleKeyCombinationCode => "CANClaimsGui";
        public float Width { get; private set; }
        public float Height { get; private set; }

        /// <summary>All mutable UI state. Pages and dialogs read and write it through here.</summary>
        public ClaimsGuiState State { get; } = new ClaimsGuiState();

        /// <summary>Shorthand for the current page, used by the tab bar and the packet handlers.</summary>
        public EnumSelectedTab SelectedTab { get => State.SelectedTab; set => State.SelectedTab = value; }

        private readonly PageRegistry pages = new PageRegistry();
        private readonly DialogRegistry dialogs = new DialogRegistry();
        public ElementBounds mainBounds;

        /// <summary>Tabs of the top row, in order. Hidden tabs simply do not take up an index.</summary>
        private sealed class MainTab
        {
            public EnumSelectedTab Tab;
            public string Icon;
            public string TooltipLangKey;
            public Func<bool> Visible;
        }

        private static readonly MainTab[] MainTabs =
        {
            new MainTab { Tab = EnumSelectedTab.City,   Icon = "claims:qaitbay-citadel",  TooltipLangKey = "claims:gui-maintab-city" },
            new MainTab { Tab = EnumSelectedTab.Player, Icon = "claims:magnifying-glass", TooltipLangKey = "claims:gui-maintab-player" },
            new MainTab { Tab = EnumSelectedTab.Prices, Icon = "claims:price-tag",        TooltipLangKey = "claims:gui-maintab-prices",
                          Visible = () => !string.IsNullOrEmpty(claims.config?.SELECTED_ECONOMY_HANDLER) },
            new MainTab { Tab = EnumSelectedTab.Plot,       Icon = "claims:flat-platform", TooltipLangKey = "claims:gui-maintab-plot" },
            // Prisons, summons and plot groups are city features - a village has none of them.
            new MainTab { Tab = EnumSelectedTab.Prison,     Icon = "claims:prisoner",      TooltipLangKey = "claims:gui-maintab-prison",
                          Visible = () => !IsOwnSettlementAVillage() },
            new MainTab { Tab = EnumSelectedTab.Summon,     Icon = "claims:magic-portal",  TooltipLangKey = "claims:gui-maintab-summon",
                          Visible = () => !IsOwnSettlementAVillage() },
            new MainTab { Tab = EnumSelectedTab.PlotsGroup, Icon = "claims:huts-village",  TooltipLangKey = "claims:gui-maintab-plotsgroup",
                          Visible = () => !IsOwnSettlementAVillage() },

            // One admin entry, hidden from everyone whose role is not listed in the config. The four
            // admin pages switch between themselves with their own tab strip - four more icons here
            // would not fit the row.
            new MainTab { Tab = EnumSelectedTab.AdminWorld, Icon = "claims:id-card",
                          TooltipLangKey = "claims:gui-maintab-admin", Visible = IsAdmin },
        };

        /// <summary>The pages reachable through the single admin entry in the tab row.</summary>
        internal static bool IsAdminPage(EnumSelectedTab tab) =>
            tab == EnumSelectedTab.AdminWorld || tab == EnumSelectedTab.AdminCities
            || tab == EnumSelectedTab.AdminWar || tab == EnumSelectedTab.AdminWarConfig
            || tab == EnumSelectedTab.AdminPlayer;

        private static bool adminChecked;
        private static bool isAdmin;

        /// <summary>
        /// Whether this player may see the admin tabs. The role is only known once the world has
        /// loaded, so the answer is worked out on first use and then kept.
        /// </summary>
        /// <summary>
        /// Whether the settlement the player belongs to is a village. Not cached, unlike IsAdmin:
        /// a village can be upgraded to a city mid-session, and the tab row must follow.
        /// </summary>
        private static bool IsOwnSettlementAVillage()
        {
            return claims.clientDataStorage?.clientPlayerInfo?.CityInfo?.IsVillage == true;
        }

        private static bool IsAdmin()
        {
            if (adminChecked) return isAdmin;

            var role = claims.capi?.World?.Player?.Role;
            if (role == null) return false;

            adminChecked = true;
            isAdmin = claims.config?.ROLE_CODES_WITH_ADMIN_RIGHTS?.Contains(role.Code) == true;
            return isAdmin;
        }

        public CANClaimsGui(ICoreClientAPI capi) : base(capi)
        {
            // The prices tab fills two columns of cards; at 500x600 the lower one ended right at
            // the bottom edge.
            Width = 560;
            Height = 660;
            SelectedTab = 0;
        }
        public override void OnGuiOpened()
        {
            base.OnGuiOpened();

            if (IsAdmin())
            {
                pages.AddAdminPages();
                // Ask for the flags the admin pages display; the reply rebuilds the window.
                claims.clientChannel.SendPacket(new network.packets.SavedPlotsPacket
                {
                    type = network.packets.PacketsContentEnum.ADMIN_REQUEST_CITY_FLAGS
                });
            }

            // The rank, group or conflict the last session was looking at may be gone by now.
            // Landing on a blank tab reads as a broken dialog, so fall back to the city page.
            if (pages.TryGet(SelectedTab, out var page) && !page.CanBeShown())
            {
                SelectedTab = EnumSelectedTab.City;
            }

            BuildMainWindow();
        }
        // Which plot the server was last asked about, and whether the plot tab was showing on the
        // previous frame. int.MinValue means "never asked", so no null checks on a per-frame path.
        private int askedPlotX = int.MinValue;
        private int askedPlotZ = int.MinValue;
        private bool wasOnPlotTab;

        public override void OnRenderGUI(float deltaTime)
        {
            base.OnRenderGUI(deltaTime);
            RefreshPlotIfNeeded();
        }

        /// <summary>
        /// Keeps the plot page describing the plot underfoot. The server only pushes plot data after
        /// one's own command, so walking a plot over - or someone else editing the plot - used to
        /// leave the page stale until the refresh button was pressed.
        ///
        /// Asks on entering the tab and on crossing a plot border while it is open. The reply
        /// rebuilds the window, but neither the tab nor the position changed, so it cannot loop.
        /// </summary>
        private void RefreshPlotIfNeeded()
        {
            if (State.SelectedTab != EnumSelectedTab.Plot)
            {
                wasOnPlotTab = false;
                return;
            }

            var pos = capi.World?.Player?.Entity?.Pos;
            if (pos == null) return;

            int size = auxialiry.PlotPosition.plotSize;
            if (size <= 0) return;

            int plotX = (int)pos.X / size;
            int plotZ = (int)pos.Z / size;

            bool entered = !wasOnPlotTab;
            wasOnPlotTab = true;

            if (!entered && plotX == askedPlotX && plotZ == askedPlotZ) return;

            askedPlotX = plotX;
            askedPlotZ = plotZ;

            claims.clientChannel.SendPacket(new network.packets.SavedPlotsPacket
            {
                type = network.packets.PacketsContentEnum.CURRENT_PLOT_CLIENT_REQUEST,
                data = ""
            });
        }

        public override void OnGuiClosed()
        {
            base.OnGuiClosed();
            // Closing the dialog also drops half-typed input: reopening used to show whatever name
            // was left in the buffer from the previous session.
            State.CloseDialog();
            // So that reopening on the plot tab counts as entering it and fetches fresh data.
            wasOnPlotTab = false;
        }

        /// <summary>Closes the secondary window and rebuilds it away.</summary>
        public void CloseDialog()
        {
            State.CloseDialog();
            BuildUpperWindow();
        }

        /// <summary>Opens the secondary window on the given form, optionally filling its arguments.</summary>
        public void OpenDialog(EnumUpperWindowSelectedState dialog, Action<DialogArgs> fill = null)
        {
            State.Dialog = dialog;
            // A form opens empty. The input buffers are shared between dialogs and only cleared on
            // close, so switching straight from one form to another - typing a price into "sell to
            // cities", then pressing "put up for bids" - handed the new form the old text while its
            // own field showed nothing. Cleared before fill, which is how a caller prefills a form.
            State.DialogArgs.Text = "";
            State.DialogArgs.First = "";
            State.DialogArgs.Second = "";
            fill?.Invoke(State.DialogArgs);
            BuildUpperWindow();
        }
        public void BuildMainWindow()
        {
            // Leaving the conflict page drops the working copy of its schedule grid, so coming back
            // shows what the server holds rather than half-finished clicks from the last visit.
            if (State.SelectedTab != EnumSelectedTab.ConflictInfoPage) State.WarGridLoadedFor = "";

            int fixedY1 = 20;
            ElementBounds globalBounds = ElementStdBounds.AutosizedMainDialog.WithAlignment(EnumDialogArea.CenterMiddle);

            ElementBounds backgroundBounds = ElementBounds.Fill.WithFixedPadding(GuiStyle.ElementToDialogPadding).WithFixedSize(Width, Height);

            mainBounds = ElementBounds.FixedPos(EnumDialogArea.CenterTop, 0, 15).WithFixedSize(Width, Height);

            ElementBounds leftArrowBounds = ElementBounds.FixedPos(EnumDialogArea.LeftMiddle, 0, 0).WithFixedHeight(50).WithFixedWidth(50);

            ElementBounds rightArrowBounds = ElementBounds.FixedPos(EnumDialogArea.RightMiddle, 0, 0).WithFixedHeight(50).WithFixedWidth(50);

            ElementBounds middleBounds = ElementBounds.FixedPos(EnumDialogArea.CenterMiddle, 0, 0).WithFixedHeight(Height).WithFixedWidth(Width - 100);

            ElementBounds tabNameBounds = ElementBounds.FixedPos(EnumDialogArea.CenterFixed, 0, 0).WithFixedHeight(40).WithFixedWidth(100);

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
            var visibleTabs = MainTabs.Where(t => t.Visible == null || t.Visible()).ToList();
            var tabIcons = visibleTabs.Select(t => t.Icon).ToList();
            tabsOrder = visibleTabs.Select(t => t.Tab).ToList();

            // The gap is tight on purpose: with the admin entry shown there are eight tabs, and at
            // the old 25px gap they would run past the window edge. Wrapping stays as a safety net
            // should the row ever grow again.
            const double tabSize = 48;
            const double tabGap = 12;
            const double tabStartX = 5;

            double usableWidth = mainBounds.fixedWidth - tabStartX * 2;
            int perRow = Math.Max(1, (int)((usableWidth + tabGap) / (tabSize + tabGap)));
            int tabRows = (int)Math.Ceiling(tabIcons.Count / (double)perRow);

            var tabBounds = new System.Collections.Generic.List<ElementBounds>();
            for (int i = 0; i < tabIcons.Count; i++)
            {
                int row = i / perRow;
                int column = i % perRow;

                ElementBounds slot = currentBounds.FlatCopy().WithAlignment(EnumDialogArea.LeftTop).WithFixedSize(tabSize, tabSize);
                slot.fixedX += tabStartX + column * (tabSize + tabGap);
                slot.fixedY += row * (tabSize + 6);
                tabBounds.Add(slot);
            }

            SingleComposer.AddIconToggleButtons(tabIcons.ToArray(),
                                                CairoFont.ButtonText(),
                                                OnTabToggled,
                                                tabBounds.ToArray(),
                                                "selectedTab");

            // Icons alone say little; name each tab on hover.
            for (int i = 0; i < visibleTabs.Count; i++)
            {
                if (visibleTabs[i].TooltipLangKey == null) continue;
                Widgets.Tooltip.Add(SingleComposer, Lang.Get(visibleTabs[i].TooltipLangKey), tabBounds[i], "tabtip-" + i);
            }

            // Any of the four admin pages lights up the one admin entry.
            EnumSelectedTab highlightTab = IsAdminPage(SelectedTab) ? EnumSelectedTab.AdminWorld : SelectedTab;
            int selectedIdx = tabsOrder.IndexOf(highlightTab);
            if (selectedIdx >= 0 && SingleComposer.GetToggleButton("selectedTab-" + selectedIdx) != null)
            {
                SingleComposer.GetToggleButton("selectedTab-" + selectedIdx).SetValue(true);
            }

            // The separator drops below whatever the last tab row is.
            var lineBounds = currentBounds.BelowCopy(0, 20 + (tabRows - 1) * (tabSize + 6)).WithFixedHeight(5);
            SingleComposer.AddInset(lineBounds);

            // Composing happens here and only here. Pages queue anything that needs the final layout
            // (scrollbar heights) through ctx.AfterCompose.
            var ctx = new PageBuildContext
            {
                Compo = SingleComposer,
                Current = currentBounds,
                Line = lineBounds,
                Main = mainBounds
            };

            if (pages.TryGet(SelectedTab, out var page))
            {
                page.Build(this, ctx);
            }

            SingleComposer.Compose();
            ctx.RunAfterCompose();
            BuildUpperWindow();
        }

        /// <summary>
        /// Draws the secondary window next to the dialog. Which form it shows is a lookup, not a
        /// 1500-line if/else chain: see DialogRegistry.
        /// </summary>
        public void BuildUpperWindow()
        {
            if (!this.IsOpened()) return;

            if (State.Dialog == EnumUpperWindowSelectedState.NONE)
            {
                this.Composers.Remove(DialogFrame.ComposerKey);
                return;
            }

            if (dialogs.TryGet(State.Dialog, out var dialog))
            {
                dialog.Build(this);
            }
        }

        private System.Collections.Generic.List<EnumSelectedTab> tabsOrder = new();
        public void OnTabToggled(int tabIndex)
        {
            if (tabIndex < 0 || tabIndex >= tabsOrder.Count) return;
            SelectedTab = tabsOrder[tabIndex];
            BuildMainWindow();
        }
        public void SelectRangeAndFill()
        {
            var tabs = SingleComposer.GetHorizontalTabs("groupTabs");
            if (tabs != null)
            {

                // A packet can arrive before the player has a city, or after they left one.
                var cityInfo = claims.clientDataStorage.clientPlayerInfo?.CityInfo;
                if (cityInfo == null) return;

                var cell = cityInfo.ClientConflictCellElements.FirstOrDefault(c => c.Guid == State.DialogArgs.Selected);
                if (cell == null)
                {
                    return;
                }
                if (tabs.activeElement == 0)
                {
                    WarRangeMath.FillWarRangeArrays(cell.WarRanges);
                }
                else
                {
                    // An independent city fights under its own name; reading AllianceInfo.Name
                    // outright threw for a lone mayor, exactly as it once did on the page itself.
                    if (Pages.ConflictInfoPage.OurPartyName().Equals(cell.FirstPartyName))
                    {
                        WarRangeMath.FillTwoWarRangesArrays(cell.FirstWarRanges, cell.SecondWarRanges);
                    }
                    else
                    {
                        WarRangeMath.FillTwoWarRangesArrays(cell.SecondWarRanges, cell.FirstWarRanges);
                    }
                }
                BuildMainWindow();
            }

        }
    }
}
