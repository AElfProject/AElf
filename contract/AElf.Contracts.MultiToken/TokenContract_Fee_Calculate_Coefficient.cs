using System.Linq;
using AElf.Sdk.CSharp;
using Google.Protobuf.WellKnownTypes;

namespace AElf.Contracts.MultiToken
{
    public partial class TokenContract
    {
        public override Empty Initialize(InitializeInput input)
        {
            Assert(!State.Initialized.Value, "MultiToken has been initialized");
            InitialParameters();
            foreach (var pair in input.ResourceAmount)
            {
                State.ResourceAmount[pair.Key] = pair.Value;
            }
            State.Initialized.Value = true;
            return new Empty();
        }

        public override Empty UpdateCoefficientFromContract(CoefficientFromContract coefficientInput)
        {
            if (coefficientInput == null)
                return new Empty();
            Assert(Context.Sender == State.ControllerForDeveloperFee.Value.RootController, "no permission");
            var coefficientInfoInState = State.CalculateCoefficientOfContract[coefficientInput.FeeType];
            if (coefficientInfoInState == null)
                return new Empty();
            var coefficient = coefficientInput.Coefficient;
            var funcCoefficient =
                coefficientInfoInState.Coefficients.SingleOrDefault(x => x.PieceKey == coefficient.PieceKey);
            Assert(funcCoefficient != null, $"piece key:{coefficient.PieceKey} does not exist");
            if (!IsModifiedDbData(coefficientInput.Coefficient, funcCoefficient)) return new Empty();
            State.CalculateCoefficientOfContract[coefficientInput.FeeType] = coefficientInfoInState;
            Context.Fire(new NoticeUpdateCalculateFeeAlgorithm
            {
                AllCoefficient = coefficientInfoInState
            });
            return new Empty();
        }

        public override Empty UpdateCoefficientFromSender(CoefficientFromSender coefficientInput)
        {
            if (coefficientInput == null)
                return new Empty();
            Assert(Context.Sender == State.ControllerForUserFee.Value.RootController, "no permission");
            var coefficientInfoInState = State.CalculateCoefficientOfSender.Value;
            if (coefficientInfoInState == null)
            {
                return new Empty();
            }

            var funcCoefficient =
                coefficientInfoInState.Coefficients.SingleOrDefault(x => x.PieceKey == coefficientInput.PieceKey);
            Assert(funcCoefficient != null, $"piece key:{coefficientInput.PieceKey} does not exist");

            if (!IsModifiedDbData(coefficientInput, funcCoefficient)) return new Empty();
            State.CalculateCoefficientOfSender.Value = coefficientInfoInState;
            Context.Fire(new NoticeUpdateCalculateFeeAlgorithm
            {
                AllCoefficient = coefficientInfoInState
            });
            return new Empty();
        }

        private bool IsModifiedDbData(CoefficientFromSender coefficientInput, CalculateFeeCoefficient funcCoefficient)
        {
            bool isChanged;
            if (coefficientInput.IsChangePieceKey)
            {
                isChanged = ChangeFeePieceKey(coefficientInput, funcCoefficient);
            }
            else if (coefficientInput.IsLiner)
            {
                isChanged = UpdateLinerAlgorithm(coefficientInput, funcCoefficient);
            }
            else
            {
                isChanged = UpdatePowerAlgorithm(coefficientInput, funcCoefficient);
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
            dbData.CoefficientDic[nameof(coefficient.ConstantValue).ToLower()] = coefficient.ConstantValue;
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
            if (State.CalculateCoefficientOfContract[FeeTypeEnum.Read] == null)
                State.CalculateCoefficientOfContract[FeeTypeEnum.Read] = GetReadFeeInitialCoefficient();
            if (State.CalculateCoefficientOfContract[FeeTypeEnum.Storage] == null)
                State.CalculateCoefficientOfContract[FeeTypeEnum.Storage] = GetStoFeeInitialCoefficient();
            if (State.CalculateCoefficientOfContract[FeeTypeEnum.Write] == null)
                State.CalculateCoefficientOfContract[FeeTypeEnum.Write] = GetWriteFeeInitialCoefficient();
            if (State.CalculateCoefficientOfContract[FeeTypeEnum.Traffic] == null)
                State.CalculateCoefficientOfContract[FeeTypeEnum.Traffic] = GetNetFeeInitialCoefficient();
            if (State.CalculateCoefficientOfSender.Value == null)
                State.CalculateCoefficientOfSender.Value = GetTxFeeInitialCoefficient();
        }

        private CalculateFeeCoefficientsOfType GetReadFeeInitialCoefficient()
        {
            var totalParameter = new CalculateFeeCoefficientsOfType();
            var readFeeParameter1 = new CalculateFeeCoefficient
            {
                FeeType = FeeTypeEnum.Read,
                FunctionType = CalculateFunctionTypeEnum.Liner,
                PieceKey = 10,
                CoefficientDic = {{"numerator", 1}, {"denominator", 8}, {"constantValue".ToLower(), 1000}}
            };
            var readFeeParameter2 = new CalculateFeeCoefficient
            {
                FeeType = FeeTypeEnum.Read,
                FunctionType = CalculateFunctionTypeEnum.Liner,
                PieceKey = 100,
                CoefficientDic = {{"numerator", 1}, {"denominator", 4}}
            };
            var readFeeParameter3 = new CalculateFeeCoefficient
            {
                FeeType = FeeTypeEnum.Read,
                FunctionType = CalculateFunctionTypeEnum.Power,
                PieceKey = int.MaxValue,
                CoefficientDic =
                {
                    {"numerator", 1}, {"denominator", 4}, {"power", 2}, {"changeSpanBase".ToLower(), 5}, {"weight", 250},
                    {"weightBase".ToLower(), 40}
                }
            };
            totalParameter.Coefficients.Add(readFeeParameter1);
            totalParameter.Coefficients.Add(readFeeParameter2);
            totalParameter.Coefficients.Add(readFeeParameter3);
            return totalParameter;
        }

        private CalculateFeeCoefficientsOfType GetStoFeeInitialCoefficient()
        {
            var totalParameter = new CalculateFeeCoefficientsOfType();
            var stoFeeParameter1 = new CalculateFeeCoefficient
            {
                FeeType = FeeTypeEnum.Storage,
                FunctionType = CalculateFunctionTypeEnum.Liner,
                PieceKey = 1000000,
                CoefficientDic = {{"numerator", 1}, {"denominator", 4}, {"constantValue".ToLower(), 1000}}
            };
            var stoFeeParameter2 = new CalculateFeeCoefficient
            {
                FeeType = FeeTypeEnum.Storage,
                FunctionType = CalculateFunctionTypeEnum.Power,
                PieceKey = int.MaxValue,
                CoefficientDic =
                {
                    {"numerator", 1}, {"denominator", 64}, {"power", 2}, {"changeSpanBase".ToLower(), 100}, {"weight", 250},
                    {"weightBase".ToLower(), 500}
                }
            };
            totalParameter.Coefficients.Add(stoFeeParameter1);
            totalParameter.Coefficients.Add(stoFeeParameter2);
            return totalParameter;
        }

        private CalculateFeeCoefficientsOfType GetWriteFeeInitialCoefficient()
        {
            var totalParameter = new CalculateFeeCoefficientsOfType();
            var writeFeeParameter1 = new CalculateFeeCoefficient
            {
                FeeType = FeeTypeEnum.Write,
                FunctionType = CalculateFunctionTypeEnum.Liner,
                PieceKey = 10,
                CoefficientDic = {{"numerator", 1}, {"denominator", 8}, {"constantValue".ToLower(), 10000}}
            };
            var writeFeeParameter2 = new CalculateFeeCoefficient
            {
                FeeType = FeeTypeEnum.Write,
                FunctionType = CalculateFunctionTypeEnum.Liner,
                PieceKey = 100,
                CoefficientDic = {{"numerator", 1}, {"denominator", 4}}
            };
            var writeFeeParameter3 = new CalculateFeeCoefficient
            {
                FeeType = FeeTypeEnum.Write,
                FunctionType = CalculateFunctionTypeEnum.Power,
                PieceKey = int.MaxValue,
                CoefficientDic =
                {
                    {"numerator", 1}, {"denominator", 4}, {"power", 2}, {"changeSpanBase".ToLower(), 2}, {"weight", 250},
                    {"weightBase".ToLower(), 40}
                }
            };
            totalParameter.Coefficients.Add(writeFeeParameter1);
            totalParameter.Coefficients.Add(writeFeeParameter2);
            totalParameter.Coefficients.Add(writeFeeParameter3);
            return totalParameter;
        }

        private CalculateFeeCoefficientsOfType GetNetFeeInitialCoefficient()
        {
            var totalParameter = new CalculateFeeCoefficientsOfType();
            var netFeeParameter1 = new CalculateFeeCoefficient
            {
                FeeType = FeeTypeEnum.Traffic,
                FunctionType = CalculateFunctionTypeEnum.Liner,
                PieceKey = 1000000,
                CoefficientDic = {{"numerator", 1}, {"denominator", 64}, {"constantValue".ToLower(), 10000}}
            };
            var netFeeParameter2 = new CalculateFeeCoefficient
            {
                FeeType = FeeTypeEnum.Traffic,
                FunctionType = CalculateFunctionTypeEnum.Power,
                PieceKey = int.MaxValue,
                CoefficientDic =
                {
                    {"numerator", 1}, {"denominator", 64}, {"power", 2}, {"changeSpanBase".ToLower(), 100}, {"weight", 250},
                    {"weightBase".ToLower(), 500}
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
                CoefficientDic = {{"numerator", 1}, {"denominator", 800}, {"constantValue".ToLower(), 10000}}
            };
            var txFeeParameter2 = new CalculateFeeCoefficient
            {
                FeeType = FeeTypeEnum.Tx,
                FunctionType = CalculateFunctionTypeEnum.Power,
                PieceKey = int.MaxValue,
                CoefficientDic =
                {
                    {"numerator", 1}, {"denominator", 800}, {"power", 2}, {"changeSpanBase".ToLower(), 100}, {"weight", 1},
                    {"weightBase".ToLower(), 1}
                }
            };
            totalParameter.Coefficients.Add(txFeeParameter1);
            totalParameter.Coefficients.Add(txFeeParameter2);
            return totalParameter;
        }
    }
}