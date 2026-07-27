using claims.src.part.structure.conflict;

namespace claims.src.gui.playerGui.structures.cellElements
{
    /// <summary>Why we may declare war on this party.</summary>
    public enum CasusBelliKind
    {
        /// <summary>No justification - the entry exists only because of a cooldown or a pact.</summary>
        None,
        /// <summary>Their citizens killed ours on our land (City.Grievances).</summary>
        Grievance,
        /// <summary>An ally of ours is already at war with them, so we may join.</summary>
        AllyAtWar,
        /// <summary>A refused/expired ultimatum granted a free war (City.WarJustifications):
        /// no cooldown, no casus-belli check, no declaration cost while it lasts.</summary>
        FreeWar
    }

    /// <summary>One casus belli our party currently holds against a target party.</summary>
    public class ClientCasusBelliCellElement
    {
        public string TargetName { get; set; }
        public string TargetGuid { get; set; }
        public WarTargetType TargetType { get; set; }
        public CasusBelliKind Kind { get; set; }
        /// <summary>Unix seconds when this reason lapses. 0 = it holds as long as the ally's war does.</summary>
        public long ExpiresAt { get; set; }
        /// <summary>Re-declare cooldown: unix seconds until we may declare war on them again. 0 = none.</summary>
        public long CooldownUntil { get; set; }
        /// <summary>Non-aggression pact: unix seconds until it expires. 0 = no pact.</summary>
        public long PactUntil { get; set; }
        /// <summary>Cooling-off after breaking a union with them: unix seconds until war is allowed. 0 = none.</summary>
        public long UnionBreakUntil { get; set; }

        public ClientCasusBelliCellElement() { }

        public ClientCasusBelliCellElement(string targetName, string targetGuid, WarTargetType targetType,
            CasusBelliKind kind, long expiresAt)
        {
            TargetName = targetName;
            TargetGuid = targetGuid;
            TargetType = targetType;
            Kind = kind;
            ExpiresAt = expiresAt;
        }

        /// <summary>
        /// True if war on this target can be declared right now. Mirrors WarDeclarationHelper's gates:
        /// the union-break cooling-off outranks everything, then a free war bypasses the rest; otherwise
        /// the cooldown must have lapsed, an active pact needs a casus belli to tear up, and the server
        /// may require a casus belli outright.
        /// </summary>
        public bool CanDeclareNow(long now)
        {
            if (UnionBreakUntil > now) return false;
            if (Kind == CasusBelliKind.FreeWar) return true;
            if (CooldownUntil > now) return false;
            bool hasCasusBelli = Kind != CasusBelliKind.None;
            if (PactUntil > now && !hasCasusBelli) return false;
            if (claims.config.WAR_REQUIRE_CASUS_BELLI && !hasCasusBelli) return false;
            return true;
        }
    }
}
