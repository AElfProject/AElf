using Acs5;
using AElf.Contracts.MultiToken.Messages;
using Google.Protobuf.WellKnownTypes;

namespace AElf.Contracts.TestContract.ProfitSharing
{
    public class ProfitSharingContract : ProfitSharingContractContainer.ProfitSharingContractBase
    {
        public override Empty InitializeProfitSharingContract(InitializeProfitSharingContractInput input)
        {
            // Create token
            State.TokenContract.Create.Send(new CreateInput
            {
                Symbol = input.Symbol,
                TokenName = "Who cares",
                Issuer = Context.Self,
                Decimals = 2,
                TotalSupply = ProfitSharingContractConstants.TotalSupply
            });
            
            
            
            return new Empty();
        }
    }
}