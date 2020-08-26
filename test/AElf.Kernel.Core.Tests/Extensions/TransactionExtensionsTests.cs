using System;
using System.Linq;
using Google.Protobuf;
using Shouldly;
using Xunit;

namespace AElf.Kernel
{
    public class TransactionExtensionsTests : AElfKernelTestBase
    {
        private readonly KernelTestHelper _kernelTestHelper;
        
        public TransactionExtensionsTests()
        {
            _kernelTestHelper = GetRequiredService<KernelTestHelper>();
        }

        [Fact]
        public void Size_Test()
        {
            var transaction = _kernelTestHelper.GenerateTransaction();
            transaction.Size().ShouldBe(transaction.CalculateSize());
        }

        [Fact]
        public void VerifySignature_Test()
        {
            var transaction = _kernelTestHelper.GenerateTransaction();
            transaction.MethodName = string.Empty;
            transaction.VerifySignature().ShouldBeFalse();
            
            transaction = _kernelTestHelper.GenerateTransaction();
            transaction.Signature = ByteString.Empty;
            transaction.VerifySignature().ShouldBeFalse();
            
            transaction = _kernelTestHelper.GenerateTransaction();
            transaction.From = SampleAddress.AddressList.Last();
            transaction.VerifySignature().ShouldBeFalse();
            
            transaction = _kernelTestHelper.GenerateTransaction();
            transaction.VerifySignature().ShouldBeTrue();
        }

        [Fact]
        public void VerifyExpiration_Test()
        {
            var transaction = _kernelTestHelper.GenerateTransaction(100);
            transaction.VerifyExpiration(99).ShouldBeFalse();
            transaction.VerifyExpiration(100).ShouldBeTrue();
            transaction.VerifyExpiration(612).ShouldBeFalse();
        }
    }
}