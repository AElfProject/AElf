namespace AElf.Management
{
    public class GlobalSetting
    {
        public const int DeployRetryTime = 3;

        public const string CommonConfigName = "config-common";

        public const string KeysConfigName = "config-keys";
        
        public const string RedisConfigName = "config-redis";
        
        public const string RedisServiceName = "service-redis";
        
        public const string RedisName = "set-redis";

        public const string ManagerServiceName = "service-manager";
        
        public const string ManagerName = "set-manager";

        public const string WorkerName = "deploy-worker";
        
        public const string LauncherServiceName = "service-launcher";
        
        public const string LauncherName = "deploy-launcher";
    }
}