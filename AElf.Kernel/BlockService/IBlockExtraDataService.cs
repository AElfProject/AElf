using System.Threading.Tasks;

namespace AElf.Kernel.BlockService
{
    public interface IBlockExtraDataService
    {
        // todo: redefine needed especially return type, maybe a new structure ExtraData is needed.
        Task<byte[]> GenerateExtraData();
    }
}