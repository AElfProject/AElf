namespace AElf.Management.Interfaces
{
    public interface INodeService
    {
        bool IsAlive(string chainId);

        bool IsForked(string chainId);
    }
}