namespace AElf.OS.Network
{
    public partial class NodeList
    {
        public string ToDiagnosticString()
        {
            return string.Join(", ", Nodes);
        }
    }
}