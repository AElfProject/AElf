using System;
using System.Linq;
using AElf.Kernel;
using AElf.Sdk.CSharp;
using AElf.Sdk.CSharp.Types;
using AElf.Types.CSharp.MetadataAttribute;
using Api = AElf.Sdk.CSharp.Api;
using AElf.Common;

#pragma warning disable CS0169,CS0649

// ReSharper disable InconsistentNaming
// ReSharper disable UnusedMember.Global
namespace AElf.Contracts.Token
{
    #region Events

    public class Transfered : Event
    {
        [Indexed] public Address From { get; set; }
        [Indexed] public Address To { get; set; }
        [Indexed] public ulong Amount { get; set; }
    }

    public class Approved : Event
    {
        [Indexed] public Address Owner { get; set; }
        [Indexed] public Address Spender { get; set; }
        [Indexed] public ulong Amount { get; set; }
    }


    public class UnApproved : Event
    {
        [Indexed] public Address Owner { get; set; }
        [Indexed] public Address Spender { get; set; }
        [Indexed] public ulong Amount { get; set; }
    }

    public class Burned : Event
    {
        public Address Burner { get; set; }
        public ulong Amount { get; set; }
    }

    #endregion Events

    public class TokenContract : CSharpSmartContract
    {
        private Address ConsensusContractAddress;

        [SmartContractFieldData("${this}._initialized", DataAccessMode.ReadWriteAccountSharing)]
        private readonly BoolField _initialized = new BoolField("_Initialized_");

        [SmartContractFieldData("${this}._symbol", DataAccessMode.ReadOnlyAccountSharing)]
        private readonly StringField _symbol = new StringField("_Symbol_");

        [SmartContractFieldData("${this}._tokenName", DataAccessMode.ReadOnlyAccountSharing)]
        private readonly StringField _tokenName = new StringField("_TokenName_");

        [SmartContractFieldData("${this}._totalSupply", DataAccessMode.ReadOnlyAccountSharing)]
        private readonly UInt64Field _totalSupply = new UInt64Field("_TotalSupply_");

        [SmartContractFieldData("${this}._decimals", DataAccessMode.ReadOnlyAccountSharing)]
        private readonly UInt32Field _decimals = new UInt32Field("_Decimals_");

        [SmartContractFieldData("${this}._balances", DataAccessMode.AccountSpecific)]
        private readonly MapToUInt64<Address> _balances = new MapToUInt64<Address>("_Balances_");

        [SmartContractFieldData("${this}._allowancePlaceHolder", DataAccessMode.AccountSpecific)]
        private readonly object _allowancePlaceHolder;

        private readonly MapToUInt64<Address> _chargedFees = new MapToUInt64<Address>("_ChargedFees_");
        private readonly PbField<Address> _feePoolAddress = new PbField<Address>("_feePoolAddress_");

        #region ABI (Public) Methods

        #region View Only Methods

        [SmartContractFunction("${this}.Symbol", new string[] { }, new[] {"${this}._symbol"})]
        [View]
        public string Symbol()
        {
            return _symbol.GetValue();
        }

        [SmartContractFunction("${this}.TokenName", new string[] { }, new[] {"${this}._tokenName"})]
        [View]
        public string TokenName()
        {
            return _tokenName.GetValue();
        }

        [SmartContractFunction("${this}.TotalSupply", new string[] { }, new[] {"${this}._totalSupply"})]
        [View]
        public ulong TotalSupply()
        {
            return _totalSupply.GetValue();
        }

        [SmartContractFunction("${this}.Decimals", new string[] { }, new[] {"${this}._decimals"})]
        [View]
        public uint Decimals()
        {
            return _decimals.GetValue();
        }

        [SmartContractFunction("${this}.BalanceOf", new string[] { }, new[] {"${this}._balances"})]
        [View]
        public ulong BalanceOf(Address owner)
        {
            return _balances[owner];
        }

        [SmartContractFunction("${this}.Allowance", new string[] { }, new[] {"${this}._allowancePlaceHolder"})]
        [View]
        public ulong Allowance(Address owner, Address spender)
        {
            return Allowances.GetAllowance(owner, spender);
        }

        [View]
        public ulong ChargedFees(Address address)
        {
            return _chargedFees[address];
        }

        [View]
        public Address FeePoolAddress()
        {
            return _feePoolAddress.GetValue();
        }
        
        #endregion View Only Methods

        #region Actions

        [SmartContractFunction("${this}.Initialize", new string[] { }, new[]
        {
            "${this}._initialized", "${this}._symbol", "${this}._tokenName", "${this}._totalSupply",
            "${this}._decimals", "${this}._balances"
        })]
        [Fee(0)]
        public void Initialize(string symbol, string tokenName, ulong totalSupply, uint decimals)
        {
            Api.Assert(!_initialized.GetValue(), "Already initialized.");
            // Api.Assert(Api.GetContractOwner().Equals(Api.GetFromAddress()), "Only owner can initialize the contract state.");
            _symbol.SetValue(symbol);
            _tokenName.SetValue(tokenName);
            _totalSupply.SetValue(totalSupply);
            _decimals.SetValue(decimals);
            _balances[Api.GetFromAddress()] = (ulong) (totalSupply * (1 - GlobalConfig.DividendsRatio)) - GlobalConfig.BalanceForInitialization;
            // Give a specific amount of tokens to Dividends Contract for sending dividends to both candidates and voters.
            _balances[Api.DividendsContractAddress] = (ulong) (totalSupply * GlobalConfig.DividendsRatio);
            _balances[Api.ConsensusContractAddress] = GlobalConfig.BalanceForInitialization;
            _initialized.SetValue(true);
        }

        [Fee(0)]
        public void SetFeePoolAddress(Address address)
        {
            var fromAddress = Api.GetFromAddress();
            var notSet = _feePoolAddress.GetValue().Value == null || _feePoolAddress.GetValue().Value.Length == 0;
            Api.Assert(notSet || fromAddress == _feePoolAddress.GetValue(), "Not allowed to perform this action.");
            _feePoolAddress.SetValue(address);
        }

        [SmartContractFunction("${this}.Transfer", new string[] {"${this}.DoTransfer"}, new string[] { })]
        public void Transfer(Address to, ulong amount)
        {
            var from = Api.GetFromAddress();
            DoTransfer(from, to, amount);
            //Console.WriteLine($"Transferred {amount} tokens to - {to.GetFormatted()}");
        }

        [SmartContractFunction("${this}.TransferFrom", new[] {"${this}.DoTransfer"},
            new[] {"${this}._allowancePlaceHolder"})]
        public void TransferFrom(Address from, Address to, ulong amount)
        {
            var allowance = Allowances.GetAllowance(from, Api.GetFromAddress());
            Api.Assert(allowance >= amount, "Insufficient allowance.");

            DoTransfer(from, to, amount);
            Allowances.Reduce(from, Api.GetFromAddress(), amount);
        }

        [SmartContractFunction("${this}.Approve", new string[] { }, new[] {"${this}._allowancePlaceHolder"})]
        public void Approve(Address spender, ulong amount)
        {
            Allowances.Approve(spender, amount);
            new Approved
            {
                Owner = Api.GetFromAddress(),
                Spender = spender,
                Amount = amount
            }.Fire();
        }

        [SmartContractFunction("${this}.UnApprove", new string[] { }, new[] {"${this}._allowancePlaceHolder"})]
        public void UnApprove(Address spender, ulong amount)
        {
            var amountOrAll = Math.Min(amount, Allowances.GetAllowance(Api.GetFromAddress(), spender));
            Allowances.Reduce(Api.GetFromAddress(), spender, amountOrAll);
            new UnApproved
            {
                Owner = Api.GetFromAddress(),
                Spender = spender,
                Amount = amountOrAll
            }.Fire();
        }

        [SmartContractFunction("${this}.Burn", new string[] { }, new[] {"${this}._balances"})]
        public void Burn(ulong amount)
        {
            var bal = _balances[Api.GetFromAddress()];
            Api.Assert(bal >= amount, "Burner doesn't own enough balance.");
            _balances[Api.GetFromAddress()] = bal.Sub(amount);
            _totalSupply.SetValue(_totalSupply.GetValue().Sub(amount));
            new Burned
            {
                Burner = Api.GetFromAddress(),
                Amount = amount
            }.Fire();
        }

        #region Used Transaction Fees

        /// <summary>
        /// The fees will be first locked according to transaction hash and will be claimed by the miner during
        /// finalizing the block. This method is only called by the main transaction (non-inline transactions).
        /// </summary>
        /// <param name="txHash"></param>
        /// <param name="feeAmount"></param>
        public void ChargeTransactionFees(ulong feeAmount)
        {
            var fromAddress = Api.GetFromAddress();
            _balances[fromAddress] = _balances[fromAddress].Sub(feeAmount);
            _chargedFees[fromAddress] = _chargedFees[fromAddress].Add(feeAmount);
        }

        [Fee(0)]
        public void ClaimTransactionFees(ulong height)
        {
            var feePoolAddressNotSet =
                _feePoolAddress.GetValue().Value == null || _feePoolAddress.GetValue().Value.Length == 0;
            Api.Assert(!feePoolAddressNotSet, "Fee pool address is not set.");
            var blk = Api.GetBlockByHeight(height);
            var senders = blk.Body.TransactionList.Select(t => t.From).ToList();
            var feePool = _feePoolAddress.GetValue();
            foreach (var sender in senders)
            {
                var fee = _chargedFees[sender];
                _chargedFees[sender] = 0UL;
                _balances[feePool] = _balances[feePool].Add(fee);
            }
        }

        #endregion Used Transaction Fees

        #endregion Actions

        #endregion ABI (Public) Methods

        #region Private Methods

        [SmartContractFunction("${this}.DoTransfer", new string[] { }, new[] {"${this}._balances"})]
        private void DoTransfer(Address from, Address to, ulong amount)
        {
            var balSender = _balances[from];
            Api.Assert(balSender >= amount, $"Insufficient balance. Current balance: {balSender}");
            var balReceiver = _balances[to];
            balSender = balSender.Sub(amount);
            balReceiver = balReceiver.Add(amount);
            _balances[from] = balSender;
            _balances[to] = balReceiver;
            new Transfered
            {
                From = from,
                To = to,
                Amount = amount
            }.Fire();
        }

        #endregion Private Methods
    }

    #region Helper Type

    internal class Allowances
    {
        private static MapToUInt64<AddressPair> _allowances = new MapToUInt64<AddressPair>("_Allowances_");

        public static ulong GetAllowance(Address owner, Address spender)
        {
            return _allowances.GetValue(new AddressPair() {First = owner, Second = spender});
        }

        public static void Approve(Address spender, ulong amount)
        {
            var pair = new AddressPair {First = Api.GetFromAddress(), Second = spender};
            _allowances[pair] = _allowances[pair].Add(amount);
        }

        public static void Reduce(Address owner, Address spender, ulong amount)
        {
            var pair = new AddressPair {First = owner, Second = spender};
            _allowances[pair] = _allowances[pair].Sub(amount);
        }
    }

    #endregion Helper Type
}