namespace AElf.ChainController.EventMessages
{
    public sealed class ExecutionStateChanged
    {
        public bool IsExecuting { get; }

        public ExecutionStateChanged(bool isExecuting)
        {
            IsExecuting = isExecuting;
        }
    }
}