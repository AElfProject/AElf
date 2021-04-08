using System.Threading.Tasks;
using AElf.Kernel;
using AElf.OS;
using AElf.Types;
using BenchmarkDotNet.Attributes;

namespace AElf.Benchmark
{
    [MarkdownExporterAttribute.GitHub]
    public class TransactionVerifySignatureTests : BenchmarkTestBase
    {
        private BenchmarkHelper _benchmarkHelper;

        private Transaction _transaction;

        [GlobalSetup]
        public async Task GlobalSetup()
        {
            _benchmarkHelper = GetRequiredService<BenchmarkHelper>();

            _transaction = await _benchmarkHelper.GenerateTransferTransaction();
        }

        [Benchmark]
        public void VerifySignatureTest()
        {
            _transaction.VerifySignature();
        }
    }
}