using System.Threading.Tasks;
using AElf.Kernel.Blockchain.Application;
using AElf.Kernel.TransactionPool.Infrastructure;
using AElf.Types;
using Volo.Abp.DependencyInjection;

namespace AElf.WebApp.Application.Chain
{
    /// <summary>
    /// Here used the public access modifier for ITransactionResultExpands
    /// Why?
    /// the access modifier for ITransactionResultExpands should follow ITransactionResultAppService
    /// because ITransactionResultAppService depends on ITransactionResultExpands
    /// if you want create an ITransactionResultAppService entity , you also need to create a ITransactionResultExpands entity
    /// </summary>
    public interface ITransactionResultExpands
    {
        Task<TransactionResult> GetTransactionResultAsync(Hash txHash);
    }

    public class TransactionResultExpands : ITransactionResultExpands, ISingletonDependency
    {
        private readonly ITxHub _txHub;
        private readonly ITransactionResultQueryService _transactionResultQueryService;


        public TransactionResultExpands(ITxHub txHub, ITransactionResultQueryService transactionResultQueryService)
        {
            _txHub = txHub;
            _transactionResultQueryService = transactionResultQueryService;
        }


        public async Task<TransactionResult> GetTransactionResultAsync(Hash txHash)
        {
            // in storage
            var res = await _transactionResultQueryService.GetTransactionResultAsync(txHash);
            if (res != null)
            {
                return res;
            }

            // in tx pool
            var receipt = await _txHub.GetTransactionReceiptAsync(txHash);
            if (receipt != null)
            {
                return new TransactionResult
                {
                    TransactionId = receipt.TransactionId,
                    Status = TransactionResultStatus.Pending
                };
            }

            // not existed
            return new TransactionResult
            {
                TransactionId = txHash,
                Status = TransactionResultStatus.NotExisted
            };
        }
    }
}