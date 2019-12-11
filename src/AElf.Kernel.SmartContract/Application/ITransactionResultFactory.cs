using AElf.Types;

namespace AElf.Kernel.SmartContract.Application
{
    interface ITransactionResultFactory
    {
        TransactionResult GetTransactionResult(TransactionTrace trace, long blockHeight);
    }

    class UnexecutableTxResultFactory : ITransactionResultFactory
    {
        public TransactionResult GetTransactionResult(TransactionTrace trace, long blockHeight)
        {
            return new TransactionResult
            {
                TransactionId = trace.TransactionId,
                Status = TransactionResultStatus.Unexecutable,
                BlockNumber = blockHeight,
                Error = ExecutionStatus.Undefined.ToString()
            };
        }
    }

    class PreFailedTxResultFactory : ITransactionResultFactory
    {
        public TransactionResult GetTransactionResult(TransactionTrace trace, long blockHeight)
        {
            if (trace.TransactionFee != null && trace.TransactionFee.IsFailedToCharge)
            {
                return new TransactionResult
                {
                    TransactionId = trace.TransactionId,
                    Status = TransactionResultStatus.Failed,
                    ReturnValue = trace.ReturnValue,
                    ReadableReturnValue = trace.ReadableReturnValue,
                    BlockNumber = blockHeight,
                    Logs = {trace.FlattenedLogs},
                    Error = ExecutionStatus.InsufficientTransactionFees.ToString()
                };
            }

            return new TransactionResult
            {
                TransactionId = trace.TransactionId,
                Status = TransactionResultStatus.Unexecutable,
                BlockNumber = blockHeight,
                Error = trace.Error
            };
        }
    }

    class MinedTxResultFactory : ITransactionResultFactory
    {
        public TransactionResult GetTransactionResult(TransactionTrace trace, long blockHeight)
        {
            var txRes = new TransactionResult
            {
                TransactionId = trace.TransactionId,
                Status = TransactionResultStatus.Mined,
                ReturnValue = trace.ReturnValue,
                ReadableReturnValue = trace.ReadableReturnValue,
                BlockNumber = blockHeight,
                //StateHash = trace.GetSummarizedStateHash(),
                Logs = {trace.FlattenedLogs}
            };

            txRes.UpdateBloom();

            return txRes;
        }
    }

    class FailedTxResultFactory : ITransactionResultFactory
    {
        public TransactionResult GetTransactionResult(TransactionTrace trace, long blockHeight)
        {
            return new TransactionResult
            {
                TransactionId = trace.TransactionId,
                Status = TransactionResultStatus.Failed,
                BlockNumber = blockHeight,
                Error = trace.Error
            };
        }
    }
}