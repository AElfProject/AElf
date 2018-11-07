namespace AElf.Node.AElfChain
{
    public interface INode
    {
        void Register(INodeService s);
        void Initialize(NodeConfiguration conf);
        bool Start();
        bool Stop();
    }
}