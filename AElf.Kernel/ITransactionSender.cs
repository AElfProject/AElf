namespace AElf.Kernel
{
    public interface ITransactionSender
    {
        /// <summary>
        /// Check the compliance of the transaction.
        /// </summary>
        /// <returns>Whether</returns>
        bool VerifyTransaction(ITransaction transaction);

        /// <summary>
        /// Broadcast a transanction.
        /// </summary>
        void BroadcastTransanction(ITransaction transaction);
    }
}