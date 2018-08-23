namespace AElf.Node.AElfChain
{
    public interface INode
    {
        void Register(INodeService s);
        void Initialize(NodeConfiguation conf);
        bool Start();
    }
}