namespace AElf.OS.Network
{
    public partial class Node
    {
        public string ToDiagnosticString()
        {
            return $"{{ endpoint: {Endpoint}, key: {Pubkey.ToHex().Substring(0, 45)}... }}";
        }
    }
}