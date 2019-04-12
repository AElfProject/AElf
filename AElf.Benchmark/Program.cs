using BenchmarkDotNet.Configs;
using BenchmarkDotNet.Running;
using Volo.Abp;

namespace AElf.Benchmark
{
    class Program
    {
        static void Main(string[] args)
        {
            using (var application = AbpApplicationFactory.Create<BenchmarkAElfModule>(options =>
            {
                options.UseAutofac();
            }))
            {
                application.Initialize();

#if DEBUG
                BenchmarkSwitcher.FromAssembly(typeof(Program).Assembly).Run(args, new DebugInProcessConfig());          
#else
                BenchmarkRunner.Run<TransactionVerifySignatureTests>();
                BenchmarkRunner.Run<TxHubHandleBestChainFoundTests>();
                BenchmarkRunner.Run<TxHubTransactionsReceiveTests>();
                BenchmarkRunner.Run<BlockAttachTests>();
                BenchmarkRunner.Run<BlockchainStateMergingTests>();
                BenchmarkRunner.Run<BlockExecutingTests>();
                BenchmarkRunner.Run<MinerTests>();

#endif
            }
        }
    }
}