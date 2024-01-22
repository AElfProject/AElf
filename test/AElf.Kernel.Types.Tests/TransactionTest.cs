using AElf.Types;
using Google.Protobuf;
using Shouldly;
using Xunit;

namespace AElf.Kernel.Types.Tests;

public class TransactionTest
{
    public class TransactionTraceTest
    {
        [Fact]
        public void Transaction_Test()
        {
            var transaction = new Transaction()
            {
                From = Address.FromBase58("z1NVbziJbekvcza3Zr4Gt4eAvoPBZThB68LHRQftrVFwjtGVM"),
                To = Address.FromBase58("2vNDCj1WjNLAXm3VnEeGGRMw3Aab4amVSEaYmCyxQKjNhLhfL7"),
                MethodName = "test",
                Params = ByteString.Empty,
                RefBlockNumber = 1000,
                RefBlockPrefix = ByteString.Empty
            };
            var newUnifiedTransaction = UnifiedTransaction.Parser.ParseFrom(transaction.ToByteString());
            newUnifiedTransaction.RefBlockNumber.ShouldBe(transaction.RefBlockNumber);
            newUnifiedTransaction.RefBlockPrefix.ShouldBe(transaction.RefBlockPrefix);
            newUnifiedTransaction.From.ShouldBe(transaction.From);
            newUnifiedTransaction.To.ShouldBe(transaction.To);
            newUnifiedTransaction.MethodName.ShouldBe(transaction.MethodName);
            newUnifiedTransaction.Params.ShouldBe(transaction.Params);
            newUnifiedTransaction.GetHash().ShouldBe(transaction.GetHash());
            var inlineTransaction = new InlineTransaction()
            {
                From = Address.FromBase58("z1NVbziJbekvcza3Zr4Gt4eAvoPBZThB68LHRQftrVFwjtGVM"),
                To = Address.FromBase58("2vNDCj1WjNLAXm3VnEeGGRMw3Aab4amVSEaYmCyxQKjNhLhfL7"),
                MethodName = "test",
                Params = ByteString.Empty,
                OriginTransactionId =
                    Hash.LoadFromHex("f7d4395083e4072c04d919dd3c2b3ee38901fa0c75cf40c34d7f8ab1a12e1261"),
            };
            var newTransaction = UnifiedTransaction.Parser.ParseFrom(inlineTransaction.ToByteString());
            newTransaction.OriginTransactionId.ShouldBe(inlineTransaction.OriginTransactionId);
            newTransaction.Index.ShouldBe(inlineTransaction.Index);
            newTransaction.From.ShouldBe(inlineTransaction.From);
            newTransaction.To.ShouldBe(inlineTransaction.To);
            newTransaction.MethodName.ShouldBe(inlineTransaction.MethodName);
            newTransaction.Params.ShouldBe(inlineTransaction.Params);
            newTransaction.GetHash().ShouldBe(inlineTransaction.GetHash());
        }
    }
}