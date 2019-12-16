using AElf.Types;
using Google.Protobuf;

namespace AElf.CrossChain.Indexing.Infrastructure
{
    public interface ITransactionInputForBlockMiningDataProvider
    {
        void AddTransactionInputForBlockMining(Hash blockHash, CrossChainTransactionInput crossChainTransactionInput);
        
        CrossChainTransactionInput GetTransactionInputForBlockMining(Hash blockHash);

        void ClearExpiredTransactionInput(long blockHeight);
    }
    
    public class CrossChainTransactionInput
    {
        public long PreviousBlockHeight { get; set; }
        public string MethodName { get; set; }
        public ByteString Value { get; set; }
    }
}