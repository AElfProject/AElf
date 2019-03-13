using AElf.Sdk.CSharp;

namespace AElf.Runtime.CSharp3.Tests.TestContract
{
    public class ContractApi : TestContractContainer.TestContractBase
    {
        public override IncrementOutput Increment(IncrementInput request)
        {
            State.Counter.Value = State.Counter.Value.Add(request.Value);
            return new IncrementOutput()
            {
                Value = State.Counter.Value
            };
        }
    }
}