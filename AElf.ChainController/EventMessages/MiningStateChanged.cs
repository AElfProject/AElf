namespace AElf.ChainController.EventMessages
{
    public class MiningStateChanged
    {
        public bool IsMining { get; }

        public MiningStateChanged(bool isMining)
        {
            IsMining = isMining;
        }
    }
}