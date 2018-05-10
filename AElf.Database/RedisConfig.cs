using Microsoft.Extensions.Configuration;
using ServiceStack.Redis;

namespace AElf.Database
{
    public class RedisConfig : ConfigurationSection
    {
        public RedisConfig(ConfigurationRoot root, string path) : base(root, path)
        {
        }
    }
}