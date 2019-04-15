namespace AElf.Blockchains.BasicBaseChain
{
    public class NodeOptions
    {
        public NodeType NodeType { get; set; }
    }

    public enum NodeType
    {
        MainNet,
        TestNet,
        CustomNet
    }
}