using System.Threading.Tasks;
using AElf.Kernel;

namespace AElf.ChainController.TxMemPool
{
//    public enum TxInsertionAndBroadcastingError
//    {
//        Success = 0,
//        AlreadyInserted,
//        Valid,
//        WrongTransactionType,
//        InvalidTxFormat,
//        NotEnoughGas,
//        TooBigSize,
//        WrongAddress,
//        InvalidSignature,
//        PoolClosed,
//        BroadCastFailed,
//        Failed,
//        AlreadyExecuted
//    }

    public interface ITxValidator
    {
        TxValidation.TxInsertionAndBroadcastingError ValidateTx(Transaction tx);
        Task<TxValidation.TxInsertionAndBroadcastingError> ValidateReferenceBlockAsync(Transaction tx);
    }
}