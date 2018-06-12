using AElf.Cryptography.ECDSA;

namespace AElf.Kernel.Node
{
    public interface IAElfNode
    {
        void Start(ECKeyPair nodeKeyPair, bool startRpc);
    }
}