using System.Collections.Generic;
using System.Threading.Tasks;
using Acs3;
using AElf.Types;

namespace AElf.Contracts.TestKet.AEDPoSExtension
{
    public interface IBlockMiningService
    {
        Task<Dictionary<Hash, Address>> DeploySystemContractsAsync(Dictionary<Hash, byte[]> nameToCode);
        Task MineBlockAsync(List<Transaction> transactions = null, bool withException = false);
        Task MineBlockToNextRoundAsync();
        Task MineBlockAsync(long targetHeight);
        void SkipTime(int seconds);
    }
}