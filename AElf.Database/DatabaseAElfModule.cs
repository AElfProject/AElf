using System;
using AElf.Common.Enums;
using AElf.Configuration;
using AElf.Modularity;
using Autofac;
using Microsoft.Extensions.DependencyInjection;
using Volo.Abp.Modularity;

namespace AElf.Database
{
    public class DatabaseAElfModule : AElfModule
    {
        public override void ConfigureServices(ServiceConfigurationContext context)
        {
            //TODO: should remove switch context, should not read config file here.
            var services = context.Services;
            switch (DatabaseConfig.Instance.Type)
            {
                case DatabaseType.InMemory:
                    services.AddSingleton<IKeyValueDatabase,InMemoryDatabase>();
                    break;
                case DatabaseType.Redis:
                    services.AddSingleton<IKeyValueDatabase,RedisDatabase>();
                    break;
                case DatabaseType.Ssdb:
                    services.AddSingleton<IKeyValueDatabase,SsdbDatabase>();
                    break;
            }
        }

    }
}