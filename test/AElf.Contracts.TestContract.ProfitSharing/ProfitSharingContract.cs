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

        public override Empty CreateProfitItem(CreateProfitItemInput input)
        {
            State.ProfitContract.CreateTreasuryProfitItem.Send(new Profit.CreateProfitItemInput
            {
                
            });
            return new Empty();
        }

        public override Empty SetProfitReceivers(ProfitReceivers input)
        {
            return new Empty();
        }
        
        
    }
}