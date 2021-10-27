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
                From = SampleAddress.AddressList[0],
                To = SampleAddress.AddressList[1],
                MethodName = "test",
                Params = ByteString.CopyFromUtf8("test")
            };

            return transaction;
        }
    }
}