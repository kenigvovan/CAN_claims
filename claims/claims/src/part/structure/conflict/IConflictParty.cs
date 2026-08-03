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
        List<City> GetCities();
        void AddHostileParty(IConflictParty party);
        void RemoveHostileParty(IConflictParty party);
        IEnumerable<IConflictParty> GetHostileParties();
        bool saveToDatabase(bool update = true);
    }
}
