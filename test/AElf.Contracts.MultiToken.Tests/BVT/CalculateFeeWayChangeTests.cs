using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using AElf.Contracts.Parliament;
using AElf.Kernel;
using AElf.Kernel.SmartContract.Application;
using AElf.Kernel.TransactionPool.Application;
using AElf.Types;
using Google.Protobuf.WellKnownTypes;
using Microsoft.Extensions.DependencyInjection;
using Shouldly;
using Xunit;

namespace AElf.Contracts.MultiToken
{
    public partial class MultiTokenContractTests
    {
        private async Task InitializeCoefficientAsync()
        {
            await TokenContractStub.Create.SendAsync(new CreateInput
            {
                Symbol = "ELF",
                TokenName = "ELF token",
                TotalSupply = 1000_000,
                Decimals = 8,
                Issuer = DefaultAddress
            });
            var initResult = (await TokenContractStub.Initialize.SendAsync(new InitializeInput())).TransactionResult;
            initResult.Status.ShouldBe(TransactionResultStatus.Mined);
        }

        private async Task<CalculateFeeCoefficientsOfType> GetCoefficientByType(FeeTypeEnum type)
        {
            if (type == FeeTypeEnum.Tx)
            {
                return await TokenContractStub.GetCalculateFeeCoefficientOfSender.CallAsync(new Empty());
            }

            return await TokenContractStub.GetCalculateFeeCoefficientOfContract.CallAsync(new SInt32Value
                {Value = (int) type});
        }

        [Fact]
        public async Task Tx_Token_Fee_Calculate_After_Update_Piecewise_Function_Test()
        {
            await InitializeCoefficientAsync();
            var calculateTxCostStrategy = Application.ServiceProvider.GetRequiredService<ICalculateTxCostStrategy>();
            var size = 10000;
            var param = new CalculateFeeCoefficient
            {
                FeeType = FeeTypeEnum.Tx,
                FunctionType = CalculateFunctionTypeEnum.Liner,
                PieceKey = 1000000,
                CoefficientDic = {{"numerator", 1}, {"denominator", 400}}
            };
            var ps = await GetCoefficientByType(FeeTypeEnum.Tx);

            var theOne = ps.Coefficients.SingleOrDefault(x => x.PieceKey == 1000000);
            ps.Coefficients.Remove(theOne);
            ps.Coefficients.Add(param);
            var blockIndex = new BlockIndex();
            IChainContext chainContext = new ChainContext
            {
                BlockHash = blockIndex.BlockHash,
                BlockHeight = blockIndex.BlockHeight
            };
            await HandleTestAsync(ps, blockIndex);

            var updatedFee = await calculateTxCostStrategy.GetCostAsync(chainContext, size);
            updatedFee.ShouldBe(25_0000_0000);

            var apiParam2 = new CalculateFeeCoefficient
            {
                FeeType = FeeTypeEnum.Tx,
                FunctionType = CalculateFunctionTypeEnum.Liner,
                PieceKey = 100,
                CoefficientDic = {{"numerator", 1}, {"denominator", 400}}
            };
            theOne = ps.Coefficients.SingleOrDefault(x => x.PieceKey == 1000000);
            ps.Coefficients.Remove(theOne);
            ps.Coefficients.Add(apiParam2);
            await HandleTestAsync(ps, blockIndex);
            var pieceKeyChangedFee = await calculateTxCostStrategy.GetCostAsync(chainContext, size);
            pieceKeyChangedFee.ShouldBe(9813_6250_0000);
        }

        [Fact]
        public async Task Cpu_Token_Fee_Calculate_After_Update_Piecewise_Function_Test()
        {
            await InitializeCoefficientAsync();
            var calculateCpuCostStrategy = Application.ServiceProvider.GetRequiredService<ICalculateReadCostStrategy>();
            var ps = await GetCoefficientByType(FeeTypeEnum.Read);
            var apiParam = new CalculateFeeCoefficient
            {
                FeeType = FeeTypeEnum.Read,
                FunctionType = CalculateFunctionTypeEnum.Liner,
                PieceKey = 10,
                CoefficientDic = {{"numerator", 1}, {"denominator", 4}}
            };
            var blockIndex = new BlockIndex();
            IChainContext chainContext = new ChainContext
            {
                BlockHash = blockIndex.BlockHash,
                BlockHeight = blockIndex.BlockHeight
            };
            var theOne = ps.Coefficients.SingleOrDefault(x => x.PieceKey == 10);
            ps.Coefficients.Remove(theOne);
            ps.Coefficients.Add(apiParam);
            await HandleTestAsync(ps, blockIndex);
            var size = 10;
            var updatedFee1 = await calculateCpuCostStrategy.GetCostAsync(chainContext, size);
            updatedFee1.ShouldBe(2_5000_0000);

            var apiParam2 = new CalculateFeeCoefficient
            {
                FeeType = FeeTypeEnum.Read,
                FunctionType = CalculateFunctionTypeEnum.Liner,
                PieceKey = 10,
                CoefficientDic = {{"numerator", 1}, {"denominator", 2}}
            };
            theOne = ps.Coefficients.SingleOrDefault(x => x.PieceKey == 10);
            ps.Coefficients.Remove(theOne);
            ps.Coefficients.Add(apiParam2);
            await HandleTestAsync(ps, blockIndex);
            var updatedFee2 = await calculateCpuCostStrategy.GetCostAsync(chainContext, size);
            updatedFee2.ShouldBe(500000000);
        }

        [Fact]
        public async Task Ram_Token_Fee_Calculate_After_Update_Piecewise_Function_Test()
        {
            await InitializeCoefficientAsync();
            var calculateRamCostStrategy = Application.ServiceProvider.GetRequiredService<ICalculateWriteCostStrategy>();
            var ps = await GetCoefficientByType(FeeTypeEnum.Write);

            var apiParam = new CalculateFeeCoefficient
            {
                FeeType = FeeTypeEnum.Write,
                FunctionType = CalculateFunctionTypeEnum.Liner,
                PieceKey = 10,
                CoefficientDic = {{"numerator", 1}, {"denominator", 2}}
            };
            var blockIndex = new BlockIndex();
            IChainContext chainContext = new ChainContext
            {
                BlockHash = blockIndex.BlockHash,
                BlockHeight = blockIndex.BlockHeight
            };
            var theOne = ps.Coefficients.SingleOrDefault(x => x.PieceKey == 10);
            ps.Coefficients.Remove(theOne);
            ps.Coefficients.Add(apiParam);
            await HandleTestAsync(ps, blockIndex);
            var size = 10;
            var updatedFee1 = await calculateRamCostStrategy.GetCostAsync(chainContext, size);
            updatedFee1.ShouldBe(500000000);

            var apiParam2 = new CalculateFeeCoefficient
            {
                FeeType = FeeTypeEnum.Write,
                FunctionType = CalculateFunctionTypeEnum.Liner,
                PieceKey = 50,
                CoefficientDic = {{"numerator", 1}, {"denominator", 2}}
            };
            theOne = ps.Coefficients.SingleOrDefault(x => x.PieceKey == 10);
            ps.Coefficients.Remove(theOne);
            ps.Coefficients.Add(apiParam2);

            await HandleTestAsync(ps, blockIndex);
            var size2 = 50;
            var updatedFee2 = await calculateRamCostStrategy.GetCostAsync(null, size2);
            updatedFee2.ShouldBe(2500000000);
        }

        [Fact]
        public async Task Sto_Token_Fee_Calculate_After_Update_Piecewise_Function_Test()
        {
            await InitializeCoefficientAsync();
            var calculateStoCostStrategy = Application.ServiceProvider.GetRequiredService<ICalculateStorageCostStrategy>();
            var ps = await GetCoefficientByType(FeeTypeEnum.Storage);
            var apiParam = new CalculateFeeCoefficient
            {
                FeeType = FeeTypeEnum.Storage,
                FunctionType = CalculateFunctionTypeEnum.Liner,
                PieceKey = 1000000,
                CoefficientDic = {{"numerator", 1}, {"denominator", 400}}
            };
            var blockIndex = new BlockIndex();
            IChainContext chainContext = new ChainContext
            {
                BlockHash = blockIndex.BlockHash,
                BlockHeight = blockIndex.BlockHeight
            };
            var theOne = ps.Coefficients.SingleOrDefault(x => x.PieceKey == 1000000);
            ps.Coefficients.Remove(theOne);
            ps.Coefficients.Add(apiParam);
            await HandleTestAsync(ps, blockIndex);
            var size = 10000;
            var updatedFee = await calculateStoCostStrategy.GetCostAsync(chainContext, size);
            updatedFee.ShouldBe(25_0000_0000);
        }

        [Fact]
        public async Task Net_Token_Fee_Calculate_After_Update_Piecewise_Function_Test()
        {
            await InitializeCoefficientAsync();
            var calculateNetCostStrategy = Application.ServiceProvider.GetRequiredService<ICalculateTrafficCostStrategy>();
            var ps = await GetCoefficientByType(FeeTypeEnum.Traffic);
            var apiParam = new CalculateFeeCoefficient
            {
                FeeType = FeeTypeEnum.Traffic,
                FunctionType = CalculateFunctionTypeEnum.Liner,
                PieceKey = 1000000,
                CoefficientDic = {{"numerator", 1}, {"denominator", 400}}
            };
            var blockIndex = new BlockIndex();
            IChainContext chainContext = new ChainContext
            {
                BlockHash = blockIndex.BlockHash,
                BlockHeight = blockIndex.BlockHeight
            };
            var theOne = ps.Coefficients.SingleOrDefault(x => x.PieceKey == 1000000);
            ps.Coefficients.Remove(theOne);
            ps.Coefficients.Add(apiParam);
            await HandleTestAsync(ps, blockIndex);
            var size = 10000;
            var updatedFee = await calculateNetCostStrategy.GetCostAsync(chainContext, size);
            updatedFee.ShouldBe(25_0000_0000);
        }

        private async Task HandleTestAsync(CalculateFeeCoefficientsOfType param, BlockIndex blockIndex)
        {
            var firstData = param.Coefficients.First();
            var selectedStrategy = firstData.FeeType switch
            {
                FeeTypeEnum.Tx => (ICalculateCostStrategy) Application.ServiceProvider
                    .GetRequiredService<ICalculateTxCostStrategy>(),
                FeeTypeEnum.Read => Application.ServiceProvider.GetRequiredService<ICalculateReadCostStrategy>(),
                FeeTypeEnum.Write => Application.ServiceProvider.GetRequiredService<ICalculateWriteCostStrategy>(),
                FeeTypeEnum.Storage => Application.ServiceProvider.GetRequiredService<ICalculateStorageCostStrategy>(),
                FeeTypeEnum.Traffic => Application.ServiceProvider.GetRequiredService<ICalculateTrafficCostStrategy>(),
                _ => null
            };

            if (selectedStrategy == null)
                return;

            var calculateWayList = new List<ICalculateWay>();
            foreach (var coefficient in param.Coefficients)
            {
                var paramDic = coefficient.CoefficientDic.ToDictionary(x => x.Key.ToLower(), x => x.Value);
                var calculateWay = coefficient.FunctionType switch
                {
                    CalculateFunctionTypeEnum.Liner => (ICalculateWay) new LinerCalculateWay(),
                    CalculateFunctionTypeEnum.Power => new PowerCalculateWay(),
                    _ => null
                };

                if (calculateWay == null)
                    continue;
                calculateWay.PieceKey = coefficient.PieceKey;
                calculateWay.InitParameter(paramDic);
                calculateWayList.Add(calculateWay);
            }

            if (calculateWayList.Any())
                selectedStrategy.AddAlgorithm(blockIndex, calculateWayList);

            await Task.CompletedTask;
        }
    }
}