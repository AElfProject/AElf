using System.Threading.Tasks;
using AElf.Kernel;
using AElf.Kernel.SmartContract.Application;
using AElf.Types;
using Google.Protobuf.WellKnownTypes;
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
            var parameters0 = await TokenContractStub.GetCalculateFeeCoefficientOfDeveloper.CallAsync(new SInt32Value
            {
                Value = 0
            });
            parameters0.ShouldNotBeNull();
            var parameters1 = await TokenContractStub.GetCalculateFeeCoefficientOfDeveloper.CallAsync(new SInt32Value
            {
                Value = 1
            });
            parameters1.ShouldNotBeNull();
            var parameters2 = await TokenContractStub.GetCalculateFeeCoefficientOfDeveloper.CallAsync(new SInt32Value
            {
                Value = 2
            });
            parameters2.ShouldNotBeNull();
            var parameters3 = await TokenContractStub.GetCalculateFeeCoefficientOfDeveloper.CallAsync(new SInt32Value
            {
                Value = 3
            });
            parameters3.ShouldNotBeNull();
            var parameters4 = await TokenContractStub.GetCalculateFeeCoefficientOfUser.CallAsync(new Empty());
            parameters4.ShouldNotBeNull();
        }
        [Fact]
        public async Task Tx_Token_Fee_Calculate_After_Update_Piecewise_Function_Test()
        {
            var calculateTxCostStrategy = Application.ServiceProvider.GetRequiredService<ICalculateTxCostStrategy>();
            var apiParam = new LinerCoefficientForUser
            {
                PieceKey = 1000000,
                Denominator = 400,
                Numerator = 1
            };
            var result = (await TokenContractStub.UpdateLinerAlgorithmForUser.SendAsync(apiParam)).TransactionResult;
            result.Status.ShouldBe(TransactionResultStatus.Mined);
            var param = new CalculateFeeCoefficient
            {
                FeeType = FeeTypeEnum.Tx,
                FunctionType = CalculateFunctionTypeEnum.Liner,
                PieceKey = 1000000,
                CoefficientDic = { {"numerator","1"},{"denominator","400"}}
            };
            
            await HandleTestAsync(param, null, null);
            var size = 10000;
            var updatedFee =  await calculateTxCostStrategy.GetCost(null,size);
            updatedFee.ShouldBe(25_0000_0000);
            
            
        }
        [Fact]
        public async Task Cpu_Token_Fee_Calculate_After_Update_Piecewise_Function_Test()
        {
            var calculateCpuCostStrategy = Application.ServiceProvider.GetRequiredService<ICalculateCpuCostStrategy>();
            var apiParam = new LinerCoefficientForUser
            {
                PieceKey = 10,
                Denominator = 4,
                Numerator = 1
            };
            var result = (await TokenContractStub.UpdateLinerAlgorithmForUser.SendAsync(apiParam)).TransactionResult;
            result.Status.ShouldBe(TransactionResultStatus.Mined);
            var param = new CalculateFeeCoefficient
            {
                FeeType = FeeTypeEnum.Cpu,
                FunctionType = CalculateFunctionTypeEnum.Liner,
                PieceKey = 10,
                CoefficientDic = { {"numerator","1"},{"denominator","2"}}
            };
            
            await HandleTestAsync(param, null, null);
            var size = 10;
            var updatedFee =  await calculateCpuCostStrategy.GetCost(null,size);
            updatedFee.ShouldBe(500000000);
            var param2 = new CalculateFeeCoefficient
            {
                FeeType = FeeTypeEnum.Cpu,
                FunctionType = CalculateFunctionTypeEnum.Liner,
                PieceKey = 10,
            };
            await HandleTestAsync(param2, null, null, 50);
            var size2 = 50;
            var updatedFee2 =  await calculateCpuCostStrategy.GetCost(null,size2);
            updatedFee2.ShouldBe(2500000000);
        }
        [Fact]
        public async Task Ram_Token_Fee_Calculate_After_Update_Piecewise_Function_Test()
        {
            var calculateRamCostStrategy = Application.ServiceProvider.GetRequiredService<ICalculateRamCostStrategy>();
            var apiParam = new LinerCoefficientForUser
            {
                Denominator = 400,
                Numerator = 1
            };
            var result = (await TokenContractStub.UpdateLinerAlgorithmForUser.SendAsync(apiParam)).TransactionResult;
            result.Status.ShouldBe(TransactionResultStatus.Mined);
            var param = new CalculateFeeCoefficient
            {
                FeeType = FeeTypeEnum.Tx,
                FunctionType = CalculateFunctionTypeEnum.Liner,
                PieceKey = 1000000,
                CoefficientDic = { {"numerator","1"},{"denominator","400"}}
            };
            
            await HandleTestAsync(param, null, null);
            var size = 10000;
            var updatedFee =  await calculateRamCostStrategy.GetCost(null,size);
            updatedFee.ShouldBe(25_0000_0000);
            
            
        }
        [Fact]
        public async Task Sto_Token_Fee_Calculate_After_Update_Piecewise_Function_Test()
        {
            var calculateStoCostStrategy = Application.ServiceProvider.GetRequiredService<ICalculateStoCostStrategy>();
            var apiParam = new LinerCoefficientForUser
            {
                Denominator = 400,
                Numerator = 1
            };
            var result = (await TokenContractStub.UpdateLinerAlgorithmForUser.SendAsync(apiParam)).TransactionResult;
            result.Status.ShouldBe(TransactionResultStatus.Mined);
            var param = new CalculateFeeCoefficient
            {
                FeeType = FeeTypeEnum.Tx,
                FunctionType = CalculateFunctionTypeEnum.Liner,
                PieceKey = 1000000,
                CoefficientDic = { {"numerator","1"},{"denominator","400"}}
            };
            
            await HandleTestAsync(param, null, null);
            var size = 10000;
            var updatedFee =  await calculateStoCostStrategy.GetCost(null,size);
            updatedFee.ShouldBe(25_0000_0000);
            
            
        }
        [Fact]
        public async Task Net_Token_Fee_Calculate_After_Update_Piecewise_Function_Test()
        {
            var calculateNetCostStrategy = Application.ServiceProvider.GetRequiredService<ICalculateNetCostStrategy>();
            var apiParam = new LinerCoefficientForUser
            {
                Denominator = 400,
                Numerator = 1
            };
            var result = (await TokenContractStub.UpdateLinerAlgorithmForUser.SendAsync(apiParam)).TransactionResult;
            result.Status.ShouldBe(TransactionResultStatus.Mined);
            var param = new CalculateFeeCoefficient
            {
                FeeType = FeeTypeEnum.Tx,
                FunctionType = CalculateFunctionTypeEnum.Liner,
                PieceKey = 1000000,
                CoefficientDic = { {"numerator","1"},{"denominator","400"}}
            };
            
            await HandleTestAsync(param, null, null);
            var size = 10000;
            var updatedFee =  await calculateNetCostStrategy.GetCost(null,size);
            updatedFee.ShouldBe(25_0000_0000);
            
            
        }
        
        private async Task HandleTestAsync(CalculateFeeCoefficient param, BlockIndex blockIndex, IChainContext chain,
            int newPieceKey = 0)
        {
            ICalculateCostStrategy selectedStrategy = null;
            switch (param.FeeType)
            {
                case FeeTypeEnum.Tx:
                    selectedStrategy = Application.ServiceProvider.GetRequiredService<ICalculateTxCostStrategy>();
                    break;
                case FeeTypeEnum.Cpu:
                    selectedStrategy = Application.ServiceProvider.GetRequiredService<ICalculateCpuCostStrategy>();
                    break;
                case FeeTypeEnum.Ram:
                    selectedStrategy = Application.ServiceProvider.GetRequiredService<ICalculateRamCostStrategy>();
                    break;
                case FeeTypeEnum.Sto:
                    selectedStrategy = Application.ServiceProvider.GetRequiredService<ICalculateStoCostStrategy>();
                    break;
                case FeeTypeEnum.Net:
                    selectedStrategy = Application.ServiceProvider.GetRequiredService<ICalculateNetCostStrategy>();
                    break;
            }

            if (selectedStrategy == null)
                return;
            
            
            if(newPieceKey > 0)
                await selectedStrategy.ChangeAlgorithmPieceKey(chain, blockIndex, param.PieceKey,newPieceKey);
            else
            {
                var pieceKey = param.PieceKey;
                var paramDic = param.CoefficientDic;
                await selectedStrategy.ModifyAlgorithm(chain, blockIndex, pieceKey, paramDic);
                
            }
        }
        
        
    }
}