using AElf.Modularity;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Volo.Abp.EventBus.RabbitMq;
using Volo.Abp.Modularity;
using Volo.Abp.RabbitMQ;

namespace AElf.WebApp.MessageQueue
{
    [DependsOn(typeof(AbpEventBusRabbitMqModule))]
    public class MessageQueueAElfModule : AElfModule
    {
        public override void ConfigureServices(ServiceConfigurationContext context)
        {
            Configure<AbpRabbitMqEventBusOptions>(options =>
            {
                options.ClientName = "AElf";
                options.ExchangeName = "AElfExchange";
            });

            Configure<AbpRabbitMqOptions>(options =>
            {
                options.Connections.Default.HostName = "localhost";
                options.Connections.Default.Port = 5672;
            });

            var configuration = context.Services.GetConfiguration();
            Configure<MessageQueueEnableOptions>(options =>
            {
                var consensusOptions = configuration.GetSection("MessageQueue");
                consensusOptions.Bind(options);
            });
        }
    }
}