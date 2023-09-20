using AElf.Types;

namespace AElf.Runtime.WebAssembly;

public interface ICSharpContractReader
{
    Task<long> GetBalanceAsync(Address owner, string? symbol = null);
}