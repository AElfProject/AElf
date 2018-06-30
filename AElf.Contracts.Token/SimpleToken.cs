using AElf.Kernel;
using AElf.Sdk.CSharp;
using AElf.Sdk.CSharp.Types;
using AElf.Types.CSharp.MetadataAttribute;
using Api = AElf.Sdk.CSharp.Api;

namespace AElf.Contracts.Token
{
    #region Events

    public class Transfered : Event
    {
        [Indexed] public Hash From { get; set; }
        [Indexed] public Hash To { get; set; }
        [Indexed] public ulong Amount { get; set; }
    }

    public class Approved : Event
    {
        [Indexed] public Hash Owner { get; set; }
        [Indexed] public Hash Spender { get; set; }
        [Indexed] public ulong Amount { get; set; }
    }


    public class UnApproved : Event
    {
        [Indexed] public Hash Owner { get; set; }
        [Indexed] public Hash Spender { get; set; }
        [Indexed] public ulong Amount { get; set; }
    }

    #endregion Events

    public class SimpleToken : CSharpSmartContract
    {
        private readonly BoolField _initialized = new BoolField("_Initialized_");
        private readonly StringField _symbol = new StringField("_Symbol_");
        private readonly StringField _tokenName = new StringField("_TokenName_");
        private readonly UInt64Field _totalSupply = new UInt64Field("_TotalSupply_");
        private readonly UInt32Field _decimals = new UInt32Field("_Decimals_");
        private readonly MapToUInt64<Hash> _balances = new MapToUInt64<Hash>("_Balances_");
        
        [SmartContractFieldData("${this}._lock", DataAccessMode.ReadWriteAccountSharing)]
        private readonly object _lock;

        #region ABI (Public) Methods

        #region View Only Methods

        [SmartContractFunction("${this}.Symbol", new string[]{}, new []{"${this}._lock"})]
        public string Symbol()
        {
            return _symbol.GetValue();
        }

        [SmartContractFunction("${this}.TokenName", new string[]{}, new []{"${this}._lock"})]
        public string TokenName()
        {
            return _tokenName.GetValue();
        }

        [SmartContractFunction("${this}.TotalSupply", new string[]{}, new []{"${this}._lock"})]
        public ulong TotalSupply()
        {
            return _totalSupply.GetValue();
        }

        [SmartContractFunction("${this}.Decimals", new string[]{}, new []{"${this}._lock"})]
        public uint Decimals()
        {
            return _decimals.GetValue();
        }

        [SmartContractFunction("${this}.BalanceOf", new string[]{}, new []{"${this}._lock"})]
        public ulong BalanceOf(Hash owner)
        {
            return _balances[owner];
        }

        [SmartContractFunction("${this}.Allowance", new string[]{}, new []{"${this}._lock"})]
        public ulong Allowance(Hash owner, Hash spender)
        {
            return Allowances.GetAllowance(owner, spender);
        }

        #endregion View Only Methods


        #region Actions

        [SmartContractFunction("${this}.Allowance", new string[]{}, new []{"${this}._lock"})]
        public void Initialize(string symbol, string tokenName, ulong totalSupply, uint decimals)
        {
            Api.Assert(!_initialized.GetValue(), "Already initialized.");
            Api.Assert(Api.GetContractOwner().Equals(Api.GetTransaction().From),
                "Only owner can initialize the contract state.");
            _symbol.SetValue(symbol);
            _tokenName.SetValue(tokenName);
            _totalSupply.SetValue(totalSupply);
            _decimals.SetValue(decimals);
            _balances[Api.GetTransaction().From] = totalSupply;
            _initialized.SetValue(true);
        }

        [SmartContractFunction("${this}.Transfer", new string[]{}, new []{"${this}._lock"})]
        public void Transfer(Hash to, ulong amount)
        {
            var from = Api.GetTransaction().From;
            DoTransfer(from, to, amount);
        }

        [SmartContractFunction("${this}.TransferFrom", new string[]{}, new []{"${this}._lock"})]
        public void TransferFrom(Hash from, Hash to, ulong amount)
        {
            var allowance = Allowances.GetAllowance(from, Api.GetTransaction().From);
            Api.Assert(allowance > amount, "Insufficient allowance.");

            DoTransfer(from, to, amount);
            Allowances.Reduce(from, amount);
        }

        [SmartContractFunction("${this}.Approve", new string[]{}, new []{"${this}._lock"})]
        public void Approve(Hash spender, ulong amount)
        {
            Allowances.Approve(spender, amount);
            new Approved()
            {
                Owner = Api.GetTransaction().From,
                Spender = spender,
                Amount = amount
            }.Fire();
        }

        [SmartContractFunction("${this}.UnApprove", new string[]{}, new []{"${this}._lock"})]
        public void UnApprove(Hash spender, ulong amount)
        {
            Allowances.Reduce(spender, amount);
            new UnApproved()
            {
                Owner = Api.GetTransaction().From,
                Spender = spender,
                Amount = amount
            }.Fire();
        }

        #endregion Actions

        #endregion ABI (Public) Methods


        #region Private Methods

        private void DoTransfer(Hash from, Hash to, ulong amount)
        {
            var balSender = _balances[from];
            Api.Assert(balSender > amount, "Insufficient balance.");
            var balReceiver = _balances[to];
            balSender = balSender.Sub(amount);
            balReceiver = balReceiver.Add(amount);
            _balances[from] = balSender;
            _balances[to] = balReceiver;
            new Transfered()
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
        private static MapToUInt64<HashPair> _allowances = new MapToUInt64<HashPair>("_Allowances_");

        public static ulong GetAllowance(Hash owner, Hash spender)
        {
            return _allowances.GetValue(new HashPair() {First = owner, Second = spender});
        }

        public static void Approve(Hash spender, ulong amount)
        {
            var pair = new HashPair() {First = Api.GetTransaction().From, Second = spender};
            _allowances[pair] = _allowances[pair].Add(amount);
        }

        public static void Reduce(Hash owner, ulong amount)
        {
            var pair = new HashPair() {First = owner, Second = Api.GetTransaction().From};
            _allowances[pair] = _allowances[pair].Sub(amount);
        }
    }

    #endregion Helper Type
}