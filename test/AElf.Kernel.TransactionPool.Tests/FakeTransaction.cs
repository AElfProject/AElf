using AElf.Types;
using Google.Protobuf;

namespace AElf.Kernel.TransactionPool
{
    public static class FakeTransaction
    {
        public static Transaction Generate()
        {
            var transaction = new Transaction()
            {
                From = AddressHelper.StringToAddress("from"),
                To = AddressHelper.StringToAddress("to"),
                MethodName = "test",
                Params = ByteString.CopyFromUtf8("test")
            };

            return transaction;
        }
    }
}