using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using AElf.Contracts.MultiToken;
using AElf.Kernel.Blockchain.Application;
using AElf.Kernel.SmartContract.Application;
using AElf.Sdk.CSharp;
using AElf.Types;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Logging.Abstractions;

namespace AElf.Kernel.TransactionPool.Application
{
    class CalculateFeeService : ICalculateFeeService
    {
        private ILogger<CalculateFeeService> Logger { get; set; }

        public CalculateFeeService()
        {
            Logger =  NullLogger<CalculateFeeService>.Instance;
        }
        public ICalculateCostStrategy CalculateCostStrategy { get; set; }

        public async Task<long> CalculateFee(IChainContext chainContext, int cost)
        {
            if (CalculateCostStrategy != null)
                return await CalculateCostStrategy.GetCost(chainContext, cost);
            Logger.LogError("does not find CalculateCostStrategy}");
            return 20000000L;
        }

        #region temporary dealt

        public async Task UpdateFeeCal(IChainContext chainContext, BlockIndex blockIndex, int pieceKey,
            CalculateFunctionTypeEnum funcTypeEnum,
            IDictionary<string, string> param)
        {
            if (CalculateCostStrategy != null)
                await CalculateCostStrategy.UpdateAlgorithm(chainContext, blockIndex, AlgorithmOpCodeEnum.UpdateFunc, pieceKey,
                    funcTypeEnum, param);
        }

        public async Task DeleteFeeCal(IChainContext chainContext, BlockIndex blockIndex, int pieceKey)
        {
            if (CalculateCostStrategy != null)
                await CalculateCostStrategy.UpdateAlgorithm(chainContext, blockIndex, AlgorithmOpCodeEnum.DeleteFunc, pieceKey);
        }

        public async Task AddFeeCal(IChainContext chainContext, BlockIndex blockIndex, int pieceKey,
            CalculateFunctionTypeEnum funcTypeEnum,
            IDictionary<string, string> param)
        {
            if (CalculateCostStrategy != null)
                await CalculateCostStrategy.UpdateAlgorithm(chainContext, blockIndex, AlgorithmOpCodeEnum.AddFunc, pieceKey, funcTypeEnum,
                    param);
        }
        #endregion
    }

    #region ICalculateAlgorithm implemention

    class CalculateAlgorithmContext : ICalculateAlgorithmContext
    {
        public FeeTypeEnum CalculateFeeTypeEnum { get; set; }
        public IChainContext ChainContext { get; set; }
        public BlockIndex BlockIndex { get; set; }
    }
    class CalculateAlgorithm : ICalculateAlgorithm
    {
        private readonly ITokenContractReaderFactory _tokenStTokenContractReaderFactory;
        private readonly IBlockchainService _blockchainService;
        private readonly IChainBlockLinkService _chainBlockLinkService;
        
        public ICalculateAlgorithmContext CalculateAlgorithmContext { get;} = new CalculateAlgorithmContext();
        public ILogger<CalculateAlgorithm> Logger { get; set; }

        public CalculateAlgorithm(ITokenContractReaderFactory tokenStTokenContractReaderFactory,
            IBlockchainService blockchainService,
            IChainBlockLinkService chainBlockLinkService)
        {
            _tokenStTokenContractReaderFactory = tokenStTokenContractReaderFactory;
            _blockchainService = blockchainService;
            _chainBlockLinkService = chainBlockLinkService;
            Logger = new NullLogger<CalculateAlgorithm>();
        }

        private readonly Dictionary<int, ICalculateWay> _defaultPieceWiseFunc = new Dictionary<int, ICalculateWay>();
        private Dictionary<int, ICalculateWay> _pieceWiseFuncCache = null;

        private readonly ConcurrentDictionary<BlockIndex, Dictionary<int, ICalculateWay>> _forkCache =
            new ConcurrentDictionary<BlockIndex, Dictionary<int, ICalculateWay>>();

        public ICalculateAlgorithm AddDefaultAlgorithm(int limit, ICalculateWay func)
        {
            _defaultPieceWiseFunc[limit] = func;
            return this;
        }

        public void RemoveForkCache(List<BlockIndex> blockIndexes)
        {
            foreach (var blockIndex in blockIndexes.Where(blockIndex => _forkCache.TryGetValue(blockIndex, out _)))
            {
                _forkCache.TryRemove(blockIndex, out _);
            }
        }

        public void SetIrreversedCache(List<BlockIndex> blockIndexes)
        {
            foreach (var blockIndex in blockIndexes)
            {
                if (!_forkCache.TryGetValue(blockIndex, out var calAlgoritm)) continue;
                _pieceWiseFuncCache = calAlgoritm;
                _forkCache.TryRemove(blockIndex, out _);
            }
        }

        public async Task<long> Calculate(int count)
        {
            count = count < 0 ? int.MaxValue : count;
            var pieceWiseFunc = await GetPieceWiseFuncUnderContext();
            long totalCost = 0;
            int prePieceKey = 0;
            foreach (var piece in pieceWiseFunc.OrderBy(x => x.Key))
            {
                if (count < piece.Key)
                {
                    totalCost = piece.Value.GetCost(count.Sub(prePieceKey)).Add(totalCost);
                    break;
                }

                var span = piece.Key.Sub(prePieceKey);
                totalCost = piece.Value.GetCost(span).Add(totalCost);
                prePieceKey = piece.Key;
                if (count == piece.Key)
                    break;
            }

            return totalCost;
        }

        public async Task Delete(int pieceKey)
        {
            var pieceWiseFunc = await GetPieceWiseFuncUnderContext();
            if (pieceWiseFunc == null)
            {
                Logger.LogWarning("does not find piecewise function in add");
                return;
            }
            pieceWiseFunc = pieceWiseFunc.ToDictionary(x => x.Key, x => x.Value);
            if (!pieceWiseFunc.ContainsKey(pieceKey))
            {
                Logger.LogWarning($"does not find piece key: {pieceKey} in piecewise function");
                return;
            }
            pieceWiseFunc.Remove(pieceKey);
            SetAlgorithm(pieceWiseFunc);
        }

        public async Task Update(int pieceKey, CalculateFunctionTypeEnum funcTypeEnum, IDictionary<string, string> parameters)
        {
            var pieceWiseFunc = await GetPieceWiseFuncUnderContext();
            if (pieceWiseFunc == null)
            {
                Logger.LogWarning("does not find piecewise function in update");
                return;
            }

            if (!pieceWiseFunc.ContainsKey(pieceKey))
            {
                Logger.LogWarning($"does not find piece key: {pieceKey} in piecewise function");
                return;
            }

            if (!System.Enum.IsDefined(typeof(CalculateFunctionTypeEnum), funcTypeEnum))
            {
                Logger.LogWarning($"does not find mapped function type : {funcTypeEnum}");
                funcTypeEnum = pieceWiseFunc[pieceKey].FunctionTypeEnum;
            }
            AddPieceFunction(pieceKey, pieceWiseFunc, funcTypeEnum, parameters);
        }

        public async Task AddByParam(int pieceKey, CalculateFunctionTypeEnum funcTypeEnum,
            IDictionary<string, string> parameters)
        {
            var pieceWiseFunc = await GetPieceWiseFuncUnderContext();
            if (pieceWiseFunc == null)
            {
                Logger.LogWarning("does not find piecewise function in add");
                return;
            }

            if (pieceWiseFunc.ContainsKey(pieceKey) || pieceKey <= 0)
            {
                Logger.LogWarning($"does not find piece key: {pieceKey} in piecewise function or piece key equals less than 0");
                return;
            }
                
            AddPieceFunction(pieceKey, pieceWiseFunc, funcTypeEnum, parameters);
        }

        private void AddPieceFunction(int pieceKey, IDictionary<int, ICalculateWay> pieceWiseFunc,
            CalculateFunctionTypeEnum funcTypeEnum,
            IDictionary<string, string> parameters)
        {
            var newCalculateWay = funcTypeEnum switch
            {
                CalculateFunctionTypeEnum.Constant => (ICalculateWay) new ConstCalculateWay(),
                CalculateFunctionTypeEnum.Liner => new LinerCalculateWay(),
                CalculateFunctionTypeEnum.Power => new PowerCalculateWay(),
                CalculateFunctionTypeEnum.Ln => new LnCalculateWay(),
                _ => null
            };
            if (newCalculateWay == null)
            {
                Logger.LogWarning($"could not find mapped function type {funcTypeEnum}");
                return;
            }
                
            parameters = parameters.ToDictionary(x => x.Key.ToLower(), x => x.Value);
            if (!newCalculateWay.InitParameter(parameters))
            {
                Logger.LogWarning("illegal parameters");
                return;
            }
            
            if (CalculateAlgorithmContext.BlockIndex != null)
            {
                _forkCache[CalculateAlgorithmContext.BlockIndex] = pieceWiseFunc.ToDictionary(x => x.Key, x => x.Value);
                _forkCache[CalculateAlgorithmContext.BlockIndex][pieceKey] = newCalculateWay;
            }
            else
            {
                _pieceWiseFuncCache[pieceKey] = newCalculateWay;
            }
        }

        private async Task<Dictionary<int, ICalculateWay>> GetPieceWiseFuncUnderContext()
        {
            var chainContext = CalculateAlgorithmContext.ChainContext;
            var keys = _forkCache.Keys.ToArray();
            if (keys.Length == 0 || chainContext == null) return await GetDefaultPieceWiseFunction();
            var minHeight = keys.Select(k => k.BlockHeight).Min();
            Dictionary<int, ICalculateWay> algorithm = null;
            var blockIndex = new BlockIndex
            {
                BlockHash = chainContext.BlockHash,
                BlockHeight = chainContext.BlockHeight
            };
            do
            {
                if (_forkCache.TryGetValue(blockIndex, out var value))
                {
                    algorithm = value;
                }

                var link = _chainBlockLinkService.GetCachedChainBlockLink(blockIndex.BlockHash);
                blockIndex.BlockHash = link?.PreviousBlockHash;
                blockIndex.BlockHeight--;
            } while (blockIndex.BlockHash != null && blockIndex.BlockHeight >= minHeight);

            return algorithm ?? await GetDefaultPieceWiseFunction();
            //Logger.LogTrace($"Get tx size fee unit price: {unitPrice.Value}");
        }

        private async Task<Dictionary<int, ICalculateWay>> GetDefaultPieceWiseFunction()
        {
            if (_pieceWiseFuncCache != null)
            {
                return _pieceWiseFuncCache;
            }

            var chain = await _blockchainService.GetChainAsync();

            var tokenStub = _tokenStTokenContractReaderFactory.Create(new ChainContext
            {
                BlockHash = chain.LastIrreversibleBlockHash,
                BlockHeight = chain.LastIrreversibleBlockHeight
            });

            var parameters =
                await tokenStub.GetCalculateFeeCoefficientByType.CallAsync(new SInt32Value
                    {Value = (int) CalculateAlgorithmContext.CalculateFeeTypeEnum});
            if (parameters == null)
            {
                Logger.LogWarning("does not find parameter from contract, initialize default ");
                _pieceWiseFuncCache = _defaultPieceWiseFunc.ToDictionary(x => x.Key, x => x.Value);
                return _pieceWiseFuncCache;
            }
            if(_pieceWiseFuncCache == null)
                _pieceWiseFuncCache = new Dictionary<int, ICalculateWay>();
            _pieceWiseFuncCache.Clear();
            foreach (var func in parameters.Coefficients)
            {
                AddPieceFunction(func.PieceKey, _pieceWiseFuncCache, (CalculateFunctionTypeEnum) func.FunctionType,
                    func.CoefficientDic);
            }

            return _pieceWiseFuncCache;
        }
        private void SetAlgorithm(Dictionary<int, ICalculateWay> calAlgorithm)
        {
            if (CalculateAlgorithmContext.BlockIndex == null)
            {
                _pieceWiseFuncCache = calAlgorithm;
                return;
            }
               
            _forkCache[CalculateAlgorithmContext.BlockIndex] = calAlgorithm;
        }

        private CalculateFeeCoefficientsOfType TransferFromParaDic(IDictionary<int, ICalculateWay> calAlgorithmDic)
        {
            if (calAlgorithmDic == null)
                return null;
            var allCalculateFeeParameter = new CalculateFeeCoefficientsOfType();
            foreach (var calAlgorithm in calAlgorithmDic)
            {
                var parameterStrDic = calAlgorithm.Value.GetParameterDic();
                var parameter = new CalculateFeeCoefficient
                {
                    FeeType = (int) CalculateAlgorithmContext.CalculateFeeTypeEnum,
                    PieceKey = calAlgorithm.Key,
                    FunctionType = (int) calAlgorithm.Value.FunctionTypeEnum
                };
                foreach (var parameterPair in parameterStrDic)
                {
                    parameter.CoefficientDic[parameterPair.Key] = parameterPair.Value;
                }

                allCalculateFeeParameter.Coefficients.Add(parameter);
            }

            return allCalculateFeeParameter;
        }
    }

    #endregion
}