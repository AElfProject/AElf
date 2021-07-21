using System.Threading.Tasks;
using Volo.Abp.BackgroundJobs;

namespace AElf.WebApp.MessageQueue
{
    public class TransactionResultListEtoHandler : IAsyncBackgroundJob<TransactionResultListEto>
    {
        public Task ExecuteAsync(TransactionResultListEto args)
        {
            throw new System.NotImplementedException();
        }
    }
}