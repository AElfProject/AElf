namespace AElf.Kernel.Node.Network.Data
{
    public enum MessageTypes
    {
        AskForTx = 0,
        AskForPeers = 1,
        ReturnPeers = 2,
        BroadcastTx = 3,
        Ok = 3
    }
}