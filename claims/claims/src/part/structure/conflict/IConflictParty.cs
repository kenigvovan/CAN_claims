using claims.src.part;
using System.Collections.Generic;

namespace claims.src.part.structure.conflict
{
    public interface IConflictParty
    {
        string Guid { get; }
        string MoneyAccountName { get; }
        string GetPartName();
        HashSet<Conflict> RunningConflicts { get; }
        bool Neutral { get; set; }
        /// <summary>Neutral AND the server still offers neutrality - what every rule should ask.</summary>
        bool IsNeutral { get; }
        /// <summary>Unix seconds when neutrality was last given up; 0 if it never was.</summary>
        long NeutralDroppedAt { get; set; }
        List<City> GetCities();
        void AddHostileParty(IConflictParty party);
        void RemoveHostileParty(IConflictParty party);
        IEnumerable<IConflictParty> GetHostileParties();
        bool saveToDatabase(bool update = true);
    }
}
