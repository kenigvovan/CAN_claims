using claims.src.gui.playerGui.Dialogs;
using claims.src.gui.playerGui.Pages;
using claims.src.perms;
using Vintagestory.API.Client;
using Vintagestory.API.Config;

namespace claims.src.gui.playerGui.Widgets
{
    /// <summary>
    /// The flags and the rights of a <see cref="PermissionsScope"/> as cards on the page itself,
    /// taken over from canclaimsextended. In a side window the rules of a plot were only seen by
    /// whoever may change them; here everyone sees them, and someone without the right gets the same
    /// switches read-only. A switch sends its command at once and updates the local copy, as the
    /// side window does, rather than waiting for the server to answer.
    /// </summary>
    public static class PermTable
    {
        private const double RowHeight = 26;
        private const double SwitchSize = 22;
        private const double ColumnWidth = 60;

        /// <summary>The flags, two to a line with their labels. Returns the y the next card starts at.</summary>
        public static double Flags(PageBuildContext ctx, ElementBounds column, double y, string keyPrefix,
                                   PermissionsScope scope, bool editable)
            => Flags(ctx, column, y, keyPrefix, scope, _ => editable);

        /// <summary>
        /// The same, with a right of its own for each flag - the server checks one per flag
        /// (CITY_SET_PVP, CITY_PLOTSGROUP_SET_FIRE...), and a rank may hold some and not others.
        /// </summary>
        public static double Flags(PageBuildContext ctx, ElementBounds column, double y, string keyPrefix,
                                   PermissionsScope scope, System.Func<FlagRow, bool> mayToggle)
        {
            GuiComposer compo = ctx.Compo;
            PermsHandler handler = scope.Source();
            int lines = (scope.Flags.Count + 1) / 2;
            double height = Card.HeaderHeight + RowHeight * lines + Card.Padding * 2;
            ElementBounds inner = Card.Frame(compo, column, y, height, Lang.Get("claims:gui-section-flags"));

            double half = inner.fixedWidth / 2;
            var labelFont = CairoFont.WhiteSmallText().WithColor(ClaimsColors.Label);
            for (int i = 0; i < scope.Flags.Count; i++)
            {
                FlagRow flag = scope.Flags[i];
                double x = (i % 2) * half;
                double rowY = inner.fixedY + (i / 2) * RowHeight;

                var sw = inner.FlatCopy().WithFixedSize(SwitchSize, SwitchSize);
                sw.fixedX += x;
                sw.fixedY = rowY;
                string key = keyPrefix + "-flag-" + flag.SubCommand;
                bool editable = mayToggle(flag);
                compo.AddSwitch(on =>
                {
                    // Read-only: a click goes straight back.
                    if (!editable)
                    {
                        compo.GetSwitch(key)?.SetValue(!on);
                        return;
                    }
                    bool stored = flag.Inverted ? !on : on;
                    ClientChat.Send(scope.FlagCommand(flag, stored));
                    PermsHandler current = scope.Source();
                    if (current != null) flag.Set(current, stored);
                }, sw, key, SwitchSize);

                double labelWidth = half - SwitchSize - 8;
                var label = inner.FlatCopy().WithFixedSize(labelWidth, RowHeight);
                label.fixedX += x + SwitchSize + 6;
                label.fixedY = rowY + 4;
                string shown = Card.Fit(flag.Label, labelFont, labelWidth);
                compo.AddStaticText(shown, labelFont, label, keyPrefix + "-flaglabel-" + flag.SubCommand);
                if (shown != flag.Label) Tooltip.Add(compo, flag.Label, label, keyPrefix + "-flagtip-" + flag.SubCommand);

                if (handler != null)
                {
                    bool stored = flag.Get(handler);
                    bool value = flag.Inverted ? !stored : stored;
                    ctx.AfterCompose(() => compo.GetSwitch(key)?.SetValue(value));
                }
            }
            return y + height + Card.Gap;
        }

        /// <summary>
        /// The rights grid: a line per group, a column per right. Returns the y the next card starts at.
        /// </summary>
        public static double Rights(PageBuildContext ctx, ElementBounds column, double y, string keyPrefix,
                                    PermissionsScope scope, bool editable)
        {
            GuiComposer compo = ctx.Compo;
            PermsHandler handler = scope.Source();
            double height = Card.HeaderHeight + RowHeight * (scope.Groups.Count + 1) + Card.Padding * 2;
            ElementBounds inner = Card.Frame(compo, column, y, height, Lang.Get("claims:gui-section-rights"));

            double labelWidth = inner.fixedWidth - ColumnWidth * scope.Perms.Count;
            var headFont = CairoFont.WhiteSmallText().WithColor(ClaimsColors.Section).WithOrientation(EnumTextOrientation.Center);
            for (int p = 0; p < scope.Perms.Count; p++)
            {
                var head = inner.FlatCopy().WithFixedSize(ColumnWidth, RowHeight);
                head.fixedX += labelWidth + p * ColumnWidth;
                // "Attack animals" does not fit a switch column; cut short, whole on hover.
                string text = scope.Perms[p].Label;
                string shown = Card.Fit(text, headFont, ColumnWidth - 4);
                compo.AddStaticText(shown, headFont, head, keyPrefix + "-head-" + p);
                if (shown != text) Tooltip.Add(compo, text, head, keyPrefix + "-headtip-" + p);
            }

            var labelFont = CairoFont.WhiteSmallText().WithColor(ClaimsColors.Label);
            for (int g = 0; g < scope.Groups.Count; g++)
            {
                GroupCol group = scope.Groups[g];
                double rowY = inner.fixedY + RowHeight * (g + 1);
                var label = inner.FlatCopy().WithFixedSize(labelWidth, RowHeight);
                label.fixedY = rowY + 4;
                compo.AddStaticText(Card.Fit(group.Label, labelFont, labelWidth - 4), labelFont, label, keyPrefix + "-group-" + g);

                for (int p = 0; p < scope.Perms.Count; p++)
                {
                    PermRow perm = scope.Perms[p];
                    var sw = inner.FlatCopy().WithFixedSize(SwitchSize, SwitchSize);
                    sw.fixedX += labelWidth + p * ColumnWidth + (ColumnWidth - SwitchSize) / 2;
                    sw.fixedY = rowY;
                    string key = keyPrefix + "-" + group.CommandToken + "-" + perm.KeyPrefix;
                    compo.AddSwitch(on =>
                    {
                        if (!editable)
                        {
                            compo.GetSwitch(key)?.SetValue(!on);
                            return;
                        }
                        ClientChat.Send(scope.PermCommand(group, perm, on));
                        scope.Source()?.setPerm(group.Group, perm.Type, on);
                    }, sw, key, SwitchSize);
                    Tooltip.Add(compo, group.Label + ": " + perm.Label, sw, key + "-tip");

                    if (handler != null)
                    {
                        bool value = handler.getPerm(group.Group, perm.Type);
                        ctx.AfterCompose(() => compo.GetSwitch(key)?.SetValue(value));
                    }
                }
            }
            return y + height + Card.Gap;
        }
    }
}
