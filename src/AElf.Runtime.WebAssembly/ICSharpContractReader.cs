using AElf.Types;

namespace AElf.Runtime.WebAssembly;

public interface ICSharpContractReader
{
    Task<long> GetBalanceAsync(Address from, Address owner, string? symbol = null);
    Task<Hash> GetContractHashAsync(Address from, Address contractAddress);
    //Task Transfer(Address from, Address to, long amount, string? symbol = null);
}