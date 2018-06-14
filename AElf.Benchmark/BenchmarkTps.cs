using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using AElf.Cryptography.ECDSA;
using AElf.Kernel;
using AElf.Kernel.Concurrency;
using AElf.Kernel.Extensions;
using AElf.Kernel.KernelAccount;
using AElf.Kernel.Managers;
using AElf.Kernel.Services;
using AElf.Runtime.CSharp;
using Akka.Actor;
using Google.Protobuf;
using ServiceStack;

namespace AElf.Benchmark
{
    public class Benchmarks
    {
        private IWorldStateManager _worldStateManager;
        private IChainCreationService _chainCreationService;
        private IBlockManager _blockManager;
        private ISmartContractManager _smartContractManager;
        private ISmartContractService _smartContractService;
        
        private ISmartContractRunnerFactory _smartContractRunnerFactory = new SmartContractRunnerFactory();

        public Benchmarks(IWorldStateManager worldStateManager, IChainCreationService chainCreationService, IBlockManager blockManager, ISmartContractManager smartContractManager)
        {
            _worldStateManager = worldStateManager;
            _chainCreationService = chainCreationService;
            _blockManager = blockManager;
            _smartContractManager = smartContractManager;
        }

        private Hash ChainId { get; } = Hash.Generate();
        private int _incrementId = 0;
        
        public byte[] SmartContractZeroCode
        {
            get
            {
                byte[] code = null;
                using (FileStream file = File.OpenRead(System.IO.Path.GetFullPath("../../../../AElf.Contracts.SmartContractZero/bin/Debug/netstandard2.0/AElf.Contracts.SmartContractZero.dll")))
                {
                    code = file.ReadFully();
                }
                return code;
            }
        }

        public static void Main()
        {
            
        }
        
        public async Task<Dictionary<string, double>> RunBenchmark(int txNumber, double conflictRate)
        {
            TransactionDataGenerator dataGenerator = new TransactionDataGenerator();

            var txList = dataGenerator.GenerateTransferTransactions("AElfTestContract".CalculateHash(), txNumber, conflictRate,
                out var keyDict);

            byte[] code = null;
            using (FileStream file = File.OpenRead(System.IO.Path.GetFullPath("AElf.Benchmark.dll")))
            {
                code = file.ReadFully();
            }

            var contractHash = await Prepare("./", code, keyDict.Keys);
            
            Stopwatch swVerifer = new Stopwatch();
            swVerifer.Start();

            var tasks = new List<Task>();
            
            foreach (var tx in txList)
            {
                var task = Task.Run(() =>
                {
                    ECKeyPair recipientKeyPair = ECKeyPair.FromPublicKey(tx.P.ToByteArray());
                    ECVerifier verifier = new ECVerifier(recipientKeyPair);
                    if(!verifier.Verify(tx.GetSignature(), tx.GetHash().GetHashBytes()))
                    {
                        throw new Exception("Signature failed");
                    }
                });
                tasks.Add(task);
            }
            Task.WaitAll(tasks.ToArray());
            
            swVerifer.Stop();

            //Execution
            Stopwatch swExec = new Stopwatch();
            swExec.Start();

            var sysActor = ActorSystem.Create("benchmark");
            var executingService = new ParallelTransactionExecutingService(sysActor);
            var txResult = Task.Factory.StartNew(async () =>
            {
                return await executingService.ExecuteAsync(txList, ChainId);
            }).Unwrap().Result;
            
            swExec.Stop();
            
            Dictionary<string, double> res = new Dictionary<string, double>();

            var verifyPerSec = txNumber / (swVerifer.ElapsedMilliseconds / 1000.0);
            res.Add("verifyTPS", verifyPerSec);

            var executeTPS = txNumber / (swExec.ElapsedMilliseconds / 1000.0);
            res.Add("executeTPS", executeTPS);
            return res;
        }
        
        public ulong NewIncrementId()
        {
            var n = Interlocked.Increment(ref _incrementId);
            return (ulong)n;
        }

        public async Task<Hash> Prepare(string pathToContractDll, byte[] contractCode, IEnumerable<Hash> addrBook)
        {
            //init smart contract service
            var runner = new SmartContractRunner(pathToContractDll);
            _smartContractRunnerFactory.AddRunner(0, runner);
            _smartContractService = new SmartContractService(_smartContractManager, _smartContractRunnerFactory, _worldStateManager);
            
            //create smart contact zero
            var reg = new SmartContractRegistration
            {
                Category = 0,
                ContractBytes = ByteString.CopyFrom(SmartContractZeroCode),
                ContractHash = Hash.Zero
            };
            
            var chain = await _chainCreationService.CreateNewChainAsync(ChainId, reg);
            var genesis = await _blockManager.GetBlockAsync(chain.GenesisBlockHash);
            var contractAddressZero = ChainId.CalculateHashWith("__SmartContractZero__");

            //deploy token contract
            var code = contractCode;

            var regExample = new SmartContractRegistration
            {
                Category = 0,
                ContractBytes = ByteString.CopyFrom(code),
                ContractHash = code.CalculateHash()
            };
            
            var txnDep = new Transaction()
            {
                From = Hash.Zero,
                To = contractAddressZero,
                IncrementId = NewIncrementId(),
                MethodName = "DeploySmartContract",
                Params = ByteString.CopyFrom(new AElf.Kernel.Parameters()
                {
                    Params = {
                        new Param
                        {
                            RegisterVal = regExample
                        }
                    }
                }.ToByteArray())
            };
            
            var txnCtxt = new TransactionContext()
            {
                Transaction = txnDep
            };
            
            var executive = await _smartContractService.GetExecutiveAsync(contractAddressZero, ChainId);
            await executive.SetTransactionContext(txnCtxt).Apply();
            
            var contractAddr = txnCtxt.Trace.RetVal.Unpack<Hash>();
            
            //init contract
            var txnInit = new Transaction
            {
                From = Hash.Zero,
                To = contractAddr,
                IncrementId = NewIncrementId(),
                MethodName = "InitializeAsync",
                Params = ByteString.CopyFrom(new AElf.Kernel.Parameters()
                {
                    Params = {
                        new Param
                        {
                            StrVal = "TestToken" + _incrementId
                        },
                        new Param
                        {
                            HashVal = Hash.Zero
                        }
                    }
                }.ToByteArray())
            };
            
            var txnInitCtxt = new TransactionContext()
            {
                Transaction = txnInit
            };
            var executiveUser = await _smartContractService.GetExecutiveAsync(contractAddr, ChainId);
            await executiveUser.SetTransactionContext(txnInitCtxt).Apply();
            
            //init contract
            var tasks = new List<Task>();
            foreach (var addr in addrBook)
            {
                var task = Task.Run(async () =>
                {
                    var txnBalInit = new Transaction
                    {
                        From = Hash.Zero,
                        To = contractAddr,
                        IncrementId = NewIncrementId(),
                        MethodName = "InitBalance",
                        Params = ByteString.CopyFrom(new AElf.Kernel.Parameters()
                        {
                            Params = {
                                new Param
                                {
                                    HashVal = addr
                                },
                                new Param
                                {
                                    HashVal = Hash.Zero
                                }
                            }
                        }.ToByteArray())
                    };
            
                    var txnBalInitCtx = new TransactionContext()
                    {
                        Transaction = txnInit
                    };
                    var executiveBalInitUser = await _smartContractService.GetExecutiveAsync(contractAddr, ChainId);
                    await executiveBalInitUser.SetTransactionContext(txnBalInitCtx).Apply();
                });
                tasks.Add(task);
            }

            Task.WaitAll(tasks.ToArray());
            
            return contractAddr;
        }
        
    }
}