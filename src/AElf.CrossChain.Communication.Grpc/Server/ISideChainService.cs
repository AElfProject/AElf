using AElf.Contracts.CrossChain;
using AElf.Kernel;
using AElf.Kernel.Blockchain.Application;
using AElf.Types;
using Google.Protobuf;
using Volo.Abp.DependencyInjection;

namespace AElf.CrossChain.Grpc
{
//    public interface ISideChainService
//    {
//        CrossChainResponse GenerateResponse(Block block);
//    }
//
//    public class SideChainService : ISideChainService, ITransientDependency
//    {
//        private readonly IBlockExtraDataService _blockExtraDataService;
//
//        public SideChainService(IBlockExtraDataService blockExtraDataService)
//        {
//            _blockExtraDataService = blockExtraDataService;
//        }
//
//        public CrossChainResponse GenerateResponse(Block block)
//        {
//            var transactionStatusMerkleRoot = ExtractTransactionStatusMerkleTreeRoot(block.Header); 
//            return new CrossChainResponse
//            {
//                BlockData = new BlockData
//                {
//                    Height = block.Height,
//                    ChainId = block.Header.ChainId,
//                    Payload = new SideChainBlockData
//                    {
//                        SideChainHeight = block.Height,
//                        BlockHeaderHash = block.GetHash(),
//                        TransactionMerkleTreeRoot = transactionStatusMerkleRoot,
//                        SideChainId = block.Header.ChainId
//                    }.ToByteString()
//                }
//            };
//        }
//        
//        private Hash ExtractTransactionStatusMerkleTreeRoot(BlockHeader header)
//        {
//            return Hash.Parser.ParseFrom(_blockExtraDataService.GetMerkleTreeRootExtraDataForTransactionStatus(header));
//        }
//    }
}