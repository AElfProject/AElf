using AElf.Contracts.CrossChain;
using AElf.Kernel;
using AElf.Kernel.Blockchain.Application;
using Google.Protobuf;
using Volo.Abp.DependencyInjection;

namespace AElf.CrossChain.Grpc
{
    public interface ISideChainServerService
    {
        ResponseData GenerateResponse(Block block);
    }

    public class SideChainServerService : ISideChainServerService, ITransientDependency
    {
        private readonly IBlockExtraDataService _blockExtraDataService;

        public SideChainServerService(IBlockExtraDataService blockExtraDataService)
        {
            _blockExtraDataService = blockExtraDataService;
        }

        public ResponseData GenerateResponse(Block block)
        {
            var transactionStatusMerkleRoot = ExtractTransactionStatusMerkleTreeRoot(block.Header); 
            return new ResponseData
            {
                BlockData = new BlockData
                {
                    Height = block.Height,
                    ChainId = block.Header.ChainId,
                    Payload = new SideChainBlockData
                    {
                        SideChainHeight = block.Height,
                        BlockHeaderHash = block.GetHash(),
                        TransactionMerkleTreeRoot = transactionStatusMerkleRoot,
                        SideChainId = block.Header.ChainId
                    }.ToByteString()
                }
            };
        }
        
        private Hash ExtractTransactionStatusMerkleTreeRoot(BlockHeader header)
        {
            return Hash.Parser.ParseFrom(_blockExtraDataService.GetMerkleTreeRootExtraDataForTransactionStatus(header));
        }
    }
}