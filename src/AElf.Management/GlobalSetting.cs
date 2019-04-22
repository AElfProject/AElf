namespace AElf.Management
{
    public class GlobalSetting
    {
        public const int DeployRetryTime = 3;

        public const string CommonConfigName = "config-common";

        public const string ChainInfoConfigName = "config-chaininfo";

        public const string KeysConfigName = "config-keys";

        public const string CertsConfigName = "config-certs";

        public const string RedisConfigName = "config-redis";

        public const string RedisServiceName = "service-redis";

        public const string RedisName = "set-redis";

        public const string LighthouseServiceName = "service-lighthouse";

        public const string LighthouseName = "set-lighthouse";

        public const string WorkerName = "deploy-worker";

        public const string LauncherServiceName = "service-launcher";

        public const string LauncherName = "deploy-launcher";

        public const string MonitorServiceName = "service-monitor";

        public const string MonitorName = "deploy-monitor";

        public const int NodePort = 30800;
        public const int RpcPort = 30600;
        public const int GrpcPort = 40001;
        public const int MonitorPort = 9099;
    }
}