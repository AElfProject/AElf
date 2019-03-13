namespace AElf.Runtime.CSharp3.Tests.TestContract
{
    public class ContractApi : TokenContract.TokenContractBase
    {
        public override EchoOutput Echo(EchoInput request)
        {
            return new EchoOutput()
            {
                Value = request.Value
            };
        }
    }
}