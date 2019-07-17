using System.Collections.Generic;
using System.Threading.Tasks;
using AElf.Types;

namespace AElf.Contracts.TestKet.AEDPoSExtension
{
    public interface IBlockMiningService
    {
        Task<Dictionary<Hash, Address>> DeploySystemContracts(Dictionary<Hash, byte[]> nameToCode);
        Task MineBlockAsync(List<Transaction> transactions);
    }
}