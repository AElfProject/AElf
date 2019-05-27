namespace AElf.OS.Network.Grpc
{
    public class GrpcRequest
    {
        public int TimeoutMs { get; set; }
        public string ErrorMessage { get; set; }
        public string MetricName { get; set; }
        public string MetricInfo { get; set; }
    }
}