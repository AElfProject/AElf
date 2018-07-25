namespace AElf.CLI2.SDK
{
    public interface IAElfSdk
    {
        IChain Chain();
    }

    public interface IChain
    {
        void ConnectChain();
    }
}