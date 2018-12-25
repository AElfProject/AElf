using System;
using AElf.Common.Enums;
using AElf.Configuration;
using AElf.Modularity;
using Microsoft.Extensions.DependencyInjection;
using Volo.Abp.Modularity;

namespace AElf.Database
{
    public class DatabaseAElfModule : AElfModule
    {
        public override void ConfigureServices(ServiceConfigurationContext context)
        {
            if (DatabaseConfig.Instance.Type == DatabaseType.InMemory)
            {
                context.Services.AddSingleton<IKeyValueDatabase, InMemoryDatabase>();
            }
            else if (DatabaseConfig.Instance.Type == DatabaseType.Ssdb)
            {
                context.Services.AddSingleton<IKeyValueDatabase, SsdbDatabase>();
            }
            else if (DatabaseConfig.Instance.Type == DatabaseType.InMemory)
            {
                context.Services.AddSingleton<IKeyValueDatabase, RedisDatabase>();
            }
        }
    }
}