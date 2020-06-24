using System.Collections.Generic;
using System.Threading.Tasks;
using AElf.Types;

namespace AElf.ContractTestKit.AEDPoSExtension
{
    public interface IBlockMiningService
    {
        Task<Dictionary<Hash, Address>> DeploySystemContractsAsync(Dictionary<Hash, byte[]> nameToCode, bool deployConsensusContract = true);
        Task MineBlockAsync(List<Transaction> transactions = null, bool withException = false);
        Task<long> MineBlockToNextRoundAsync();
        Task<long> MineBlockToNextTermAsync();
        Task MineBlockAsync(long targetHeight);
        void SkipTime(int seconds);
    }
}