using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using AElf.Contracts.MultiToken;
using AElf.Kernel.Blockchain.Application;
using AElf.Kernel.SmartContract.Application;
using AElf.Sdk.CSharp;
using AElf.Types;
using Google.Protobuf.WellKnownTypes;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Logging.Abstractions;

namespace AElf.Kernel.TransactionPool.Application
{
    #region ICalculateAlgorithm implemention

    class CalculateAlgorithmContext : ICalculateAlgorithmContext
    {
        public int CalculateFeeTypeEnum { get; set; }
        public BlockIndex BlockIndex { get; set; }
    }

    class CalculateAlgorithm : ICalculateAlgorithm
    {
        private readonly ITokenContractReaderFactory _tokenStTokenContractReaderFactory;
        private readonly IBlockchainService _blockchainService;
        private readonly IChainBlockLinkService _chainBlockLinkService;

        public ICalculateAlgorithmContext CalculateAlgorithmContext { get; } = new CalculateAlgorithmContext();
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
                if (!_forkCache.TryGetValue(blockIndex, out var calAlgorithm)) continue;
                _pieceWiseFuncCache = calAlgorithm;
                _forkCache.TryRemove(blockIndex, out _);
            }
        }

        public async Task<long> CalculateAsync(int count)
        {
            count = count < 0 ? int.MaxValue : count;
            var pieceWiseFunc = await GetPieceWiseFuncUnderContextAsync();
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

        public void AddAlgorithmByBlock(BlockIndex blockIndex, IList<ICalculateWay> allFunc)
        {
            var funcDic = new Dictionary<int, ICalculateWay>();
            foreach (var func in allFunc)
            {
                funcDic[func.PieceKey] = func;
            }

            _forkCache[blockIndex] = funcDic;
        }
        private void AddPieceFunction(int pieceKey, IDictionary<int, ICalculateWay> pieceWiseFunc,
            CalculateFunctionTypeEnum funcTypeEnum,
            IDictionary<string, int> parameters)
        {
            
        }

        private async Task<Dictionary<int, ICalculateWay>> GetPieceWiseFuncUnderContextAsync()
        {
            var keys = _forkCache.Keys.ToArray();
            if (keys.Length == 0) return await GetDefaultPieceWiseFunctionAsync();
            var minHeight = keys.Select(k => k.BlockHeight).Min();
            Dictionary<int, ICalculateWay> algorithm = null;
            var blockIndex = new BlockIndex
            {
                BlockHash = CalculateAlgorithmContext.BlockIndex.BlockHash,
                BlockHeight = CalculateAlgorithmContext.BlockIndex.BlockHeight
            };
            do
            {
                if (_forkCache.TryGetValue(blockIndex, out var value))
                {
                    algorithm = value;
                    break;
                }

                var link = _chainBlockLinkService.GetCachedChainBlockLink(blockIndex.BlockHash);
                blockIndex.BlockHash = link?.PreviousBlockHash;
                blockIndex.BlockHeight--;
            } while (blockIndex.BlockHash != null && blockIndex.BlockHeight >= minHeight);

            return algorithm ?? await GetDefaultPieceWiseFunctionAsync();
        }

        private async Task<Dictionary<int, ICalculateWay>> GetDefaultPieceWiseFunctionAsync()
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

            CalculateFeeCoefficientsOfType parameters;
            if (CalculateAlgorithmContext.CalculateFeeTypeEnum == (int) FeeTypeEnum.Tx)
            {
                parameters = await tokenStub.GetCalculateFeeCoefficientOfSender.CallAsync(new Empty());
            }
            else
            {
                parameters = await tokenStub.GetCalculateFeeCoefficientOfContract.CallAsync(new SInt32Value
                    {Value = CalculateAlgorithmContext.CalculateFeeTypeEnum});
            }

            if (parameters == null)
            {
                Logger.LogWarning("does not find parameter from contract, initialize from default ");
                _pieceWiseFuncCache = _defaultPieceWiseFunc.ToDictionary(x => x.Key, x => x.Value);
                return _pieceWiseFuncCache;
            }

            if (_pieceWiseFuncCache == null)
                _pieceWiseFuncCache = new Dictionary<int, ICalculateWay>();
            _pieceWiseFuncCache.Clear();
            var calWayDic = new Dictionary<int, ICalculateWay>();
            foreach (var func in parameters.Coefficients)
            {
                ICalculateWay newCalculateWay = null;
                switch (func.FunctionType)
                {
                    case CalculateFunctionTypeEnum.Liner:
                        newCalculateWay = new LinerCalculateWay();
                        break;
                    case CalculateFunctionTypeEnum.Power:
                        newCalculateWay = new PowerCalculateWay();
                        break;
                }

                if (newCalculateWay == null)
                {
                    Logger.LogWarning($"could not find mapped function type {func.FunctionType}");
                    continue;
                }

                calWayDic[func.PieceKey] = newCalculateWay;
                calWayDic[func.PieceKey].InitParameter(func.CoefficientDic);
            }

            _forkCache[CalculateAlgorithmContext.BlockIndex] = calWayDic;
            _pieceWiseFuncCache = calWayDic;
            return _pieceWiseFuncCache;
        }
    }

    #endregion
}