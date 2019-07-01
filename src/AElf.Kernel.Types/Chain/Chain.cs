using Google.Protobuf;

namespace AElf.Kernel
{
    public partial class Chain
    {
        public Chain(int chainId, Hash genesisBlockHash)
        {
            Id = chainId;
            GenesisBlockHash = genesisBlockHash;
        }

        // Done: remove
        public byte[] Serialize()
        {
            return this.ToByteArray();
        }
    }
}