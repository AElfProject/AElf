
namespace AElf.Configuration.Config.GRPC
{
    [ConfigFile(FileName = "grpclocal.json")]
    public class GrpcLocalConfig : ConfigBase<GrpcLocalConfig>
    {
        public bool Client { get; set; }
        public bool SideChainServer { get; set; }
        public bool ParentChainServer { get; set; }
        public int WaitingIntervalInMillisecond { get; set; }
        public string LocalServerIP { get; set; }
        public int LocalSideChainServerPort { get; set; }
        public int LocalParentChainServerPort { get; set; }
    }
}