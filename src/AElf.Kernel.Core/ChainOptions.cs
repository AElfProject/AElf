namespace AElf.Kernel
{
    public class ChainOptions
    {
        public int ChainId { get; set; }
        public ChainType ChainType { get; set; }
        public NetType NetType { get; set; }
    }
    
    public enum ChainType
    {
        MainChain,
        SideChain,
        PoWChain
    }

    public enum NetType
    {
        MainNet,
        TestNet,
        CustomNet
    }
}