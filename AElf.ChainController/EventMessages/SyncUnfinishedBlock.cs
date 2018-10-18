namespace AElf.ChainController.EventMessages
{
    public sealed class SyncUnfinishedBlock
    {
        public ulong TargetHeight { get; }
        
        public SyncUnfinishedBlock(ulong targetHeight)
        {
            TargetHeight = targetHeight;
        }
    }
}