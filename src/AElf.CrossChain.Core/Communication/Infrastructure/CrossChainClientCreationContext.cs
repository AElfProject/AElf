namespace AElf.CrossChain.Communication.Infrastructure;

public class CrossChainClientCreationContext
{
    public string RemoteServerHost { get; set; }
    public int RemoteServerPort { get; set; }
    public int RemoteChainId { get; set; }
    public int LocalChainId { get; set; }

    public bool IsClientToParentChain { get; set; }
}