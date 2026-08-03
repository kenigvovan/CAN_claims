using claims.src.perms;
using Vintagestory.API.Client;
using Vintagestory.API.Config;

namespace claims.src.gui.playerGui.Dialogs
{
    /// <summary>
    /// The flags column plus the permission grid, driven by a <see cref="PermissionsScope"/>.
    /// Replaces three near-identical hand-written panels.
    /// </summary>
    public sealed class PermissionsDialog : CANGuiDialogPanel
    {
        private readonly string titleLangKey;
        private readonly PermissionsScope scope;

        public PermissionsDialog(string titleLangKey, PermissionsScope scope)
        {
            this.titleLangKey = titleLangKey;
            this.scope = scope;
        }

        // Four group columns (friend, citizen, ally, stranger) need more room than the default panel:
        // at 300 the last two ran off the right edge and the column heads wrapped mid-word.
        protected override double Width =>
            LabelWidth + System.Math.Max(1, scope.Groups.Count) * ColumnWidth + 20;

        /// <summary>Caption column, then one column per group - wide enough for its own heading.</summary>
        private const double LabelWidth = 130;
        private const double ColumnWidth = 68;

        /// <summary>Switch size the game draws by default; used to centre it in its column.</summary>
        private const double SwitchSize = 30;

        protected override void BuildContent(DialogLayout l)
        {
            PermsHandler handler = scope.Source();
            if (handler == null) return;

            Text(l, Lang.Get(titleLangKey));

            foreach (var flag in scope.Flags)
            {
                ElementBounds labelBounds = l.NextRow();
                labelBounds.fixedWidth = LabelWidth;
                l.Compo.AddStaticText(flag.Label, CairoFont.WhiteDetailText(), labelBounds);

                ElementBounds switchBounds = Column(l, labelBounds, 0);
                var currentFlag = flag;
                l.Compo.AddSwitch((on) =>
                {
                    bool stored = currentFlag.Inverted ? !on : on;
                    Send(scope.FlagCommand(currentFlag, stored));
                    currentFlag.Set(handler, stored);
                }, switchBounds, currentFlag.Key);

                bool shown = currentFlag.Get(handler);
                l.Compo.GetSwitch(currentFlag.Key).SetValue(currentFlag.Inverted ? !shown : shown);
            }

            // Column heads: with four groups the hover text alone is not enough to tell the
            // switches apart at a glance.
            if (scope.Groups.Count > 1)
            {
                ElementBounds headRow = l.NextRow();
                headRow.fixedWidth = LabelWidth;
                var headFont = CairoFont.WhiteSmallText().WithFontSize(13)
                                        .WithOrientation(EnumTextOrientation.Center);

                for (int i = 0; i < scope.Groups.Count; i++)
                {
                    ElementBounds head = headRow.FlatCopy().WithFixedWidth(ColumnWidth);
                    head.fixedX += LabelWidth + i * ColumnWidth;

                    // The background sizes itself to its children, so every column has to be one of
                    // them - otherwise the panel stays as wide as the caption column and the
                    // switches sit outside it.
                    l.Bg.WithChildren(head);
                    l.Compo.AddStaticText(scope.Groups[i].Label, headFont, head);
                }
            }

            foreach (var perm in scope.Perms)
            {
                ElementBounds labelBounds = l.NextRow();
                labelBounds.fixedWidth = LabelWidth;
                l.Compo.AddStaticText(perm.Label, CairoFont.WhiteDetailText(), labelBounds);

                for (int i = 0; i < scope.Groups.Count; i++)
                {
                    var group = scope.Groups[i];
                    var currentPerm = perm;

                    string key = group.CommandToken + "-" + currentPerm.KeyPrefix;
                    var switchBounds = Column(l, labelBounds, i);
                    l.Compo.AddSwitch((on) =>
                    {
                        Send(scope.PermCommand(group, currentPerm, on));
                        handler.setPerm(group.Group, currentPerm.Type, on);
                    }, switchBounds, key);

                    l.Compo.GetSwitch(key).SetValue(handler.getPerm(group.Group, currentPerm.Type));
                    l.Compo.AddHoverText(group.Label, CairoFont.WhiteDetailText(), 80, switchBounds);
                }
            }
        }

        /// <summary>
        /// The switch slot of group <paramref name="index"/>, centred under its heading and
        /// registered with the background so the panel grows to hold it.
        /// </summary>
        private static ElementBounds Column(DialogLayout l, ElementBounds labelBounds, int index)
        {
            ElementBounds slot = labelBounds.FlatCopy().WithFixedWidth(SwitchSize);
            slot.fixedX += LabelWidth + index * ColumnWidth + (ColumnWidth - SwitchSize) / 2;
            l.Bg.WithChildren(slot);
            return slot;
        }
    }
}
