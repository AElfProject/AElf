using System.Threading.Tasks;
using AElf.Kernel;
using AElf.Kernel.SmartContract.Application;
using AElf.Types;
using Microsoft.Extensions.DependencyInjection;
using Shouldly;
using Xunit;
namespace AElf.Contracts.MultiToken
{
    public partial class MultiTokenContractTests
    {
        [Fact]
        public async Task Get_Calculate_Fee_Coefficient_Function_Test()
        {
            var parameters0 = await TokenContractStub.GetCalculateFeeCoefficientByType.CallAsync(new SInt32Value
            {
                Value = 0
            });
            parameters0.ShouldNotBeNull();
            var parameters1 = await TokenContractStub.GetCalculateFeeCoefficientByType.CallAsync(new SInt32Value
            {
                Value = 1
            });
            parameters1.ShouldNotBeNull();
            var parameters2 = await TokenContractStub.GetCalculateFeeCoefficientByType.CallAsync(new SInt32Value
            {
                Value = 2
            });
            parameters2.ShouldNotBeNull();
            var parameters3 = await TokenContractStub.GetCalculateFeeCoefficientByType.CallAsync(new SInt32Value
            {
                Value = 3
            });
            parameters3.ShouldNotBeNull();
            var parameters4 = await TokenContractStub.GetCalculateFeeCoefficientByType.CallAsync(new SInt32Value
            {
                Value = 4
            });
            parameters4.ShouldNotBeNull();
        }
        [Fact]
        public async Task Token_Fee_Calculate_After_Add_Piecewise_Function_Test()
        {
            var calculateFeeService = Application.ServiceProvider.GetRequiredService<ICalculateFeeService>();
            var calculateStrategyProvider = Application.ServiceProvider.GetRequiredService<ICalculateStrategyProvider>();
            calculateFeeService.CalculateCostStrategy = calculateStrategyProvider.GetTxCalculateStrategy();
            var size = 10000;
            var fee = await calculateFeeService.CalculateFee(null,size);
            fee.ShouldBe(1250010000);
            var param = new CalculateFeeCoefficient
            {
                OperationType = AlgorithmOpCodeEnum.AddFunc,
                FunctionType = CalculateFunctionTypeEnum.Liner,
                FeeType = FeeTypeEnum.Tx,
                PieceKey = 500,
                CoefficientDic = {{"numerator", "1"}, {"denominator", "4"}}
            };
            var result = (await TokenContractStub.UpdateCalculateFeeAlgorithmParameters.SendAsync(param)).TransactionResult;
            result.Status.ShouldBe(TransactionResultStatus.Mined);
            
            await HandleTestAsync(param, null, null);
            var updatedFee =  await calculateFeeService.CalculateFee(null,size);
            updatedFee.ShouldBe(13687510000);
        }
        [Fact]
        public async Task Token_Fee_Calculate_After_Remove_Piecewise_Function_Test()
        {
            var calculateFeeService = Application.ServiceProvider.GetRequiredService<ICalculateFeeService>();
            var calculateStrategyProvider = Application.ServiceProvider.GetRequiredService<ICalculateStrategyProvider>();
            calculateFeeService.CalculateCostStrategy = calculateStrategyProvider.GetTxCalculateStrategy();
            var size = 10000;
            var param = new CalculateFeeCoefficient
            {
                OperationType = AlgorithmOpCodeEnum.DeleteFunc,
                FeeType = (int) FeeTypeEnum.Tx,
                PieceKey = int.MaxValue
            };
            var result = (await TokenContractStub.UpdateCalculateFeeAlgorithmParameters.SendAsync(param)).TransactionResult;
            result.Status.ShouldBe(TransactionResultStatus.Mined);
            var param2 = new CalculateFeeCoefficient
            {
                OperationType = AlgorithmOpCodeEnum.DeleteFunc,
                FeeType = (int) FeeTypeEnum.Tx,
                PieceKey = 1000000
            };
            await HandleTestAsync(param, null, null);
            var updatedFee1 =  await calculateFeeService.CalculateFee(null,100000000);
            updatedFee1.ShouldBe(1250_0001_0000);
            await HandleTestAsync(param2, null, null);
            var updatedFee2 =  await calculateFeeService.CalculateFee(null,size);
            updatedFee2.ShouldBe(0);
        } 
        [Fact]
        public async Task Token_Fee_Calculate_After_Update_Piecewise_Function_Test()
        {
            var calculateFeeService = Application.ServiceProvider.GetRequiredService<ICalculateFeeService>();
            var calculateStrategyProvider = Application.ServiceProvider.GetRequiredService<ICalculateStrategyProvider>();
            calculateFeeService.CalculateCostStrategy = calculateStrategyProvider.GetTxCalculateStrategy();
            var param = new CalculateFeeCoefficient
            {
                OperationType = AlgorithmOpCodeEnum.UpdateFunc,
                FeeType = (int) FeeTypeEnum.Tx,
                FunctionType = CalculateFunctionTypeEnum.Liner,
                PieceKey = 1000000,
                CoefficientDic = { {"numerator","1"},{"denominator","400"},{"pieceKey","10000"}}
            };
            var result = (await TokenContractStub.UpdateCalculateFeeAlgorithmParameters.SendAsync(param)).TransactionResult;
            result.Status.ShouldBe(TransactionResultStatus.Mined);
            
            await HandleTestAsync(param, null, null);
            var size = 10000;
            var updatedFee =  await calculateFeeService.CalculateFee(null,size);
            updatedFee.ShouldBe(25_0000_0000);
        }
        [Fact]
        public async Task Token_Fee_Calculate_Compose_Update_Piecewise_Function_Test()
        {
            var calculateFeeService = Application.ServiceProvider.GetRequiredService<ICalculateFeeService>();
            var calculateStrategyProvider = Application.ServiceProvider.GetRequiredService<ICalculateStrategyProvider>();
            calculateFeeService.CalculateCostStrategy = calculateStrategyProvider.GetTxCalculateStrategy();
            var param = new CalculateFeeCoefficient
            {
                OperationType = AlgorithmOpCodeEnum.DeleteFunc,
                FeeType = FeeTypeEnum.Tx,
                PieceKey = int.MaxValue
            };
            var param2 = new CalculateFeeCoefficient
            {
                OperationType =  AlgorithmOpCodeEnum.DeleteFunc,
                FeeType = FeeTypeEnum.Tx,
                PieceKey = 1000000
            };
            var param3 = new CalculateFeeCoefficient
            {
                OperationType = (int) AlgorithmOpCodeEnum.AddFunc,
                FeeType = (int) FeeTypeEnum.Tx,
                FunctionType = CalculateFunctionTypeEnum.Liner,
                PieceKey = 1000000,
                CoefficientDic = {{"numerator", "1"}, {"denominator", "4"}}
            };
            var param4 = new CalculateFeeCoefficient
            {
                OperationType = AlgorithmOpCodeEnum.UpdateFunc,
                FeeType = (int) FeeTypeEnum.Tx,
                FunctionType = CalculateFunctionTypeEnum.Liner,
                PieceKey = 1000000,
                CoefficientDic = {{"numerator", "1"}, {"denominator", "2"}}
            };
            await HandleTestAsync(param, null, null);
            await HandleTestAsync(param2, null, null);
            await HandleTestAsync(param3, null, null);
            await HandleTestAsync(param4, null, null);
            var size = 100000000;
            var updatedFee =  await calculateFeeService.CalculateFee(null,size);
            updatedFee.ShouldBe(50_0000_0000_0000);
        }
        private async Task HandleTestAsync(CalculateFeeCoefficient param, BlockIndex blockIndex, IChainContext chain)
        {
            var calculateFeeService = Application.ServiceProvider.GetRequiredService<ICalculateFeeService>();
            var calculateStrategyProvider = Application.ServiceProvider.GetRequiredService<ICalculateStrategyProvider>();
            var feeType = param.FeeType;
            var pieceKey = param.PieceKey;
            var funcType = param.FunctionType;
            var paramDic = param.CoefficientDic;
            var opCode = param.OperationType;
            calculateFeeService.CalculateCostStrategy =
                calculateStrategyProvider.GetCalculateStrategyByFeeType((int)feeType);
            if(calculateFeeService.CalculateCostStrategy == null)
                return;
            switch (opCode)
            {
                case AlgorithmOpCodeEnum.AddFunc:
                    await calculateFeeService.AddFeeCal(chain, blockIndex, pieceKey,
                        (int)funcType, paramDic);
                    break;
                case AlgorithmOpCodeEnum.DeleteFunc:
                    await calculateFeeService.DeleteFeeCal(chain, blockIndex, pieceKey);
                    break;
                case AlgorithmOpCodeEnum.UpdateFunc:
                    await calculateFeeService.UpdateFeeCal(chain, blockIndex, pieceKey,
                        (int)funcType, paramDic);
                    break;
            }
        }
    }
}