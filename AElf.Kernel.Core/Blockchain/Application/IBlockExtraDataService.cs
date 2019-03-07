using System;
using System.Threading.Tasks;
using Google.Protobuf;

namespace AElf.Kernel.Blockchain.Application
{
    public interface IBlockExtraDataService
    {
        // todo: redefine needed especially return type, maybe a new structure ExtraData is needed.
        Task FillBlockExtraData(BlockHeader blockHeader);
        ByteString GetBlockExtraData(Type blockExtraDataProviderType, BlockHeader blockHeader);
    }
}