using System.Threading.Tasks;
using AElf.Kernel;
using AElf.OS;
using AElf.Types;
using BenchmarkDotNet.Attributes;

namespace AElf.Benchmark
{
    [MarkdownExporterAttribute.GitHub]
    public class TransactionVerifySignatureTests: BenchmarkTestBase
    {
        private OSTestHelper _osTestHelper;
        
        private Transaction _transaction;

        [GlobalSetup]
        public async Task GlobalSetup()
        {
            _osTestHelper = GetRequiredService<OSTestHelper>();
            
            _transaction = await _osTestHelper.GenerateTransferTransaction();
        }

        [Benchmark]
        public async Task VerifySignatureTest()
        {
            _transaction.VerifySignature();
        }
    }
}