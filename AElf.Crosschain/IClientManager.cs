namespace AElf.Crosschain
{
    public interface IClientManager
    {
        void CreateClient(IClientBase clientCache);
        void UpdateRequestInterval(int interval);
    }
}