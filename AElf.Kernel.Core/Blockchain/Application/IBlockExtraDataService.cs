using System.Threading.Tasks;

namespace AElf.Kernel.Blockchain.Application
{
    public interface IBlockExtraDataService
    {
        // todo: redefine needed especially return type, maybe a new structure ExtraData is needed.
        Task FillBlockExtraData(int chainId, Block block);
    }
}