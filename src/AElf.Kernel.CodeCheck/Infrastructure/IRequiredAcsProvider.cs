using AElf.Kernel.SmartContract;

namespace AElf.Kernel.CodeCheck.Infrastructure;

public interface IRequiredAcsProvider
{
    Task<RequiredAcs> GetRequiredAcsInContractsAsync(Hash blockHash, long blockHeight);
}