using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using AElf.Contracts.MultiToken;
using AElf.Kernel.Blockchain.Application;
using AElf.Kernel.SmartContract.Application;
using AElf.Kernel.TransactionPool.Infrastructure;
using AElf.Sdk.CSharp;
using AElf.Types;
using Google.Protobuf.WellKnownTypes;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Logging.Abstractions;

namespace AElf.Kernel.TransactionPool.Application
{
    #region ICalculateAlgorithm implemention

    public class CalculateAlgorithmContext : ICalculateAlgorithmContext
    {
        public int CalculateFeeTypeEnum { get; set; }
        public BlockIndex BlockIndex { get; set; }
    }

    internal class CalculateAlgorithmService : ICalculateAlgorithmService
    {
        private readonly ITokenContractReaderFactory _tokenStTokenContractReaderFactory;
        private readonly IBlockchainService _blockchainService;
        private readonly IChainBlockLinkService _chainBlockLinkService;
        private readonly ICalculateFunctionCacheProvider _cacheCacheProvider;

        public ICalculateAlgorithmContext CalculateAlgorithmContext { get; } = new CalculateAlgorithmContext();
        public ILogger<CalculateAlgorithmService> Logger { get; set; }

        public CalculateAlgorithmService(ITokenContractReaderFactory tokenStTokenContractReaderFactory,
            IBlockchainService blockchainService,
            IChainBlockLinkService chainBlockLinkService,
            ICalculateFunctionCacheProvider cacheCacheProvider)
        {
            _tokenStTokenContractReaderFactory = tokenStTokenContractReaderFactory;
            _blockchainService = blockchainService;
            _chainBlockLinkService = chainBlockLinkService;
            _cacheCacheProvider = cacheCacheProvider;
            Logger = new NullLogger<CalculateAlgorithmService>();
        }

        public void RemoveForkCache(List<BlockIndex> blockIndexes)
        {
            _cacheCacheProvider.RemoveFromForkCacheByBlockIndex(blockIndexes);
        }

        public void SetIrreversedCache(List<BlockIndex> blockIndexes)
        {
            _cacheCacheProvider.SyncCache(blockIndexes);
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
            _cacheCacheProvider.SetPieceWiseFunctionToForkCache(blockIndex,funcDic);
        }

        private async Task<Dictionary<int, ICalculateWay>> GetPieceWiseFuncUnderContextAsync()
        {
            var keys = _cacheCacheProvider.GetForkCacheKeys();
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
                if (_cacheCacheProvider.TryGetPieceWiseFunctionFromForkCacheByBlockIndex(blockIndex, out var value))
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
            var tempCache = _cacheCacheProvider.GetPieceWiseFunctionFromNormalCache();
            if (  tempCache!= null)
            {
                return tempCache;
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

                var funDicToLowerKey = func.CoefficientDic.ToDictionary(x => x.Key.ToLower(), x => x.Value);
                calWayDic[func.PieceKey] = newCalculateWay;
                //TODO: if you new a class, why not pass parameters by constructor? you know the type of calWayDic[func.PieceKey], but why don't you use strong signature?
                calWayDic[func.PieceKey].InitParameter(funDicToLowerKey);
            }
            _cacheCacheProvider.SetPieceWiseFunctionToNormalCache(calWayDic);
            return calWayDic;
        }
    }

    #endregion
}