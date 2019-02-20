//using AElf.SmartContract;
//using AElf.Sdk.CSharp;
//using AElf.Sdk.CSharp.Types;
//using AElf.Types.CSharp.MetadataAttribute;
//using AElf.Common;

namespace AElf.Kernel.Tests.Concurrency.Metadata.TestContracts
{
    /*
    /// <summary>
    /// Smart Contract early example for using another contract
    /// </summary>
    public class Casino : CSharpSmartContract
    {
        [SmartContractFieldData("${this}.CasinoToken", DataAccessMode.AccountSpecific)]
        public readonly MapToUInt64<Address> CasinoToken = new MapToUInt64<Address>("CasinoToken");

        [SmartContractFieldData("${this}.ExchangeRate", DataAccessMode.ReadOnlyAccountSharing)]
        public ulong ExchangeRate = 100;

        #region Smart contract reference
        
        [SmartContractReference("_tokenContractA", "0x123")]
        private TestTokenContract _tokenContractA;
        [SmartContractReference("_tokenContractB", "0X456")]
        private TestTokenContract _tokenContractB;
        
        #endregion
        
        //To test multiple resource set
        [SmartContractFunction(
            "${this}.BuyTokenFromA(AElf.Kernel.Hash, UInt64)", 
            new []{"${_tokenContractA}.Transfer(AElf.Kernel.Hash, AElf.Kernel.Hash, UInt64)"}, 
            new []{"${this}.CasinoToken", "${this}.ExchangeRate"})]
        public bool BuyTokenFromA(Address from, ulong value)
        {
            if(_tokenContractA.Transfer(from, Api.GetContractAddress(), value))
            {
                var originBalance = CasinoToken.GetValue(from);
                originBalance += value * ExchangeRate;
                CasinoToken.SetValue(from, originBalance);
                return true;
            }
            else
            {
                return false;
            }
        }
        
        //To test in-class function call
        [SmartContractFunction("${this}.BuyTokenFromB(AElf.Kernel.Hash, UInt64)", new []{"${_tokenContractB}.Transfer(AElf.Kernel.Hash, AElf.Kernel.Hash, UInt64)", "${this}.GetExchangeRate()"}, new []{"${this}.CasinoToken"})]
        public bool BuyTokenFromB(Address from, ulong value)
        {
            if(_tokenContractB.Transfer(from, Api.GetContractAddress(), value))
            {
                var originBalance = CasinoToken.GetValue(from);
                originBalance += value * GetExchangeRate();
                CasinoToken.SetValue(from, originBalance);
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

        public void Initialize(IAccountDataProvider dataProvider)
        {
            CasinoToken.SetValue(Address.FromString("0"), 200);
            CasinoToken.SetValue(Address.FromString("1"), 100);
        }

//        public override async Task InvokeAsync()
//        {
//            var tx = Api.GetTransaction();
//            
//
//            var methodname = tx.MethodName;
//            var type = GetType();
//            var member = type.GetMethod(methodname);
//            // params array
//            var parameters = Parameters.Parser.ParseFrom(tx.Params).Params.Select(p => p.Value()).ToArray();
//            
//            // invoke
//            await (Task<object>) member.Invoke(this, parameters);
//        }
        
        public Casino(TestTokenContract tokenContractA, TestTokenContract tokenContractB)
        {
            _tokenContractA = tokenContractA;
            _tokenContractB = tokenContractB;
        }
    }
     */
}