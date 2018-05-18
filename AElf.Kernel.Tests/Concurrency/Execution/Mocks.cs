using System;
using System.Threading.Tasks;
using System.Collections.Generic;
using System.Collections.Concurrent;
using AElf.Kernel.KernelAccount;
using Google.Protobuf;

namespace AElf.Kernel.Tests.Concurrency.Execution
{

	public class SmartContractZeroWithTransfer : ISmartContractZero
	{
		public class InsufficientBalanceException : Exception { }

		private Dictionary<Hash, ulong> _cryptoAccounts = new Dictionary<Hash, ulong>();
		public ConcurrentDictionary<TransferArgs, DateTime> TransactionStartTimes = new ConcurrentDictionary<TransferArgs, DateTime>();
		public ConcurrentDictionary<TransferArgs, DateTime> TransactionEndTimes = new ConcurrentDictionary<TransferArgs, DateTime>();

		public async Task InvokeAsync(IHash caller, string methodname, ByteString bytes)
		{
			var type = typeof(SmartContractZeroWithTransfer);
			var member = type.GetMethod(methodname);

			var p = member.GetParameters()[0]; //first parameters
			ProtobufSerializer serializer = new ProtobufSerializer();
			// TODO: Compare with SmartContractZero
			#region Not Same As SmartContractZero
			var obj = serializer.Deserialize(bytes.ToByteArray(), p.ParameterType);

			await (Task)member.Invoke(this, new object[] { obj });
			#endregion Not Same As Smart Contract Zero
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

		public Task RegisterSmartContract(SmartContractRegistration reg)
		{
			return Task.CompletedTask;
		}

		public Task DeploySmartContract(SmartContractDeployment smartContractRegister)
		{
			return Task.CompletedTask;
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
		public ChainContextWithSmartContractZeroWithTransfer(SmartContractZeroWithTransfer smartContractZero, Hash chainId)
		{
			SmartContractZero = smartContractZero;
			ChainId = chainId;
		}
	}
}
