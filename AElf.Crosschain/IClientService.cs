namespace AElf.Crosschain
{
    public interface IClientService
    {
        void CreateClient(IClientBase clientCache);
        void UpdateRequestInterval(int interval);
    }
}