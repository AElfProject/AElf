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

        private readonly Dictionary<int, ICalculateWay> _defaultPieceWise = new Dictionary<int, ICalculateWay>();
        private Dictionary<int, ICalculateWay> _pieceWiseCache = new Dictionary<int, ICalculateWay>();

        private readonly ConcurrentDictionary<BlockIndex, Dictionary<int, ICalculateWay>> _forkCache =
            new ConcurrentDictionary<BlockIndex, Dictionary<int, ICalculateWay>>();

        public ICalculateAlgorithm AddDefaultAlgorithm(int limit, ICalculateWay func)
        {
            _defaultPieceWise[limit] = func;
            return this;
        }

        public void RemoveForkCache(List<BlockIndex> blockIndexes)
        {
            foreach (var blockIndex in blockIndexes)
            {
                if (!_forkCache.TryGetValue(blockIndex, out _)) continue;
                _forkCache.TryRemove(blockIndex, out _);
            }
        }

        public void SetIrreversedCache(List<BlockIndex> blockIndexes)
        {
            Hash blockHash = null;
            long height = 0;
            foreach (var blockIndex in blockIndexes)
            {
                if (!_forkCache.TryGetValue(blockIndex, out var calAlgoritm)) continue;
                _pieceWiseCache = calAlgoritm;
                _forkCache.TryRemove(blockIndex, out _);
                blockHash = blockIndex.BlockHash;
                height = blockIndex.BlockHeight;
            }

            var tokenStub = _tokenStTokenContractReaderFactory.Create(new ChainContext
            {
                BlockHash = blockHash,
                BlockHeight = height
            });
            var parameters = TransferFromParaDic(_pieceWiseCache);
            tokenStub.SetCalculateFeeAlgorithmParameters.CallAsync(parameters).GetAwaiter().GetResult();
        }

        public async Task<long> Calculate(int count)
        {
            var pieceWise = await GetPieceWiseFuncUnderContext();
            long totalCost = 0;
            int prePieceKey = 0;
            foreach (var piece in pieceWise.OrderBy(x => x.Key))
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
            var pieceWise = await GetPieceWiseFuncUnderContext();
            if (pieceWise == null)
                return;
            pieceWise = pieceWise.ToDictionary(x => x.Key, x => x.Value);
            if (pieceWise.ContainsKey(pieceKey))
                pieceWise.Remove(pieceKey);
            SetAlgorithm(pieceWise);
        }

        public async Task Update(int pieceKey, CalculateFunctionTypeEnum funcTypeEnum, IDictionary<string, string> parameters)
        {
            var pieceWise = await GetPieceWiseFuncUnderContext();
            if (!pieceWise.ContainsKey(pieceKey))
                return;
            AddPieceFunction(pieceKey, pieceWise, funcTypeEnum, parameters);
        }

        public async Task AddByParam(int pieceKey, CalculateFunctionTypeEnum funcTypeEnum,
            IDictionary<string, string> parameters)
        {
            var pieceWise = await GetPieceWiseFuncUnderContext();
            if (pieceWise.ContainsKey(pieceKey) || pieceKey <= 0)
                return;
            AddPieceFunction(pieceKey, pieceWise, funcTypeEnum, parameters);
        }

        private void AddPieceFunction(int pieceKey, IDictionary<int, ICalculateWay> pieceWise,
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

            if (newCalculateWay == null || !newCalculateWay.InitParameter(parameters)) return;
            if (CalculateAlgorithmContext.BlockIndex != null)
            {
                _forkCache[CalculateAlgorithmContext.BlockIndex] = pieceWise.ToDictionary(x => x.Key, x => x.Value);
                _forkCache[CalculateAlgorithmContext.BlockIndex][pieceKey] = newCalculateWay;
            }
            else
            {
                _pieceWiseCache[pieceKey] = newCalculateWay; //todo
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
            if (_pieceWiseCache != null && _pieceWiseCache.Count > 0)
            {
                return _pieceWiseCache;
            }

            var chain = await _blockchainService.GetChainAsync();

            var tokenStub = _tokenStTokenContractReaderFactory.Create(new ChainContext
            {
                BlockHash = chain.LastIrreversibleBlockHash,
                BlockHeight = chain.LastIrreversibleBlockHeight
            });

            var parameters =
                await tokenStub.GetCalculateFeeAllParameters.CallAsync(new SInt32Value
                    {Value = (int) CalculateAlgorithmContext.CalculateFeeTypeEnum});
            if (parameters == null)
            {
                return _defaultPieceWise;
            }
            if(_pieceWiseCache == null)
                _pieceWiseCache = new Dictionary<int, ICalculateWay>();
            _pieceWiseCache.Clear();
            foreach (var func in parameters.AllParameter)
            {
                AddPieceFunction(func.PieceKey, _pieceWiseCache, (CalculateFunctionTypeEnum) func.FunctionType,
                    func.ParameterDic);
            }

            return _pieceWiseCache;
        }

        private void SetAlgorithm(Dictionary<int, ICalculateWay> calAlgorithm)
        {
            if (CalculateAlgorithmContext.BlockIndex == null)
                return;
            _forkCache[CalculateAlgorithmContext.BlockIndex] = calAlgorithm;
        }

        private AllCalculateFeeParameter TransferFromParaDic(IDictionary<int, ICalculateWay> calAlgorithmDic)
        {
            if (calAlgorithmDic == null)
                return null;
            var allCalculateFeeParameter = new AllCalculateFeeParameter();
            foreach (var calAlgorithm in calAlgorithmDic)
            {
                var parameterStrDic = calAlgorithm.Value.GetParameterDic();
                var parameter = new CalculateFeeParameter
                {
                    FeeType = (int) CalculateAlgorithmContext.CalculateFeeTypeEnum,
                    PieceKey = calAlgorithm.Key,
                    FunctionType = (int) calAlgorithm.Value.FunctionTypeEnum
                };
                foreach (var parameterPair in parameterStrDic)
                {
                    parameter.ParameterDic[parameterPair.Key] = parameterPair.Value;
                }

                allCalculateFeeParameter.AllParameter.Add(parameter);
            }

            return allCalculateFeeParameter;
        }
    }

    #endregion
}