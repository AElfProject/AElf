using AElf.Kernel;
using AElf.Types;
using Volo.Abp.DependencyInjection;
using Volo.Abp.ObjectMapping;

namespace AElf.WebApp.MessageQueue.Helpers
{
    public interface ITransactionEtoGenerator
    {
        TransactionResultEto GetTransactionEto(IBlock block, TransactionResult txResult, Transaction tx);
    }

    public class TransactionEtoGenerator : ITransactionEtoGenerator, ISingletonDependency
    {
        private readonly IObjectMapper<TransactionResult, TransactionResultEto> _mapper;

        public TransactionEtoGenerator(IObjectMapper<TransactionResult, TransactionResultEto> mapper)
        {
            _mapper = mapper;
        }

        public TransactionResultEto GetTransactionEto(IBlock block, TransactionResult txResult, Transaction tx)
        {
            var txResultEto = _mapper.Map(txResult);
            txResultEto.BlockTime = block.Header.Time.ToDateTime();
            txResultEto.FromAddress = tx.From.ToBase58();
            txResultEto.ToAddress = tx.To.ToBase58();
            return txResultEto;
        }
    }
}