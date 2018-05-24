using System;
using System.Threading.Tasks;
using System.Collections.Generic;
using System.Collections.Concurrent;
using AElf.Kernel.KernelAccount;
using AElf.Kernel.Services;
using Google.Protobuf;

namespace AElf.Kernel.Tests.Concurrency.Execution
{

	public class SmartContractZeroWithTransfer : ISmartContractZero
	{
		public class InsufficientBalanceException : Exception { }

		private Dictionary<Hash, ulong> _cryptoAccounts = new Dictionary<Hash, ulong>();
		public ConcurrentDictionary<TransferArgs, DateTime> TransactionStartTimes = new ConcurrentDictionary<TransferArgs, DateTime>();
		public ConcurrentDictionary<TransferArgs, DateTime> TransactionEndTimes = new ConcurrentDictionary<TransferArgs, DateTime>();

        public async Task<object> InvokeAsync(SmartContractInvokeContext context)
		{
            IHash caller = context.Caller;
            string methodname = context.MethodName;
            ByteString bytes = context.Params;
			var type = typeof(SmartContractZeroWithTransfer);
			var member = type.GetMethod(methodname);

			var p = member.GetParameters()[0]; //first parameters
			ProtobufSerializer serializer = new ProtobufSerializer();
			// TODO: Compare with SmartContractZero
			#region Not Same As SmartContractZero
			var obj = serializer.Deserialize(bytes.ToByteArray(), p.ParameterType);

			await (Task)member.Invoke(this, new object[] { obj });
            #endregion Not Same As Smart Contract Zero
            return null;
		}

		public void SetBalance(Hash account, ulong balance)
		{
			_cryptoAccounts[account] = balance;
		}

		public ulong GetBalance(Hash account)
		{
			if (!_cryptoAccounts.TryGetValue(account, out var bal))
			{
				bal = 0;
			}
			return bal;
		}

		public string Now()
		{
			return DateTime.Now.ToString("o");
		}

		public async Task Transfer(TransferArgs transfer)
		{
			TransactionStartTimes.TryAdd(transfer, DateTime.Now);
			var qty = transfer.Quantity;
			var from = transfer.From;
			var to = transfer.To;

			var fromBal = GetBalance(transfer.From);
			var toBal = GetBalance(transfer.To);

			if (fromBal <= transfer.Quantity)
			{
				TransactionEndTimes.TryAdd(transfer, DateTime.Now);
				throw new InsufficientMemoryException();
			}

			SetBalance(transfer.From, fromBal - qty);
			SetBalance(transfer.To, toBal + qty);

			await Task.CompletedTask;
			TransactionEndTimes.TryAdd(transfer, DateTime.Now);
		}

		#region ISmartContractZero

		// All are dummies
		public Task InitializeAsync(IAccountDataProvider dataProvider)
		{
			return Task.CompletedTask;
		}

        public Task<object> RegisterSmartContract(SmartContractRegistration reg)
		{
            return Task.FromResult<object>(null);
		}

        public Task<object> DeploySmartContract(SmartContractDeployment smartContractRegister)
		{
            return Task.FromResult<object>(null);
		}

		public Task<ISmartContract> GetSmartContractAsync(Hash hash)
		{
			return Task.FromResult<ISmartContract>(this);
		}
		#endregion
	}

	public class ChainContextWithSmartContractZeroWithTransfer : IChainContext
	{
		public ISmartContractZero SmartContractZero { get; }
		public Hash ChainId { get; }
		public ChainContextWithSmartContractZeroWithTransfer(SmartContractZeroWithTransfer smartContractZero)
		{
			SmartContractZero = smartContractZero;
			ChainId = Hash.Generate();
		}
	}

	public class ChainContextServiceWithAdd : IChainContextService
	{
		private readonly Dictionary<IHash, IChainContext> _chainContexts = new Dictionary<IHash, IChainContext>();

		public void AddChainContext(Hash chainId, IChainContext chainContext)
		{
			_chainContexts.Add(chainId, chainContext);
		}

		public IChainContext GetChainContext(Hash chainId)
		{
			_chainContexts.TryGetValue(chainId, out var ctx);
			return ctx;
		}
	}

	#region For Multichain
	public class SmartContractZeroWithTransfer2 : SmartContractZeroWithTransfer { }

	public class ChainContextWithSmartContractZeroWithTransfer2 : IChainContext
	{
		public ISmartContractZero SmartContractZero { get; }
		public Hash ChainId { get; }
		public ChainContextWithSmartContractZeroWithTransfer2(SmartContractZeroWithTransfer2 smartContractZero)
		{
			SmartContractZero = smartContractZero;
			ChainId = Hash.Generate();
		}
	}
	#endregion For Multichain
}
