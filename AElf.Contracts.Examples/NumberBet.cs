using System;
using System.Linq;
using System.Threading.Tasks;
using AElf.Api.CSharp;
using AElf.Kernel;
using AElf.Kernel.Concurrency.Metadata;
using AElf.Kernel.Extensions;
using Org.BouncyCastle.Security;
using CSharpSmartContract = AElf.Api.CSharp.CSharpSmartContract;

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

        private SimpleTokenContract tokenContract;
        
        

        [SmartContractFunction("BuyToken", new []{"MainChain.SimpleTokenContract.Transfer(string, string, ulong)"}, new []{"CasinoToken", "ExchangeRate"})]
        public async Task<bool> BuyToken(Hash from, ulong value)
        {
            if (GetContractByName("MainChain.AElf", out var aelf))
            {
                if(await tokenContract.Transfer(from, Api.CSharp.Api.GetContractAddress(), value))
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
            else
            {
                //should have a exception of "SmartContractNotFound"
                throw new InvalidParameterException();
            }
        }

        public override async Task InitializeAsync(IAccountDataProvider dataProvider)
        {
            await CasinoToken.SetValueAsync("0".CalculateHash(), ((ulong)200).ToBytes());
            await CasinoToken.SetValueAsync("1".CalculateHash(), ((ulong)100).ToBytes());
        }

        public override async Task<object> InvokeAsync(SmartContractInvokeContext context)
        {
            var methodname = context.MethodName;
            var type = GetType();
            var member = type.GetMethod(methodname);
            // params array
            var parameters = Parameters.Parser.ParseFrom(context.Params).Params.Select(p => p.Value()).ToArray();
            
            // invoke
            return await (Task<object>) member.Invoke(this, parameters);
        }

        /// <summary>
        /// Dummy function for displaying metadata
        /// </summary>
        /// <param name="contractFullName"></param>
        /// <returns></returns>
        public bool GetContractByName(string contractFullName, out CSharpSmartContract contract)
        {
            throw new NotImplementedException();
        }
    }
}