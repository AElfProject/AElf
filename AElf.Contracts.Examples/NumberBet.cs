using System;
using System.Linq;
using System.Threading.Tasks;
using AElf.Kernel;
using AElf.Kernel.Concurrency.Metadata;
using AElf.Kernel.Extensions;
using AElf.Sdk.CSharp;
using AElf.Sdk.CSharp.Types;
using Org.BouncyCastle.Security;

namespace AElf.Contracts.Examples
{
    /// <summary>
    /// Smart Contract early example for using another contract
    /// </summary>
    public class Casino : CSharpSmartContract
    {
        [SmartContractFieldData("CasinoToken", DataAccessMode.AccountSpecific)]
        public readonly Map CasinoToken = new Map("CasinoToken");

        [SmartContractFieldData("ExchangeRate", DataAccessMode.ReadOnlyAccountSharing)]
        public ulong ExchangeRate = 100;

        #region Smart contract reference
        
        [SmartContractReference("_tokenContractA", "SimpleTokenContract")]
        private SimpleTokenContract _tokenContractA;
        [SmartContractReference("_tokenContractB", "SimpleTokenContract")]
        private SimpleTokenContract _tokenContractB;
        
        #endregion

        public Casino(SimpleTokenContract tokenContractA, SimpleTokenContract tokenContractB)
        {
            _tokenContractA = tokenContractA;
            _tokenContractB = tokenContractB;
        }
        
        [SmartContractFunction("BuyTokenFromA(Hash, ulong)", new []{"${ChainId}.${_tokenContractA}.Transfer(Hash, Hash, ulong)"}, new []{"CasinoToken", "ExchangeRate"})]
        public async Task<bool> BuyTokenFromA(Hash from, ulong value)
        {
            if(await _tokenContractA.Transfer(from, Api.GetContractAddress(), value))
            {
                var originBalance = (await CasinoToken.GetValue(from)).ToUInt64();
                originBalance += value * ExchangeRate;
                await CasinoToken.SetValueAsync(from, originBalance.ToBytes());
                return true;
            }
            else
            {
                return false;
            }
        }
        
        [SmartContractFunction("BuyTokenFromB(Hash, ulong)", new []{"${ChainId}.${_tokenContractB}.Transfer(Hash, Hash, ulong)"}, new []{"CasinoToken", "ExchangeRate"})]
        public async Task<bool> BuyTokenFromB(Hash from, ulong value)
        {
            if(await _tokenContractB.Transfer(from, Api.GetContractAddress(), value))
            {
                var originBalance = (await CasinoToken.GetValue(from)).ToUInt64();
                originBalance += value * ExchangeRate;
                await CasinoToken.SetValueAsync(from, originBalance.ToBytes());
                return true;
            }
            else
            {
                return false;
            }
        }

        public async Task InitializeAsync(IAccountDataProvider dataProvider)
        {
            await CasinoToken.SetValueAsync("0".CalculateHash(), ((ulong)200).ToBytes());
            await CasinoToken.SetValueAsync("1".CalculateHash(), ((ulong)100).ToBytes());
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
    }
}