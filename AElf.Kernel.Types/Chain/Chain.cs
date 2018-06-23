// ReSharper disable once CheckNamespace

using Google.Protobuf;

// ReSharper disable once CheckNamespace
namespace AElf.Kernel.Types
{
    public partial class Chain : IChain
    {
        public Chain(Hash chainId, Hash genesisBlockHash)
        {
            Id = chainId;
            GenesisBlockHash = genesisBlockHash;
        }

        public byte[] Serialize()
        {
            return this.ToByteArray();
        }
        
    }
}