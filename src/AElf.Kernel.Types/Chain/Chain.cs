using AElf.Types;

namespace AElf.Kernel;

public partial class Chain
{
    public Chain(int chainId, Hash genesisBlockHash)
    {
        Id = chainId;
        GenesisBlockHash = genesisBlockHash;
    }
}