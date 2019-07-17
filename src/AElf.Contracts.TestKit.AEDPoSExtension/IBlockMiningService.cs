using System.Threading.Tasks;

namespace AElf.Contracts.TestKet.AEDPoSExtension
{
    public interface IBlockMiningService
    {
        Task MineBlockAsync();
    }
}