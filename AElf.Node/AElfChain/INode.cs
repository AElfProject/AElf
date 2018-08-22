using System.Threading.Tasks;
using AElf.Cryptography.ECDSA;
using AElf.Kernel;
using AElf.Miner.Miner;

namespace AElf.Node.AElfChain
{
    public interface IAElfNode
    {
        bool Start(ECKeyPair nodeKeyPair);
    }
}