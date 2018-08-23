namespace AElf.Node.AElfChain
{
    public interface INodeService
    {
        void Initialize(NodeConfiguation conf);
        bool Start();
        void Stop();
    }
}