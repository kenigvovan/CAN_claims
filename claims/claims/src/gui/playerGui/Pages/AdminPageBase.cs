using Vintagestory.API.Client;
using Vintagestory.API.Config;

namespace claims.src.gui.playerGui.Pages
{
    /// <summary>
    /// Shared frame of the four admin pages: one entry in the main tab row, and this strip to move
    /// between them. Four separate icons up top would have pushed the row past the window edge.
    /// </summary>
    public abstract class AdminPageBase : CANGuiPage
    {
        private static readonly EnumSelectedTab[] AdminTabs =
        {
            EnumSelectedTab.AdminWorld, EnumSelectedTab.AdminCities,
            EnumSelectedTab.AdminWar, EnumSelectedTab.AdminPlayer
        };

        private static readonly string[] AdminTabLangKeys =
        {
            "claims:gui-admin-tab-world", "claims:gui-admin-tab-cities",
            "claims:gui-admin-tab-war", "claims:gui-admin-tab-player"
        };

        protected sealed override void BuildContent(PageBuildContext ctx)
        {
            var compo = ctx.Compo;

            GuiTab[] tabs = new GuiTab[AdminTabs.Length];
            for (int i = 0; i < AdminTabs.Length; i++)
            {
                tabs[i] = new GuiTab { Name = Lang.Get(AdminTabLangKeys[i]), DataInt = i };
            }

            // ctx.Current is the icon tab row itself; the strip belongs below the separator line,
            // where page content normally starts.
            var stripBounds = ctx.Current.BelowCopy(0, 50).WithAlignment(EnumDialogArea.LeftTop)
                .WithFixedSize(ctx.Line.fixedWidth, 30);

            compo.AddHorizontalTabs(tabs, stripBounds, (int value) =>
            {
                if (value < 0 || value >= AdminTabs.Length) return;
                if (AdminTabs[value] == State.SelectedTab) return;
                GoTo(AdminTabs[value]);
            }, CairoFont.WhiteSmallText(), CairoFont.WhiteSmallText(), "adminPageTabs");

            compo.GetHorizontalTabs("adminPageTabs").activeElement =
                System.Array.IndexOf(AdminTabs, State.SelectedTab);

            // Content starts below the strip; pages lay themselves out from ctx.Current as usual.
            ctx.Current = stripBounds.BelowCopy(0, 5);
            BuildAdminContent(ctx);
        }

        /// <summary>The page's own content, drawn under the shared strip.</summary>
        protected abstract void BuildAdminContent(PageBuildContext ctx);

        /// <summary>
        /// Width a Normal button needs for its caption, padding included. The 20 this used to add was
        /// less than the button's own padding, so neighbouring captions overlapped.
        /// </summary>
        protected static double ButtonWidth(string caption) =>
            CairoFont.ButtonText().GetTextExtents(caption).Width + 34;

        /// <summary>
        /// How many lines a block of text wraps into at the given width. Cards are framed before
        /// their contents are added, so a hint has to be measured rather than assumed to be one line.
        /// </summary>
        protected static double TextHeight(string text, double width, double lineHeight = 18)
        {
            double textWidth = CairoFont.WhiteSmallText().GetTextExtents(text).Width;
            int lines = (int)System.Math.Ceiling(textWidth / System.Math.Max(1, width));
            return System.Math.Max(1, lines) * lineHeight;
        }

        /// <summary>
        /// Lays buttons out left to right, sizing every one to its own caption and wrapping to a new
        /// line when the next would leave the page. Fixed widths clipped the longer translations,
        /// which is how captions ended up overlapping and running past the window edge.
        /// </summary>
        protected sealed class ButtonRow
        {
            private const double Spacing = 8;

            /// <summary>
            /// What a Normal button actually occupies. The bounds are 28 tall but the button draws
            /// its border outside them, so a card framed to 28 clipped its own buttons.
            /// </summary>
            public const double ButtonHeight = 28;
            public const double ButtonExtent = 34;

            /// <summary>Vertical step between two wrapped lines of buttons.</summary>
            public const double LineStep = ButtonExtent + 6;

            /// <summary>
            /// How tall the same set of captions will end up, so a card can be framed before its
            /// buttons are laid out. Mirrors the wrapping rule in <see cref="Add"/>.
            /// </summary>
            public static double HeightFor(double maxWidth, double startX, params string[] captions)
            {
                int lines = 1;
                double offsetX = startX;

                foreach (string caption in captions)
                {
                    double width = ButtonWidth(caption);
                    if (offsetX > startX && offsetX + width > maxWidth)
                    {
                        lines++;
                        offsetX = startX;
                    }
                    offsetX += width + Spacing;
                }

                return ButtonExtent + (lines - 1) * LineStep;
            }

            private readonly GuiComposer compo;
            private readonly double maxWidth;
            private readonly double startX;
            private ElementBounds row;
            private double offsetX;

            public ButtonRow(GuiComposer compo, ElementBounds row, double maxWidth, double startX = 0)
            {
                this.compo = compo;
                this.maxWidth = maxWidth;
                this.startX = startX;
                this.row = row.FlatCopy().WithAlignment(EnumDialogArea.LeftTop).WithFixedHeight(ButtonHeight);
                this.offsetX = startX;
            }

            /// <summary>The row the last button was placed on - the anchor for whatever comes below.</summary>
            public ElementBounds Bounds => row;

            public ElementBounds Add(string caption, System.Func<bool> onClick, string tooltip = null)
            {
                double width = ButtonWidth(caption);

                if (offsetX > startX && offsetX + width > maxWidth)
                {
                    // Same step HeightFor counts with, so a framed card fits the rows it predicted.
                    row = row.BelowCopy(0, LineStep - ButtonHeight);
                    offsetX = startX;
                }

                var target = row.FlatCopy().WithAlignment(EnumDialogArea.LeftTop).WithFixedSize(width, ButtonHeight);
                target.fixedX += offsetX;

                compo.AddButton(caption, new Vintagestory.API.Common.ActionConsumable(onClick), target, EnumButtonStyle.Normal);
                if (tooltip != null) compo.AddHoverText(tooltip, CairoFont.SmallButtonText(), 250, target);

                offsetX += width + Spacing;
                return target;
            }
        }
    }
}
