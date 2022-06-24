using System;
using System.Net.Security;
using System.Security.Authentication;
using System.Threading.Tasks;
using AElf.Kernel;
using AElf.Modularity;
using AElf.WebApp.Application;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using RabbitMQ.Client;
using Volo.Abp;
using Volo.Abp.AutoMapper;
using Volo.Abp.EventBus.RabbitMq;
using Volo.Abp.Modularity;
using Volo.Abp.RabbitMQ;
using Volo.Abp.Threading;

namespace AElf.WebApp.MessageQueue
{
    [DependsOn(typeof(CoreKernelAElfModule), typeof(AbpEventBusRabbitMqModule),
        typeof(CoreApplicationWebAppAElfModule))]
    public class MessageQueueAElfModule : AElfModule
    {
        public override void ConfigureServices(ServiceConfigurationContext context)
        {
            var configuration = context.Services.GetConfiguration();
            Configure<AbpAutoMapperOptions>(options => { options.AddMaps<MessageQueueAElfModule>(); });
            ConfigureRabbitMqEventBus(configuration);
        }

        public override void OnApplicationShutdown(ApplicationShutdownContext context)
        {
            AsyncHelper.RunSync(() => OnApplicationShutdownAsync(context));
        }

        public override async Task OnApplicationShutdownAsync(ApplicationShutdownContext context)
        {
            var taskManageService = context.ServiceProvider.GetRequiredService<IEventSendTaskManager>();
            await taskManageService.StopAsync();
        }

        private void ConfigureRabbitMqEventBus(IConfiguration configuration)
        {
            Configure<AbpRabbitMqEventBusOptions>(options =>
            {
                var messageQueueConfig = configuration.GetSection("MessageQueue");
                options.ExchangeType = RabbitMqConsts.ExchangeTypes.Topic;
                options.ClientName = messageQueueConfig.GetSection("ClientName").Value;
                options.ExchangeName = messageQueueConfig.GetSection("ExchangeName").Value;
            });

            Configure<AbpRabbitMqOptions>(options =>
            {
                var messageQueueConfig = configuration.GetSection("MessageQueue");
                var hostName = messageQueueConfig.GetSection("HostName").Value;

                options.Connections.Default.HostName = hostName;
                options.Connections.Default.Port = int.Parse(messageQueueConfig.GetSection("Port").Value);
                options.Connections.Default.UserName = messageQueueConfig.GetSection("UserName").Value;
                options.Connections.Default.Password = messageQueueConfig.GetSection("Password").Value;
                options.Connections.Default.Ssl = new SslOption
                {
                    Enabled = true,
                    ServerName = hostName,
                    Version = SslProtocols.Tls12,
                    AcceptablePolicyErrors = SslPolicyErrors.RemoteCertificateNameMismatch |
                                             SslPolicyErrors.RemoteCertificateChainErrors
                };
                options.Connections.Default.VirtualHost = "/";
                options.Connections.Default.Uri = new Uri(messageQueueConfig.GetSection("Uri").Value);
            });
        }
    }
}