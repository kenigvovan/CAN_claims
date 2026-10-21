using claims.src.economy;
using Vintagestory.API.Server;

namespace claims.scenarios.Infrastructure;

public enum FakeMoneyOp { Withdraw, Deposit, Transfer }

/// <summary>
/// An economy with real balances, for scenarios that need "not enough money" or a failing
/// operation - neither ever happens with the default NoopMoneyProvider.
///
/// Every account exists and starts at 0, as with the noop provider: the mod does not always
/// create accounts before using them, and a fake that refused unknown accounts would fail flows
/// the real economy lets through. A zero amount succeeds; what the real economy does with it is
/// an open question (O9), not something to decide here.
/// </summary>
public sealed class FakeMoneyProvider : IMoneyProvider
{
    private readonly object sync = new();
    private readonly Dictionary<string, decimal> balances = new();
    private readonly List<(FakeMoneyOp Op, string? Account, MoneyOperationResult Result)> plannedFailures = new();
    private readonly List<string> log = new();

    /// <summary>Every operation in order, failed ones included: "Transfer a->b 40: Success".</summary>
    public IReadOnlyList<string> Log { get { lock (sync) return log.ToList(); } }

    public void SetBalance(string account, decimal amount)
    {
        lock (sync) balances[account] = amount;
    }

    /// <summary>Sum of all balances. Moving money must not change it.</summary>
    public decimal Total()
    {
        lock (sync) return balances.Values.Sum();
    }

    /// <summary>
    /// Makes the next <paramref name="op"/> fail with <paramref name="result"/> and change nothing.
    /// With <paramref name="account"/> set, only an operation touching that account counts.
    /// </summary>
    public void FailNext(FakeMoneyOp op, string? account = null,
        MoneyOperationResult result = MoneyOperationResult.FailedSourceWithdraw)
    {
        lock (sync) plannedFailures.Add((op, account, result));
    }

    /// <summary>
    /// Makes the next <paramref name="op"/> touching <paramref name="account"/> throw, as a crashing
    /// economy mod would - for code that must survive an exception, not only a failed result.
    /// </summary>
    public void ThrowNext(FakeMoneyOp op, string account)
    {
        lock (sync) plannedFailures.Add((op, account, ThrowMarker));
    }

    // Not a real result: TakePlannedFailure turns it into an exception.
    private const MoneyOperationResult ThrowMarker = (MoneyOperationResult)(-1);

    public decimal GetBalance(string account)
    {
        lock (sync) return balances.TryGetValue(account, out decimal b) ? b : 0m;
    }

    public MoneyOperationResult Withdraw(string account, decimal amount)
    {
        lock (sync)
        {
            MoneyOperationResult result = TakePlannedFailure(FakeMoneyOp.Withdraw, account)
                ?? (amount < 0 ? MoneyOperationResult.WrongParameter
                    : Balance(account) < amount ? MoneyOperationResult.SourceNotEnoughMoney
                    : MoneyOperationResult.Success);
            if (result == MoneyOperationResult.Success) balances[account] = Balance(account) - amount;
            log.Add($"Withdraw {account} {amount}: {result}");
            return result;
        }
    }

    public MoneyOperationResult Deposit(string account, decimal amount)
    {
        lock (sync)
        {
            MoneyOperationResult result = TakePlannedFailure(FakeMoneyOp.Deposit, account)
                ?? (amount < 0 ? MoneyOperationResult.WrongParameter : MoneyOperationResult.Success);
            if (result == MoneyOperationResult.Success) balances[account] = Balance(account) + amount;
            log.Add($"Deposit {account} {amount}: {result}");
            return result;
        }
    }

    public MoneyOperationResult Transfer(string fromAccount, string toAccount, decimal amount)
    {
        lock (sync)
        {
            MoneyOperationResult result = TakePlannedFailure(FakeMoneyOp.Transfer, fromAccount, toAccount)
                ?? (amount < 0 ? MoneyOperationResult.WrongParameter
                    : Balance(fromAccount) < amount ? MoneyOperationResult.SourceNotEnoughMoney
                    : MoneyOperationResult.Success);
            if (result == MoneyOperationResult.Success)
            {
                balances[fromAccount] = Balance(fromAccount) - amount;
                balances[toAccount] = Balance(toAccount) + amount;
            }
            log.Add($"Transfer {fromAccount}->{toAccount} {amount}: {result}");
            return result;
        }
    }

    public bool NewAccount(string account, IDictionary<string, object>? meta = null)
    {
        lock (sync) balances.TryAdd(account, 0m);
        return true;
    }

    public bool UpdateAccount(string account, IDictionary<string, object>? meta = null) => true;

    public bool DeleteAccount(string account)
    {
        lock (sync) balances.Remove(account);
        return true;
    }

    public bool AccountExists(string account) => true;

    public bool SupportsPlayerWallet => true;
    public bool SupportsRemoteCoinTransfer => true;

    // Coins are a separate area (R17); these tests never hand coins over.
    public decimal TakeCoinItemsFromActiveSlot(IServerPlayer player) => 0m;
    public void GiveCoinItemsToPlayer(IServerPlayer player, decimal amount) { }

    public IReadOnlyList<CoinDisplayInfo> CoinDenominations { get; } = new List<CoinDisplayInfo>();

    private decimal Balance(string account) => balances.TryGetValue(account, out decimal b) ? b : 0m;

    private MoneyOperationResult? TakePlannedFailure(FakeMoneyOp op, params string[] accounts)
    {
        int i = plannedFailures.FindIndex(f => f.Op == op && (f.Account == null || accounts.Contains(f.Account)));
        if (i < 0) return null;
        MoneyOperationResult result = plannedFailures[i].Result;
        plannedFailures.RemoveAt(i);
        if (result == ThrowMarker)
        {
            log.Add($"{op} {string.Join("->", accounts)}: threw");
            throw new InvalidOperationException($"FakeMoneyProvider: planned exception in {op}");
        }
        return result;
    }
}
