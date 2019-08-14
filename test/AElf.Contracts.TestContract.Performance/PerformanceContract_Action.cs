using System;
using AElf.Sdk.CSharp;
using Google.Protobuf.WellKnownTypes;

namespace AElf.Contracts.TestContract.Performance
{
    public partial class PerformanceContract : PerformanceContractContainer.PerformanceContractBase
    {
        public override Empty InitialPerformanceContract(InitialPerformanceInput input)
        {
            Assert(!State.Initialized.Value, "Already initialized.");
            State.ContractName.Value = input.ContractName;
            State.ContractManager.Value = input.Manager;
            
            return new Empty();
        }

        public override Empty Write1KContentByte(WriteInput input)
        {
            Assert(input.Content.Length == 1000, $"input length not correct, {input.Content.Length}/1k");
            State.Content[Context.Sender] = input.Content.ToHex();
            return new Empty();
        }

        public override Empty Write2KContentByte(WriteInput input)
        {
            Assert(input.Content.Length == 2000, $"input length not correct, {input.Content.Length}/2k");
            State.Content[Context.Sender] = input.Content.ToHex();
            return new Empty();
        }

        public override Empty Write5KContentByte(WriteInput input)
        {
            Assert(input.Content.Length == 5000, $"input length not correct, {input.Content.Length}/5k");
            State.Content[Context.Sender] = input.Content.ToHex();
            return new Empty();
        }
        
        public override Empty Write10KContentByte(WriteInput input)
        {
            Assert(input.Content.Length == 10000, $"input length not correct, {input.Content.Length}/10k");
            State.Content[Context.Sender] = input.Content.ToHex();
            return new Empty();
        }

        public override Empty ComputeLevel1(Empty input)
        {
            var result = CalculateFibonacci(20); // 0ms
            Assert(result==6765, $"ComputeLevel1 calculate error, result: {result}/6765");
            State.MapContent[Context.Sender][20] = result;
            
            return new Empty();
        }

        public override Empty ComputeLevel2(Empty input)
        {
            var result = CalculateFibonacci(24); //1ms
            Assert(result==46368, $"ComputeLevel2 calculate error, result: {result}/46368");
            State.MapContent[Context.Sender][24] = result;
            
            return new Empty();
        }

        public override Empty ComputeLevel3(Empty input)
        {
            var result = CalculateFibonacci(28); //12ms
            Assert(result==317811, $"ComputeLevel3 calculate error, result: {result}/317811");
            State.MapContent[Context.Sender][28] = result;
            
            return new Empty();
        }

        public override Empty ComputeLevel4(Empty input)
        {
            var result = CalculateFibonacci(32); //85ms
            Assert(result==2178309, $"ComputeLevel4 calculate error, result: {result}/2178309");
            State.MapContent[Context.Sender][32] = result;
            
            return new Empty();
        }
        
        private static long CalculateFibonacci(long n)
        {
            if (n == 0 || n == 1)
                return n;

            return CalculateFibonacci(n - 2) + CalculateFibonacci(n - 1);
        }
    }
}