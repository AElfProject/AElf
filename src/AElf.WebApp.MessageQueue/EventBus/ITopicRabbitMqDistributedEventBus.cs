using System.Collections.Generic;
using System.Threading.Tasks;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Options;
using RabbitMQ.Client;
using Volo.Abp.DependencyInjection;
using Volo.Abp.EventBus;
using Volo.Abp.EventBus.Distributed;
using Volo.Abp.EventBus.RabbitMq;
using Volo.Abp.Guids;
using Volo.Abp.MultiTenancy;
using Volo.Abp.RabbitMQ;
using Volo.Abp.Timing;
using Volo.Abp.Uow;

namespace AElf.WebApp.MessageQueue.EventBus;

public interface ITopicRabbitMqDistributedEventBus
{
    Task PublishTopicMessageAsync(string topic, object eventData, IBasicProperties properties = null,
        Dictionary<string, object> headersArguments = null);
}

[Dependency(ReplaceServices = true)]
[ExposeServices(typeof(ITopicRabbitMqDistributedEventBus))]
public class TopicRabbitMqDistributedEventBus : RabbitMqDistributedEventBus, ITopicRabbitMqDistributedEventBus,
    ISingletonDependency
{
    public TopicRabbitMqDistributedEventBus(IOptions<AbpRabbitMqEventBusOptions> options,
        IConnectionPool connectionPool, IRabbitMqSerializer serializer, IServiceScopeFactory serviceScopeFactory,
        IOptions<AbpDistributedEventBusOptions> distributedEventBusOptions,
        IRabbitMqMessageConsumerFactory messageConsumerFactory, ICurrentTenant currentTenant,
        IUnitOfWorkManager unitOfWorkManager, IGuidGenerator guidGenerator, IClock clock,
        IEventHandlerInvoker eventHandlerInvoker) : base(options, connectionPool, serializer, serviceScopeFactory,
        distributedEventBusOptions, messageConsumerFactory, currentTenant, unitOfWorkManager, guidGenerator, clock,
        eventHandlerInvoker)
    {
    }

    public async Task PublishTopicMessageAsync(string topic, object eventData, IBasicProperties properties = null,
        Dictionary<string, object> headersArguments = null)
    {
        var body = Serializer.Serialize(eventData);
        await PublishAsync(topic, body, properties, headersArguments);
    }
}