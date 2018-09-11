
namespace AElf.Configuration.Config.GRPC
{
    [ConfigFile(FileName = "grpclocal.json")]
    public class GrpcLocalConfig : ConfigBase<GrpcLocalConfig>
    {
        public bool Client { get; set; }
        public bool Server { get; set; }
        public int WaitingIntervalInMillisecond { get; set; }
        public string LocalServerIP { get; set; }
        public int LocalServerPort { get; set; }
    }
}