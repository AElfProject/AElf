namespace AElf.Node.AElfChain
{
    public interface INodeService
    {
        void Initialize(NodeConfiguration conf);
        bool Start();
        void Stop();
    }
}