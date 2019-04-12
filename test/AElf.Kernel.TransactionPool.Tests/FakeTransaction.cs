using AElf.Common;
using Google.Protobuf;

namespace AElf.Kernel.TransactionPool
{
    public static class FakeTransaction
    {
        public static Transaction Generate()
        {
            var transaction = new Transaction()
            {
                From = Address.Generate(),
                To = Address.Generate(),
                MethodName = "test",
                Params = ByteString.CopyFromUtf8("test")
            };

            return transaction;
        }
    }
}