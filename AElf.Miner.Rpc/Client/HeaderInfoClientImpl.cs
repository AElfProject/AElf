namespace AElf.Miner.Rpc.Client
{
    public class HeaderInfoClientImpl
    {
        readonly HeaderInfoRpc.HeaderInfoRpcClient client;

        public HeaderInfoClientImpl(HeaderInfoRpc.HeaderInfoRpcClient client)
        {
            this.client = client;
        }
    }
}