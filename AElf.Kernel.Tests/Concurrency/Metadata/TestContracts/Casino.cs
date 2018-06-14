using System.Linq;
using System.Threading.Tasks;
using AElf.Kernel.Concurrency.Metadata;
using AElf.Kernel.Extensions;
using AElf.Sdk.CSharp;
using AElf.Sdk.CSharp.Types;

namespace AElf.Kernel.Tests.Concurrency.Metadata.TestContracts
{
    /// <summary>
    /// Smart Contract early example for using another contract
    /// </summary>
    public class Casino : CSharpSmartContract
    {
        [SmartContractFieldData("${this}.CasinoToken", DataAccessMode.AccountSpecific)]
        public readonly MapToUInt64<Hash> CasinoToken = new MapToUInt64<Hash>("CasinoToken");

        [SmartContractFieldData("${this}.ExchangeRate", DataAccessMode.ReadOnlyAccountSharing)]
        public ulong ExchangeRate = 100;

        #region Smart contract reference
        
        [SmartContractReference("_tokenContractA", typeof(SimpleTokenContract))]
        private TestTokenContract _tokenContractA;
        [SmartContractReference("_tokenContractB", typeof(SimpleTokenContract))]
        private TestTokenContract _tokenContractB;
        
        #endregion
        
        //To test multiple resource set
        [SmartContractFunction(
            "${this}.BuyTokenFromA(AElf.Kernel.Hash, UInt64)", 
            new []{"${_tokenContractA}.Transfer(AElf.Kernel.Hash, AElf.Kernel.Hash, UInt64)"}, 
            new []{"${this}.CasinoToken", "${this}.ExchangeRate"})]
        public async Task<bool> BuyTokenFromA(Hash from, ulong value)
        {
            if(await _tokenContractA.Transfer(from, Api.GetContractAddress(), value))
            {
                var originBalance = await CasinoToken.GetValueAsync(from);
                originBalance += value * ExchangeRate;
                await CasinoToken.SetValueAsync(from, originBalance);
                return true;
            }
            else
            {
                return false;
            }
        }
        
        //To test in-class function call
        [SmartContractFunction("${this}.BuyTokenFromB(AElf.Kernel.Hash, UInt64)", new []{"${_tokenContractB}.Transfer(AElf.Kernel.Hash, AElf.Kernel.Hash, UInt64)", "${this}.GetExchangeRate()"}, new []{"${this}.CasinoToken"})]
        public async Task<bool> BuyTokenFromB(Hash from, ulong value)
        {
            if(await _tokenContractB.Transfer(from, Api.GetContractAddress(), value))
            {
                var originBalance = await CasinoToken.GetValueAsync(from);
                originBalance += value * GetExchangeRate();
                await CasinoToken.SetValueAsync(from, originBalance);
                return true;
            }
            else
            {
                return false;
            }
        }

        [SmartContractFunction("${this}.GetExchangeRate()", new string[]{}, new []{"${this}.ExchangeRate"})]
        private ulong GetExchangeRate()
        {
            return ExchangeRate;
        }

        public async Task InitializeAsync(IAccountDataProvider dataProvider)
        {
            await CasinoToken.SetValueAsync("0".CalculateHash(), 200);
            await CasinoToken.SetValueAsync("1".CalculateHash(), 100);
        }

        public override async Task InvokeAsync()
        {
            var tx = Api.GetTransaction();
            

            var methodname = tx.MethodName;
            var type = GetType();
            var member = type.GetMethod(methodname);
            // params array
            var parameters = Parameters.Parser.ParseFrom(tx.Params).Params.Select(p => p.Value()).ToArray();
            
            // invoke
            await (Task<object>) member.Invoke(this, parameters);
        }
        
        public Casino(TestTokenContract tokenContractA, TestTokenContract tokenContractB)
        {
            _tokenContractA = tokenContractA;
            _tokenContractB = tokenContractB;
        }
    }
}