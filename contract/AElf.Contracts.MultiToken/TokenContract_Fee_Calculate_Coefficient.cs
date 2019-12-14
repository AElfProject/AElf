using System.Linq;
using AElf.Sdk.CSharp;
using Google.Protobuf.WellKnownTypes;

namespace AElf.Contracts.MultiToken
{
    public partial class TokenContract
    {
        public override Empty InitializeCoefficient(Empty empty)
        {
            AssertIsAuthorized();
            InitialParameters();
            return new Empty();
        }

        public override Empty UpdateCoefficientFromContract(CoefficientFromContract coeInput)
        {
            if (coeInput == null)
                return new Empty();
            AssertIsAuthorized();
            var dataInDb = State.CalculateCoefficientOfContract[coeInput.FeeType];
            if (dataInDb == null)
                return new Empty();
            var coefficient = coeInput.Coefficient;
            var theOne = dataInDb.Coefficients.SingleOrDefault(x => x.PieceKey == coefficient.PieceKey);
            if (theOne == null)
                return new Empty();
            if (!IsModifiedDbData(coeInput.Coefficient, theOne)) return new Empty();
            Context.Fire(new NoticeUpdateCalculateFeeAlgorithm
            {
                AllCoefficient = dataInDb
            });
            return new Empty();
        }

        public override Empty UpdateCoefficientFromSender(CoefficientFromSender coeInput)
        {
            if (coeInput == null)
                return new Empty();
            AssertIsAuthorized();
            var dataInDb = State.CalculateCoefficientOfSender.Value;
            if (dataInDb == null)
            {
                return new Empty();
            }

            var theOne = dataInDb.Coefficients.SingleOrDefault(x => x.PieceKey == coeInput.PieceKey);
            if (theOne == null)
            {
                return new Empty();
            }

            if (!IsModifiedDbData(coeInput, theOne)) return new Empty();
            Context.Fire(new NoticeUpdateCalculateFeeAlgorithm
            {
                AllCoefficient = dataInDb
            });
            return new Empty();
        }

        private bool IsModifiedDbData(CoefficientFromSender coeInput, CalculateFeeCoefficient theOne)
        {
            bool isChanged;
            if (coeInput.IsChangePieceKey)
            {
                isChanged = ChangeFeePieceKey(coeInput, theOne);
            }
            else if (coeInput.IsLiner)
            {
                isChanged = UpdateLinerAlgorithm(coeInput, theOne);
            }
            else
            {
                isChanged = UpdatePowerAlgorithm(coeInput, theOne);
            }

            return isChanged;
        }

        private bool UpdateLinerAlgorithm(CoefficientFromSender sender, CalculateFeeCoefficient dbData)
        {
            var coefficient = sender.LinerCoefficient;
            if (coefficient.Denominator <= 0)
                return false;
            if (coefficient.Numerator < 0)
                return false;
            if (coefficient.ConstantValue < 0)
                return false;
            dbData.CoefficientDic[nameof(coefficient.Denominator).ToLower()] = coefficient.Denominator;
            dbData.CoefficientDic[nameof(coefficient.Numerator).ToLower()] = coefficient.Numerator;
            dbData.CoefficientDic[nameof(coefficient.ConstantValue).ToLower()] = coefficient.ConstantValue;
            return true;
        }

        private bool UpdatePowerAlgorithm(CoefficientFromSender sender, CalculateFeeCoefficient dbData)
        {
            var coefficient = sender.PowerCoefficient;
            if (coefficient.Denominator <= 0)
                return false;
            if (coefficient.Numerator < 0)
                return false;
            if (coefficient.Weight <= 0)
                return false;
            if (coefficient.WeightBase <= 0)
                return false;
            if (coefficient.ChangeSpanBase <= 0)
                return false;
            dbData.CoefficientDic[nameof(coefficient.Denominator).ToLower()] = coefficient.Denominator;
            dbData.CoefficientDic[nameof(coefficient.Numerator).ToLower()] = coefficient.Numerator;
            dbData.CoefficientDic[nameof(coefficient.Weight).ToLower()] = coefficient.Weight;
            dbData.CoefficientDic[nameof(coefficient.WeightBase).ToLower()] = coefficient.WeightBase;
            dbData.CoefficientDic[nameof(coefficient.ChangeSpanBase).ToLower()] = coefficient.ChangeSpanBase;
            return true;
        }

        private bool ChangeFeePieceKey(CoefficientFromSender coefficient, CalculateFeeCoefficient dbData)
        {
            var newPieceKey = coefficient.NewPieceKeyCoefficient.NewPieceKey;
            if (newPieceKey == coefficient.PieceKey || newPieceKey <= 0)
                return false;
            dbData.PieceKey = newPieceKey;
            return true;
        }

        private void InitialParameters()
        {
            State.CalculateCoefficientOfContract[FeeTypeEnum.Cpu] = GetCpuFeeInitialCoefficient();
            State.CalculateCoefficientOfContract[FeeTypeEnum.Sto] = GetStoFeeInitialCoefficient();
            State.CalculateCoefficientOfContract[FeeTypeEnum.Ram] = GetRamFeeInitialCoefficient();
            State.CalculateCoefficientOfContract[FeeTypeEnum.Net] = GetNetFeeInitialCoefficient();
            State.CalculateCoefficientOfSender.Value = GetTxFeeInitialCoefficient();
        }
                private CalculateFeeCoefficientsOfType GetCpuFeeInitialCoefficient()
        {
            var totalParameter = new CalculateFeeCoefficientsOfType();
            var cpuFeeParameter1 = new CalculateFeeCoefficient
            {
                FeeType = FeeTypeEnum.Cpu,
                FunctionType = CalculateFunctionTypeEnum.Liner,
                PieceKey = 10,
                CoefficientDic = { {"numerator", 1},{"denominator", 8},{"constantvalue", 1000}}
                
            };
            var cpuFeeParameter2 = new CalculateFeeCoefficient
            {
                FeeType = FeeTypeEnum.Cpu,
                FunctionType = CalculateFunctionTypeEnum.Liner,
                PieceKey = 100,
                CoefficientDic = { {"numerator", 1},{"denominator", 4}}
            };
            var cpuFeeParameter3 = new CalculateFeeCoefficient
            {
                FeeType = FeeTypeEnum.Cpu,
                FunctionType = CalculateFunctionTypeEnum.Power,
                PieceKey = int.MaxValue,
                CoefficientDic =
                {
                    {"numerator", 1},{"denominator", 4},{"power", 2},{"changespanbase", 5},{"weight", 250},
                    {"weightbase", 40}
                }
            };
            totalParameter.Coefficients.Add(cpuFeeParameter1);
            totalParameter.Coefficients.Add(cpuFeeParameter2);
            totalParameter.Coefficients.Add(cpuFeeParameter3);
            return totalParameter;
        }
        private CalculateFeeCoefficientsOfType GetStoFeeInitialCoefficient()
        {
            var totalParameter = new CalculateFeeCoefficientsOfType();
            var stoFeeParameter1 = new CalculateFeeCoefficient
            {
                FeeType = FeeTypeEnum.Sto,
                FunctionType = CalculateFunctionTypeEnum.Liner,
                PieceKey = 1000000,
                CoefficientDic = { {"numerator", 1},{"denominator", 4},{"constantvalue", 1000}}
                
            };
            var stoFeeParameter2 = new CalculateFeeCoefficient
            {
                FeeType = FeeTypeEnum.Sto,
                FunctionType = CalculateFunctionTypeEnum.Power,
                PieceKey = int.MaxValue,
                CoefficientDic =
                {
                    {"numerator", 1},{"denominator", 64},{"power", 2},{"changespanbase", 100},{"weight", 250},
                    {"weightbase", 500}
                }
            };
            totalParameter.Coefficients.Add(stoFeeParameter1);
            totalParameter.Coefficients.Add(stoFeeParameter2);
            return totalParameter;
        }
        private CalculateFeeCoefficientsOfType GetRamFeeInitialCoefficient()
        {
            var totalParameter = new CalculateFeeCoefficientsOfType();
            var ramFeeParameter1 = new CalculateFeeCoefficient
            {
                FeeType = FeeTypeEnum.Ram,
                FunctionType = CalculateFunctionTypeEnum.Liner,
                PieceKey = 10,
                CoefficientDic = { {"numerator", 1},{"denominator", 8},{"constantvalue", 10000}}
                
            };
            var ramFeeParameter2 = new CalculateFeeCoefficient
            {
                FeeType = FeeTypeEnum.Ram,
                FunctionType = CalculateFunctionTypeEnum.Liner,
                PieceKey = 100,
                CoefficientDic = { {"numerator", 1},{"denominator", 4}}
            };
            var ramFeeParameter3 = new CalculateFeeCoefficient
            {
                FeeType = FeeTypeEnum.Ram,
                FunctionType = CalculateFunctionTypeEnum.Power,
                PieceKey = int.MaxValue,
                CoefficientDic =
                {
                    {"numerator", 1},{"denominator", 4},{"power", 2},{"changespanbase", 2},{"weight", 250},
                    {"weightbase", 40}
                }
            };
            totalParameter.Coefficients.Add(ramFeeParameter1);
            totalParameter.Coefficients.Add(ramFeeParameter2);
            totalParameter.Coefficients.Add(ramFeeParameter3);
            return totalParameter;
        }
        private CalculateFeeCoefficientsOfType GetNetFeeInitialCoefficient()
        {
            var totalParameter = new CalculateFeeCoefficientsOfType();
            var netFeeParameter1 = new CalculateFeeCoefficient
            {
                FeeType = FeeTypeEnum.Net,
                FunctionType = CalculateFunctionTypeEnum.Liner,
                PieceKey = 1000000,
                CoefficientDic = { {"numerator", 1},{"denominator", 64},{"constantvalue", 10000}}
                
            };
            var netFeeParameter2 = new CalculateFeeCoefficient
            {
                FeeType = FeeTypeEnum.Net,
                FunctionType = CalculateFunctionTypeEnum.Power,
                PieceKey = int.MaxValue,
                CoefficientDic =
                {
                    {"numerator", 1},{"denominator", 64},{"power", 2},{"changespanbase", 100},{"weight", 250},
                    {"weightbase", 500}
                }
            };
            totalParameter.Coefficients.Add(netFeeParameter1);
            totalParameter.Coefficients.Add(netFeeParameter2);
            return totalParameter;
        }
        private CalculateFeeCoefficientsOfType GetTxFeeInitialCoefficient()
        {
            var totalParameter = new CalculateFeeCoefficientsOfType();
            var txFeeParameter1 = new CalculateFeeCoefficient
            {
                FeeType = FeeTypeEnum.Tx,
                FunctionType = CalculateFunctionTypeEnum.Liner,
                PieceKey = 1000000,
                CoefficientDic = { {"numerator", 1},{"denominator", 800},{"constantvalue", 10000}}
            };
            var txFeeParameter2 = new CalculateFeeCoefficient
            {
                FeeType = FeeTypeEnum.Tx,
                FunctionType = CalculateFunctionTypeEnum.Power,
                PieceKey = int.MaxValue,
                CoefficientDic =
                {
                    {"numerator", 1},{"denominator", 800},{"power", 2},{"changespanbase", 100},{"weight", 1},
                    {"weightbase", 1}
                }
            };
            totalParameter.Coefficients.Add(txFeeParameter1);
            totalParameter.Coefficients.Add(txFeeParameter2);
            return totalParameter;
        }
    }
}