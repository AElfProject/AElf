using AElf.Cryptography.ECDSA;

namespace AElf.Node
{
    public class NodeConfiguation
    {
        public ECKeyPair KeyPair { get; set; }
        public bool WithRpc { get; set; }
        public string LauncherAssemblyLocation { get; set; }
    }
}