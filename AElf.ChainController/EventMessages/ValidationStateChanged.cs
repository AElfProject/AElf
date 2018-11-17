namespace AElf.ChainController.EventMessages
{
    public class ValidationStateChanged
    {
        public string BlockHashToHex { get; }
        public ulong Index { get; }
        public bool Start { get; }
        public BlockValidationResult BlockValidationResult { get; }

        public ValidationStateChanged(string blockHashToHex, ulong index, bool start,
            BlockValidationResult blockValidationResult)
        {
            BlockHashToHex = blockHashToHex;
            Index = index;
            Start = start;
            BlockValidationResult = blockValidationResult;
        }
    }
}