namespace claims.src.part.structure.war
{
    /// <summary>
    /// What declaring a war took from the declarer up front - the fee, and a free-war justification
    /// if one was spent getting past the gates.
    ///
    /// A declaration that needs the other side's agreement is only a letter at first, and the letter
    /// may be refused, expire, or be withdrawn. The price was still paid at the moment it was sent
    /// (checking the money later would let a city declare a war it can no longer afford), so what was
    /// taken is remembered here and given back when the letter comes to nothing.
    /// </summary>
    public class DeclarationCharge
    {
        /// <summary>Money withdrawn from the declaring party's account. 0 when the fee is off.</summary>
        public double Cost { get; set; }

        /// <summary>
        /// Expiry of the free-war justification consumed by this declaration, 0 when none was. Kept
        /// as the original expiry rather than a flag: giving back a fresh window would let a city
        /// stretch one refused ultimatum indefinitely by declaring and withdrawing.
        /// </summary>
        public long JustificationExpire { get; set; }

        /// <summary>Guid of the party the justification was held against - where to put it back.</summary>
        public string TargetGuid { get; set; } = "";

        public bool IsEmpty => Cost <= 0 && JustificationExpire <= 0;
    }
}
