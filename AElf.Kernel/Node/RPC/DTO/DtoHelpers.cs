using System.Security.Cryptography;
using AElf.Kernel;

namespace AElf.Node.RPC.DTO
{
    public static class DtoHelper
    {
        public static TransactionDto ToTransactionDto(this ITransaction tx)
        {
            TransactionDto dto = new TransactionDto()
            {
                From = tx.From.GetHashBytes(),
                To = tx.To.GetHashBytes()
            };

            return dto;
        }
    }
}