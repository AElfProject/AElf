using AElf.Cryptography.ECDSA;

namespace AElf.Node
{
    public class NodeConfiguration
    {
        public bool WithRpc { get; set; }
        public string LauncherAssemblyLocation { get; set; }
    }
}