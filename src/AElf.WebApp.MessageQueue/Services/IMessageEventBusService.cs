using System.Threading.Tasks;
using AElf.WebApp.MessageQueue.EventBus;
using Volo.Abp.DependencyInjection;

namespace AElf.WebApp.MessageQueue.Services
{
    public interface IMessageEventBusService
    {
        Task PublishMessageAsync(string topic, TransactionResultListEto messages);
    }

    public class MessageEventBusService : IMessageEventBusService, ISingletonDependency
    {
        private readonly ITopicRabbitMqDistributedEventBus _topicRabbitMqDistributedEventBus;

        public MessageEventBusService(ITopicRabbitMqDistributedEventBus topicRabbitMqDistributedEventBus)
        {
            _topicRabbitMqDistributedEventBus = topicRabbitMqDistributedEventBus;
        }

        public async Task PublishMessageAsync(string topic, TransactionResultListEto messages)
        {
            await _topicRabbitMqDistributedEventBus.PublishTopicMessageAsync(topic, messages);
        }
    }
}