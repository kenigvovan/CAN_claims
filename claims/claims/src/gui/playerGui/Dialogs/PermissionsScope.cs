using System;
using System.Collections.Generic;
using claims.src.perms;
using claims.src.perms.type;
using Vintagestory.API.Config;

namespace claims.src.gui.playerGui.Dialogs
{
    /// <summary>A world flag toggle: pvp, fire, blast.</summary>
    public sealed class FlagRow
    {
        public string Label;
        public string SubCommand;
        public Func<PermsHandler, bool> Get;
        public Action<PermsHandler, bool> Set;
        /// <summary>
        /// True when the switch shows the opposite of the stored flag. Blast does: the switch means
        /// "blast protection on", the flag means "blast allowed".
        /// </summary>
        public bool Inverted;
        /// <summary>Element key of the switch. Kept from the original so nothing else breaks.</summary>
        public string Key;
    }

    /// <summary>One row of the permission grid - build, use, attack animals.</summary>
    public sealed class PermRow
    {
        public string Label;
        public string SubCommand;
        public PermType Type;
        /// <summary>Prefix of the switch keys, e.g. "build" gives friend-build / citizen-build.</summary>
        public string KeyPrefix;
    }

    /// <summary>One column of the permission grid - friend, citizen, stranger.</summary>
    public sealed class GroupCol
    {
        public string Label;
        public string CommandToken;
        public PermGroup Group;
    }

    /// <summary>
    /// What a permissions panel acts on. The plot panel and the city panel were two 164-line copies
    /// of each other differing only in the command prefix and where the flags are read from; the
    /// plots group panel was a third, shorter copy.
    /// </summary>
    public sealed class PermissionsScope
    {
        public Func<PermsHandler> Source;
        public IReadOnlyList<FlagRow> Flags = new FlagRow[0];
        public IReadOnlyList<PermRow> Perms = new PermRow[0];
        public IReadOnlyList<GroupCol> Groups = new GroupCol[0];

        /// <summary>
        /// Builds the flag command. A delegate rather than a prefix because the plots group scope
        /// puts its target name in the middle: "/c plotsgroup set pvp &lt;name&gt; on".
        /// </summary>
        public Func<FlagRow, bool, string> FlagCommand;

        /// <summary>Builds the per-group permission command.</summary>
        public Func<GroupCol, PermRow, bool, string> PermCommand;
    }

    public static class PermissionsScopes
    {
        private static readonly GroupCol Friend =
            new GroupCol { Label = Lang.Get("claims:gui-admin-group-friend"),   CommandToken = "friend",   Group = PermGroup.COMRADE };
        private static readonly GroupCol Citizen =
            new GroupCol { Label = Lang.Get("claims:gui-admin-group-citizen"),  CommandToken = "citizen",  Group = PermGroup.CITIZEN };
        private static readonly GroupCol Ally =
            new GroupCol { Label = Lang.Get("claims:gui-admin-group-ally"),     CommandToken = "ally",     Group = PermGroup.ALLY };
        private static readonly GroupCol Stranger =
            new GroupCol { Label = Lang.Get("claims:gui-admin-group-stranger"), CommandToken = "stranger", Group = PermGroup.STRANGER };

        private static readonly GroupCol[] StandardGroups = { Friend, Citizen, Ally, Stranger };

        private static readonly PermRow Build =
            new PermRow { Label = Lang.Get("claims:gui-build-title"),          SubCommand = "build",  Type = PermType.BUILD_AND_DESTROY_PERM, KeyPrefix = "build" };
        private static readonly PermRow Use =
            new PermRow { Label = Lang.Get("claims:gui-use-title"),            SubCommand = "use",    Type = PermType.USE_PERM,               KeyPrefix = "use" };
        private static readonly PermRow Attack =
            new PermRow { Label = Lang.Get("claims:gui-attack-animals-title"), SubCommand = "attack", Type = PermType.ATTACK_ANIMALS_PERM,    KeyPrefix = "attack" };

        private static readonly PermRow[] StandardPerms = { Build, Use, Attack };

        private static FlagRow[] StandardFlags() => new[]
        {
            new FlagRow { Label = Lang.Get("claims:gui-admin-flag-pvp"),   SubCommand = "pvp",   Key = "pvp-switch",   Get = h => h.pvpFlag,   Set = (h, v) => h.setPvp(v) },
            new FlagRow { Label = Lang.Get("claims:gui-admin-flag-fire"),  SubCommand = "fire",  Key = "fire-switch",  Get = h => h.fireFlag,  Set = (h, v) => h.setFire(v) },
            new FlagRow { Label = Lang.Get("claims:gui-admin-flag-blast"), SubCommand = "blast", Key = "blast-switch", Get = h => h.blastFlag, Set = (h, v) => h.setBlast(v), Inverted = true },
            new FlagRow { Label = Lang.Get("claims:gui-admin-flag-nomobspawn", claims.config.PLOT_NO_MOBSPAWN_FLAG_COST), SubCommand = "nomobspawn", Key = "nomobspawn-switch", Get = h => h.noMobSpawnFlag, Set = (h, v) => h.setNoMobSpawn(v) },
        };

        private static string OnOff(bool v) => v ? "on" : "off";

        public static PermissionsScope Plot => new PermissionsScope
        {
            Source = () => claims.clientDataStorage.clientPlayerInfo.CurrentPlotInfo.PermsHandler,
            Flags = StandardFlags(),
            Perms = StandardPerms,
            Groups = StandardGroups,
            FlagCommand = (flag, on) => $"/plot set {flag.SubCommand} {OnOff(on)}",
            PermCommand = (group, perm, on) => $"/plot set p {group.CommandToken} {perm.SubCommand} {OnOff(on)}"
        };

        public static PermissionsScope City => new PermissionsScope
        {
            Source = () => claims.clientDataStorage.clientPlayerInfo.CityInfo.PermsHandler,
            Flags = StandardFlags(),
            Perms = StandardPerms,
            Groups = StandardGroups,
            FlagCommand = (flag, on) => $"/city set {flag.SubCommand} {OnOff(on)}",
            PermCommand = (group, perm, on) => $"/city set p {group.CommandToken} {perm.SubCommand} {OnOff(on)}"
        };

        /// <summary>
        /// Plots group. Its commands carry the group name, and it only ever exposed build and use,
        /// for citizens only.
        /// </summary>
        public static PermissionsScope PlotsGroup(Func<string> groupName, Func<PermsHandler> source) => new PermissionsScope
        {
            Source = source,
            Flags = StandardFlags(),
            Perms = new[] { Build, Use },
            Groups = new[] { Citizen },
            FlagCommand = (flag, on) => $"/c plotsgroup set {flag.SubCommand} {groupName()} {OnOff(on)}",
            PermCommand = (group, perm, on) => $"/c plotsgroup set p {groupName() ?? "_"} {group.CommandToken} {perm.SubCommand} {OnOff(on)}"
        };
    }
}
