using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using AElf.Kernel;
using AElf.Kernel.Concurrency;
using AElf.Kernel.Concurrency.Execution;
using AElf.Kernel.Concurrency.Metadata;
using AElf.Kernel.Concurrency.Scheduling;
using AElf.Kernel.Managers;
using AElf.Kernel.Services;
using AElf.Kernel.Storages;
using AElf.Runtime.CSharp;
using AElf.Sdk.CSharp;
using AElf.Types.CSharp;
using Google.Protobuf;
using NLog;
using ServiceStack;
using ServiceStack.Text;

namespace AElf.Benchmark
{
    public class Benchmarks
    {
        private readonly IChainCreationService _chainCreationService;
        private readonly IBlockManager _blockManager;
        private readonly ISmartContractService _smartContractService;
        private readonly IWorldStateDictator _worldStateDictator;
        private readonly IAccountContextService _accountContextService;
        private readonly ILogger _logger;
        private readonly BenchmarkOptions _options;
        private readonly IConcurrencyExecutingService _concurrencyExecutingService;


        private readonly ServicePack _servicePack;

        private readonly TransactionDataGenerator _dataGenerater;
        private readonly Hash _contractHash;
        
        private Hash ChainId { get; }
        private int _incrementId = 0;
        
        public byte[] SmartContractZeroCode
        {
            get
            {
                byte[] code = null;
                using (FileStream file = File.OpenRead(System.IO.Path.GetFullPath(System.IO.Path.Combine(_options.DllDir, _options.ZeroContractDll))))
                {
                    code = file.ReadFully();
                }
                return code;
            }
        }

        public Benchmarks(IChainCreationService chainCreationService, IBlockManager blockManager,
            IChainContextService chainContextService, ISmartContractService smartContractService,
            ILogger logger, IFunctionMetadataService functionMetadataService,
            IAccountContextService accountContextService, IWorldStateDictator worldStateDictator, BenchmarkOptions options, IConcurrencyExecutingService concurrencyExecutingService)
        {
            ChainId = Hash.Generate();
            
            _worldStateDictator = worldStateDictator;
            _worldStateDictator.SetChainId(ChainId).DeleteChangeBeforesImmidiately = true;
            
            _chainCreationService = chainCreationService;
            _blockManager = blockManager;
            _smartContractService = smartContractService;
            _accountContextService = accountContextService;
            _logger = logger;
            _options = options;
            _concurrencyExecutingService = concurrencyExecutingService;


            _servicePack = new ServicePack()
            {
                ChainContextService = chainContextService,
                SmartContractService = _smartContractService,
                ResourceDetectionService = new ResourceUsageDetectionService(functionMetadataService),
                WorldStateDictator = _worldStateDictator,
                AccountContextService = _accountContextService,
            };

            //TODO: set timeout to int.max in order to run the large trunk of tx list
            
            _dataGenerater = new TransactionDataGenerator(options.TxNumber);
            byte[] code = null;
            using (FileStream file = File.OpenRead(System.IO.Path.GetFullPath(options.DllDir + "/" + options.ContractDll)))
            {
                code = file.ReadFully();
            }
            _contractHash = Prepare(code).Result;
            
            InitContract(_contractHash, _dataGenerater.KeyDict.Keys).GetResult();
            
        }

        

        public async Task BenchmarkEvenGroup()
        {
            var resDict = new Dictionary<string, double>();
            for (int currentGroupCount = _options.GroupRange.ElementAt(0); currentGroupCount <= _options.GroupRange.ElementAt(1); currentGroupCount++)
            {
                var res = await MultipleGroupBenchmark(_options.TxNumber, currentGroupCount);
                resDict.Add(res.Key, res.Value);
            }

            _logger.Info("Benchmark report \n \t Configuration: \n" + 
                         string.Join("\n", _options.ToStringDictionary().Select(option => string.Format("\t {0} - {1}", option.Key, option.Value))) + 
                         "\n\n\n\t Benchmark result:\n" + string.Join("\n", resDict.Select(kv=> "\t" + kv.Key + ": " + kv.Value)));

            // + string.Join("\n", resDict.Select(kv=> "\t" + kv.Key + ": " + kv.Value)))
        }


        public async Task<KeyValuePair<string, double>> MultipleGroupBenchmark(int txNumber, int groupCount)
        {
            int repeatTime = _options.RepeatTime;
        
            var txList = _dataGenerater.GetMultipleGroupTx(txNumber, groupCount, _contractHash);
            long timeused = 0;
            for (int i = 0; i < repeatTime; i++)
            {
                foreach (var tx in txList)
                {
                    tx.IncrementId += 1;
                }
                Stopwatch swExec = new Stopwatch();
                swExec.Start();

                var txResult = await Task.Factory.StartNew(async () =>
                {
                    return await _concurrencyExecutingService.ExecuteAsync(txList, ChainId,new Grouper(_servicePack.ResourceDetectionService, _logger) );
                }).Unwrap();
        
                swExec.Stop();
                timeused += swExec.ElapsedMilliseconds;
                txResult.ForEach(trace =>
                {
                    if (!trace.StdErr.IsNullOrEmpty())
                    {
                        _logger.Error("Execution error: " + trace.StdErr);
                    }
                });
            }
            
            var time = txNumber / (timeused / 1000.0 / (double)repeatTime);
            var str = groupCount + " groups with " + txList.Count + " tx in total";

            return new KeyValuePair<string,double>(str, time);
        }
        
        public ulong NewIncrementId()
        {
            var n = Interlocked.Increment(ref _incrementId);
            return (ulong)n;
        }

        public async Task<Hash> Prepare(byte[] contractCode)
        {
            //create smart contact zero
            var reg = new SmartContractRegistration
            {
                Category = 0,
                ContractBytes = ByteString.CopyFrom(SmartContractZeroCode),
                ContractHash = Hash.Zero
            };
            
            var chain = await _chainCreationService.CreateNewChainAsync(ChainId, reg);
            var genesis = await _blockManager.GetBlockAsync(chain.GenesisBlockHash);
            var contractAddressZero = _chainCreationService.GenesisContractHash(ChainId);
            
            
            //deploy token contract
            var code = contractCode;
            
            var txnDep = new Transaction()
            {
                From = Hash.Zero.ToAccount(),
                To = contractAddressZero,
                IncrementId = 0,
                MethodName = "DeploySmartContract",
                Params = ByteString.CopyFrom(ParamsPacker.Pack(0, code))
            };
            
            var txnCtxt = new TransactionContext()
            {
                Transaction = txnDep
            };
            
            var executive = await _smartContractService.GetExecutiveAsync(contractAddressZero, ChainId);
            await executive.SetTransactionContext(txnCtxt).Apply(true);

            
            var contractAddr = txnCtxt.Trace.RetVal.DeserializeToPbMessage<Hash>();
            return contractAddr;
        }

        public async Task InitContract(Hash contractAddr, IEnumerable<Hash> addrBook)
        {
            //init contract
            string name = "TestToken" + _incrementId;
            var txnInit = new Transaction
            {
                From = Hash.Zero.ToAccount(),
                To = contractAddr,
                IncrementId = NewIncrementId(),
                MethodName = "Initialize",
                Params = ByteString.CopyFrom(ParamsPacker.Pack(name, Hash.Zero.ToAccount()))
            };
            
            var txnInitCtxt = new TransactionContext()
            {
                Transaction = txnInit
            };
            var executiveUser = await _smartContractService.GetExecutiveAsync(contractAddr, ChainId);
            await executiveUser.SetTransactionContext(txnInitCtxt).Apply(true);
            
            //init contract
            var initTxList = new List<ITransaction>();
            foreach (var addr in addrBook)
            {
                var txnBalInit = new Transaction
                {
                    From = Hash.Zero.ToAccount(),
                    To = contractAddr,
                    IncrementId = NewIncrementId(),
                    MethodName = "InitBalance",
                    Params = ByteString.CopyFrom(ParamsPacker.Pack(addr))
                };
                
                initTxList.Add(txnBalInit);
            }
            var txTrace = await _concurrencyExecutingService.ExecuteAsync(initTxList, ChainId,new Grouper(_servicePack.ResourceDetectionService, _logger));
        }
        
    }
}