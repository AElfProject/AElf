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
            var configuration = context.Services.GetConfiguration();

            Configure<MessageQueueOptions>(options => { configuration.GetSection("MessageQueue").Bind(options); });

            var messageQueueOptions = context.Services.GetRequiredServiceLazy<MessageQueueOptions>().Value;

            Configure<AbpRabbitMqEventBusOptions>(options =>
            {
                options.ClientName = messageQueueOptions.ClientName;
                options.ExchangeName = messageQueueOptions.ExchangeName;
            });

            Configure<AbpRabbitMqOptions>(options =>
            {
                options.Connections.Default.HostName = messageQueueOptions.HostName;
                options.Connections.Default.Port = messageQueueOptions.Port;
            });
        }
    }
}