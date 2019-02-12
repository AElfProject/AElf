using System.Threading.Tasks;

namespace AElf.Kernel.BlockService
{
    public interface IBlockExtraDataGenerationService
    {
        // todo: redefine needed especially return type, maybe a new structure ExtraData is needed.
        Task FillBlockExtraData(Block block);
    }
}