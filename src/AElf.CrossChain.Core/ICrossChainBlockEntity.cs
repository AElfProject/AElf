using AElf.Types;
using Google.Protobuf;

namespace AElf.CrossChain
{
    public interface ICrossChainBlockEntity : IMessage
    {
        long Height { get; set; }
        int ChainId { get; set; }
        Hash TransactionStatusMerkleTreeRoot { get; set; }
    }
}