using AElf.Kernel;
using AElf.Kernel.Blockchain.Application;
using Volo.Abp.DependencyInjection;

namespace AElf.CrossChain.Grpc
{
    public interface ISideChainServerService
    {
        ResponseSideChainBlockData GenerateResponse(Block block);
    }

    public class SideChainServerService : ISideChainServerService, ITransientDependency
    {
        private readonly IBlockExtraDataService _blockExtraDataService;

        public SideChainServerService(IBlockExtraDataService blockExtraDataService)
        {
            _blockExtraDataService = blockExtraDataService;
        }

        public ResponseSideChainBlockData GenerateResponse(Block block)
        {
            var transactionStatusMerkleRoot = ExtractTransactionStatusMerkleTreeRoot(block.Header); 
            return new ResponseSideChainBlockData
            {
                Success = block.Header != null,
                BlockData = new SideChainBlockData
                {
                    SideChainHeight = block.Height,
                    BlockHeaderHash = block.GetHash(),
                    TransactionMerkleTreeRoot = transactionStatusMerkleRoot,
                    SideChainId = block.Header.ChainId
                }
            };
        }
        
        private Hash ExtractTransactionStatusMerkleTreeRoot(BlockHeader header)
        {
            return Hash.Parser.ParseFrom(_blockExtraDataService.GetMerkleTreeRootExtraDataForTransactionStatus(header));
        }
    }
}