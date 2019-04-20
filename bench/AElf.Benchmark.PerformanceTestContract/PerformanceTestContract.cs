using System;
using Google.Protobuf.WellKnownTypes;

namespace AElf.Benchmark.PerformanceTestContract
{
    public class PerformanceTestContract : PerformanceTestContractContainer.PerformanceTestContractBase
    {
        public override UInt64Value Fibonacci(UInt64Value input)
        {
            var result = CalculateFibonacci(input.Value);
            return new UInt64Value {Value = result};
        }

        private ulong CalculateFibonacci(ulong n)
        {
            if (n == 0 || n == 1)
                return n;
            return CalculateFibonacci(n - 1) + CalculateFibonacci(n - 2);
        }

        public override Int32Value Nop(PerformanceTesteInput input)
        {
            for (uint i = 0; i < input.N; i++)
            {
                
            }

            return new Int32Value {Value = input.Seed};
        }
    }
}