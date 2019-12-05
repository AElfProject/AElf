using AElf.Types;
using Google.Protobuf.WellKnownTypes;

namespace AElf.Contracts.TestContract.Performance
{
    public partial class PerformanceContract
    {
        public override StringValue GetContractName(Empty input)
        {
            return new StringValue
            {
                Value = nameof(PerformanceContract)
            };
        }

        public override ReadOutput QueryReadInfo(Address input)
        {
            if(State.Content[input] == null)
                return new ReadOutput();
            
            return new ReadOutput
            {
                Content = State.Content[input]
            };
        }

        public override NumberOutput QueryFibonacci(NumberInput input)
        {
            Assert(input.Number < 50, $"Query number should less than 50. Actual number: {input.Number}");
            
            var result = CalculateFibonacci(input.Number);
            
            return new NumberOutput
            {
                Number = input.Number,
                Result = result
            };
        }
    }
}