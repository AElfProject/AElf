// ReSharper disable once CheckNamespace

using Google.Protobuf;
using ServiceStack.DataAnnotations;

// ReSharper disable once CheckNamespace
namespace AElf.Kernel
{
    public partial class Chain : IChain
    {
        public void UpdateCurrentBlock(Block block)
        {
            block.Header.Index = CurrentBlockHeight;
            CurrentBlockHeight += 1;
            CurrentBlockHash = block.GetHash();
        }
        public Hash GenesisBlockHash { get; set; }

        public Chain(Hash id)
        {
            Id = id;
            CurrentBlockHash = null;
            CurrentBlockHeight = 0;
        }

        public byte[] Serialize()
        {
            return this.ToByteArray();
        }
    }
}