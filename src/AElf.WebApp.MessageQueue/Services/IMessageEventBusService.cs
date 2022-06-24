using System.Collections.Generic;
using System.Threading.Tasks;
using Volo.Abp.DependencyInjection;
using Volo.Abp.EventBus.Distributed;

namespace AElf.WebApp.MessageQueue.Services
{
    public interface IMessageEventBusService
    {
        Task PublishMessageAsync(string topic, List<TransactionResultListEto> messages);
        Task PublishMessageAsync(string topic, TransactionResultListEto messages);
    }

    public class MessageEventBusService : IMessageEventBusService, ISingletonDependency
    {
        private readonly IDistributedEventBus _distributedEventBus;

        public MessageEventBusService(IDistributedEventBus distributedEventBus)
        {
            _distributedEventBus = distributedEventBus;
        }

        public Task PublishMessageAsync(string topic, List<TransactionResultListEto> messages)
        {
            throw new System.NotImplementedException();
        }

        public Task PublishMessageAsync(string topic, TransactionResultListEto messages)
        {
            throw new System.NotImplementedException();
        }
    }
}