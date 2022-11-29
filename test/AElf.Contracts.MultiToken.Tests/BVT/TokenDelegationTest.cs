using System.Linq;
using System.Threading.Tasks;
using AElf.Types;
using Google.Protobuf;
using Google.Protobuf.WellKnownTypes;
using Shouldly;
using Xunit;
using AElf.Contracts.MultiToken;

namespace AElf.Contracts.MultiToken;

public partial class MultiTokenContractTests
{
    [Fact(DisplayName = "set Delegation test")]
    public async Task SetTokenDelegation_Test1()
    {
        CreateBaseNativeTokenAsync();
        var Testdata = 100;
        await TokenContractStub.SetTransactionFeeDelegations.SendAsync(new SetTransactionFeeDelegationsInput()
        {
            DelegatorAddress = User1Address,
            Delegations =
            {
                Keys = {"ELF"},
                Values = {Testdata}
            }
        });
        
        var delegateAllowance = await TokenContractStub.GetDelegatorAllowance.CallAsync(new GetDelegatorAllowanceInput()
        {
           DelegateeAddress = User1Address,
           DelegatorAddress = DefaultAddress
        });
       delegateAllowance.Delegations["ELF"].ShouldBe(Testdata);
       
       //set Delegation normally
       Testdata = 50;
       await TokenContractStub.SetTransactionFeeDelegations.SendAsync(new SetTransactionFeeDelegationsInput()
       {
           DelegatorAddress = User1Address,
           Delegations =
           {
               Keys = {"ELF"},
               Values = {Testdata}
           }
       });
       delegateAllowance = await TokenContractStub.GetDelegatorAllowance.CallAsync(new GetDelegatorAllowanceInput()
       {
           DelegateeAddress = User1Address,
           DelegatorAddress = DefaultAddress
       });
       delegateAllowance.Delegations["ELF"].ShouldBe(Testdata);
       
       //remove the userdelegation
       Testdata = 0;
       await TokenContractStub.SetTransactionFeeDelegations.SendAsync(new SetTransactionFeeDelegationsInput()
       {
           DelegatorAddress = User1Address,
           Delegations =
           {
               Keys = {"ELF"},
               Values = {Testdata}
           }
       });
        delegateAllowance = await TokenContractStub.GetDelegatorAllowance.CallAsync(new GetDelegatorAllowanceInput()
       {
           DelegateeAddress = User1Address,
           DelegatorAddress = DefaultAddress
       });
       delegateAllowance.Delegations.ShouldBeNull();
       
    }

    
    [Fact(DisplayName = "set Delegation test")]
    public async Task SetTokenDelegation_Test2()
    {
        CreateBaseNativeTokenAsync();
        await TokenContractStub.SetTransactionFeeDelegations.SendAsync(new SetTransactionFeeDelegationsInput()
        {
            DelegatorAddress = User1Address,
            Delegations =
            {
                Keys = {"ELF"},
                Values = {100}
            }
        });

        var delegateAllowance = await TokenContractStub.GetDelegatorAllowance.CallAsync(new GetDelegatorAllowanceInput()
        {
            DelegateeAddress = User1Address,
            DelegatorAddress = DefaultAddress
        });
        delegateAllowance.Delegations["ELF"].ShouldBe(100);
    }
    private async Task CreateBaseNativeTokenAsync()
    {
        await TokenContractStub.Create.SendAsync(new CreateInput
        {
            Symbol = NativeTokenInfo.Symbol,
            TokenName = NativeTokenInfo.TokenName,
            TotalSupply = NativeTokenInfo.TotalSupply,
            Decimals = NativeTokenInfo.Decimals,
            Issuer = NativeTokenInfo.Issuer,
            IsBurnable = NativeTokenInfo.IsBurnable,
          
        });
    }
}