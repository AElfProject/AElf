using System.Threading.Tasks;
using AElf.Kernel;
using AElf.Kernel.Account.Application;
using AElf.Kernel.Consensus.AEDPoS.Application;
using AElf.Types;

namespace AElf.Contracts.Economic.TestBase;

public class MockRandomNumberProvider : IRandomNumberProvider
{
    private readonly IAccountService _accountService;

    public MockRandomNumberProvider(IAccountService accountService)
    {
        _accountService = accountService;
    }

    public async Task<byte[]> GenerateRandomProofAsync(IChainContext chainContext)
    {
        return await _accountService.ECVrfProveAsync(Hash.Empty.ToByteArray());
    }
}