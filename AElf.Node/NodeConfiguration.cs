using AElf.Cryptography.ECDSA;

namespace AElf.Node
{
    public class NodeConfiguration
    {
        public ECKeyPair KeyPair { get; set; }
        public bool WithRpc { get; set; }
        public string LauncherAssemblyLocation { get; set; }
    }
}