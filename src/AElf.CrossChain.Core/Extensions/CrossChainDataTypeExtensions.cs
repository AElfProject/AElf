using System.Linq;
using AElf.Standards.ACS7;
using AElf.Types;
using Google.Protobuf;

namespace AElf.CrossChain
{
    public static class CrossChainDataTypeExtensions
    {
        public static bool IsNullOrEmpty(this CrossChainBlockData crossChainBlockData)
        {
            return crossChainBlockData == null || crossChainBlockData.ParentChainBlockDataList.Count == 0 &&
                crossChainBlockData.SideChainBlockDataList.Count == 0;
        }
        
        public static ByteString ExtractCrossChainExtraDataFromCrossChainBlockData(this CrossChainBlockData crossChainBlockData)
        {
            if (crossChainBlockData.IsNullOrEmpty() || crossChainBlockData.SideChainBlockDataList.Count == 0)
                return ByteString.Empty;

            var indexedSideChainBlockData = new IndexedSideChainBlockData
            {
                SideChainBlockDataList = {crossChainBlockData.SideChainBlockDataList}
            };

            return indexedSideChainBlockData.ExtractCrossChainExtraDataFromCrossChainBlockData();
        }

        public static ByteString ExtractCrossChainExtraDataFromCrossChainBlockData(
            this IndexedSideChainBlockData indexedSideChainBlockData)
        {
            var txRootHashList = indexedSideChainBlockData.SideChainBlockDataList
                .Select(scb => scb.TransactionStatusMerkleTreeRoot).ToList();

            var calculatedSideChainTransactionsRoot = BinaryMerkleTree.FromLeafNodes(txRootHashList).Root;
            return new CrossChainExtraData
                {
                    TransactionStatusMerkleTreeRoot = calculatedSideChainTransactionsRoot
                }
                .ToByteString();
        }
        

        public static bool IsNullOrEmpty(this IndexedSideChainBlockData indexedSideChainBlockData)
        {
            return indexedSideChainBlockData == null || indexedSideChainBlockData.SideChainBlockDataList.Count == 0;
        }
    }
}