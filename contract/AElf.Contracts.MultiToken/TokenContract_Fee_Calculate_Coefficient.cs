using System.Collections.Generic;
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
            AssertDeveloperFeeController();
            var coefficientInfoInState = State.CalculateCoefficientOfContract.Value
                .CoefficientDicOfContract[(int) coefficientInput.FeeType];
            Assert(coefficientInfoInState != null, "coefficient does not exist");
            var coefficient = coefficientInput.Coefficient;
            var funcCoefficient =
                coefficientInfoInState.Coefficients.SingleOrDefault(x => x.CoefficientArray[0] == coefficient.PieceKey);
            Assert(funcCoefficient != null, $"piece key:{coefficient.PieceKey} does not exist");
            AssertValidCoefficients(coefficientInput.Coefficient, funcCoefficient);
            if (coefficient.CoefficientArray[0] != funcCoefficient.CoefficientArray[0])
            {
                var oldPieceKey = funcCoefficient.CoefficientArray[0];
                var newPieceKey = coefficient.CoefficientArray[0];
                var pieceKeyArray = coefficientInfoInState.Coefficients.Select(x => x.CoefficientArray[0]);
                Assert(IsValidNewPieceKey(newPieceKey, oldPieceKey, pieceKeyArray),"invalid piece key");
            }
            UpdateCoefficient(coefficientInput.Coefficient, funcCoefficient);
            State.CalculateCoefficientOfContract.Value.CoefficientDicOfContract[(int)coefficientInput.FeeType] = coefficientInfoInState;
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
            AssertUserFeeController();
            var coefficientInfoInState = State.CalculateCoefficientOfSender.Value.CoefficientOfSender;
            Assert(coefficientInfoInState != null, "coefficient does not exist");
            var funcCoefficient =
                coefficientInfoInState.Coefficients.SingleOrDefault(x => x.CoefficientArray[0] == coefficientInput.PieceKey);
            Assert(funcCoefficient != null, $"piece key:{coefficientInput.PieceKey} does not exist");
            AssertValidCoefficients(coefficientInput, funcCoefficient);
            if (coefficientInput.CoefficientArray[0] != funcCoefficient.CoefficientArray[0])
            {
                var oldPieceKey = funcCoefficient.CoefficientArray[0];
                var newPieceKey = coefficientInput.CoefficientArray[0];
                var pieceKeyArray = coefficientInfoInState.Coefficients.Select(x => x.CoefficientArray[0]);
                Assert(IsValidNewPieceKey(newPieceKey, oldPieceKey, pieceKeyArray),"invalid piece key");
            }
            UpdateCoefficient(coefficientInput, funcCoefficient);
            State.CalculateCoefficientOfSender.Value.CoefficientOfSender = coefficientInfoInState;
            Context.Fire(new NoticeUpdateCalculateFeeAlgorithm
            {
                AllCoefficient = coefficientInfoInState
            });
            return new Empty();
        }

        private void UpdateCoefficient(CoefficientFromSender coefficientInput, CalculateFeeCoefficient funcCoefficient)
        {
            funcCoefficient.CoefficientArray.Clear();
            funcCoefficient.CoefficientArray.AddRange(coefficientInput.CoefficientArray);
        }

        private void AssertValidCoefficients(CoefficientFromSender coefficientInput, CalculateFeeCoefficient funcCoefficient)
        {
            IEnumerable<int> targetArray = coefficientInput.CoefficientArray;
            IEnumerable<int> currentArray = funcCoefficient.CoefficientArray;
            Assert(targetArray.Count() == currentArray.Count(), "invalid coefficient input");
            Assert(targetArray.Any(x => x <= 0), "invalid coefficient input");
        }
        private bool IsValidNewPieceKey(int newPieceKey, int oldPieceKey, IEnumerable<int> orderPieceKeys)
        {
            var pieceKeys = orderPieceKeys as int[] ?? orderPieceKeys.ToArray();
            if (pieceKeys.Contains(newPieceKey))
                return false;
            return !pieceKeys.Any(x => x < oldPieceKey && x < newPieceKey);
        }

        private void InitialParameters()
        {
            if (State.CalculateCoefficientOfContract.Value == null)
                State.CalculateCoefficientOfContract.Value = new CalculateFeeCoefficientOfContract();
            if (State.CalculateCoefficientOfContract.Value.CoefficientDicOfContract[(int)FeeTypeEnum.Read] == null)
                State.CalculateCoefficientOfContract.Value.CoefficientDicOfContract[(int)FeeTypeEnum.Read] = GetReadFeeInitialCoefficient();
            if (State.CalculateCoefficientOfContract.Value.CoefficientDicOfContract[(int)FeeTypeEnum.Storage] == null)
                State.CalculateCoefficientOfContract.Value.CoefficientDicOfContract[(int)FeeTypeEnum.Storage] = GetStoFeeInitialCoefficient();
            if (State.CalculateCoefficientOfContract.Value.CoefficientDicOfContract[(int)FeeTypeEnum.Write] == null)
                State.CalculateCoefficientOfContract.Value.CoefficientDicOfContract[(int)FeeTypeEnum.Write] = GetWriteFeeInitialCoefficient();
            if (State.CalculateCoefficientOfContract.Value.CoefficientDicOfContract[(int)FeeTypeEnum.Traffic] == null)
                State.CalculateCoefficientOfContract.Value.CoefficientDicOfContract[(int)FeeTypeEnum.Traffic] = GetTrafficFeeInitialCoefficient();
            if (State.CalculateCoefficientOfSender.Value == null)
            {
                State.CalculateCoefficientOfSender.Value = new CalculateFeeCoefficientOfSender
                {
                    CoefficientOfSender = GetTxFeeInitialCoefficient()
                };
            }
               
        }

        private CalculateFeeCoefficientsOfType GetReadFeeInitialCoefficient()
        {
            var totalParameter = new CalculateFeeCoefficientsOfType();
            var readFeeParameter1 = new CalculateFeeCoefficient();
            int[] coefficient1 = { 10, 1, 8, 1000};
            readFeeParameter1.CoefficientArray.AddRange(coefficient1);
            // {
            //     FeeType = FeeTypeEnum.Read,
            //     FunctionType = CalculateFunctionTypeEnum.Liner,
            //     PieceKey = 10,
            //     CoefficientDic = {{"numerator", 1}, {"denominator", 8}, {"constantValue".ToLower(), 1000}}
            // };
            var readFeeParameter2 = new CalculateFeeCoefficient();
            int[] coefficient2 = {100, 1 , 4, 0 };
            readFeeParameter2.CoefficientArray.AddRange(coefficient2);
            // {
            //     FeeType = FeeTypeEnum.Read,
            //     FunctionType = CalculateFunctionTypeEnum.Liner,
            //     PieceKey = 100,
            //     CoefficientDic = {{"numerator", 1}, {"denominator", 4}}
            // };
            var readFeeParameter3 = new CalculateFeeCoefficient();
            int[] coefficient3 = { int.MaxValue, 1, 4, 2, 5, 250, 40};
            readFeeParameter3.CoefficientArray.AddRange(coefficient3);
            // {
            //     FeeType = FeeTypeEnum.Read,
            //     FunctionType = CalculateFunctionTypeEnum.Power,
            //     PieceKey = int.MaxValue,
            //     CoefficientDic =
            //     {
            //         {"numerator", 1}, {"denominator", 4}, {"power", 2}, {"changeSpanBase".ToLower(), 5}, {"weight", 250},
            //         {"weightBase".ToLower(), 40}
            //     }
            // };
            totalParameter.Coefficients.Add(readFeeParameter1);
            totalParameter.Coefficients.Add(readFeeParameter2);
            totalParameter.Coefficients.Add(readFeeParameter3);
            return totalParameter;
        }

        private CalculateFeeCoefficientsOfType GetStoFeeInitialCoefficient()
        {
            var totalParameter = new CalculateFeeCoefficientsOfType();
            var stoFeeParameter1 = new CalculateFeeCoefficient();
            int[] coefficient1 = {1000000, 1, 4, 1000 };
            stoFeeParameter1.CoefficientArray.AddRange(coefficient1);
            // {
            //     FeeType = FeeTypeEnum.Storage,
            //     FunctionType = CalculateFunctionTypeEnum.Liner,
            //     PieceKey = 1000000,
            //     CoefficientDic = {{"numerator", 1}, {"denominator", 4}, {"constantValue".ToLower(), 1000}}
            // };
            var stoFeeParameter2 = new CalculateFeeCoefficient();
            int[] coefficient2 = {int.MaxValue, 1, 64, 2, 100, 250, 500 };
            stoFeeParameter2.CoefficientArray.AddRange(coefficient2);
            // {
            //     FeeType = FeeTypeEnum.Storage,
            //     FunctionType = CalculateFunctionTypeEnum.Power,
            //     PieceKey = int.MaxValue,
            //     CoefficientDic =
            //     {
            //         {"numerator", 1}, {"denominator", 64}, {"power", 2}, {"changeSpanBase".ToLower(), 100}, {"weight", 250},
            //         {"weightBase".ToLower(), 500}
            //     }
            // };
            totalParameter.Coefficients.Add(stoFeeParameter1);
            totalParameter.Coefficients.Add(stoFeeParameter2);
            return totalParameter;
        }

        private CalculateFeeCoefficientsOfType GetWriteFeeInitialCoefficient()
        {
            var totalParameter = new CalculateFeeCoefficientsOfType();
            var writeFeeParameter1 = new CalculateFeeCoefficient();
            int[] coefficient1 = { 10, 1, 8, 10000};
            writeFeeParameter1.CoefficientArray.AddRange(coefficient1);
            // {
            //     FeeType = FeeTypeEnum.Write,
            //     FunctionType = CalculateFunctionTypeEnum.Liner,
            //     PieceKey = 10,
            //     CoefficientDic = {{"numerator", 1}, {"denominator", 8}, {"constantValue".ToLower(), 10000}}
            // };
            var writeFeeParameter2 = new CalculateFeeCoefficient();
            int[] coefficient2 = { 100, 1, 4, 0};
            writeFeeParameter2.CoefficientArray.AddRange(coefficient2);
            // {
            //     FeeType = FeeTypeEnum.Write,
            //     FunctionType = CalculateFunctionTypeEnum.Liner,
            //     PieceKey = 100,
            //     CoefficientDic = {{"numerator", 1}, {"denominator", 4}}
            // };
            var writeFeeParameter3 = new CalculateFeeCoefficient();
            int[] coefficient3 = {int.MaxValue, 1, 4, 2, 2, 250, 40 };
            writeFeeParameter3.CoefficientArray.AddRange(coefficient3);
            // {
            //     FeeType = FeeTypeEnum.Write,
            //     FunctionType = CalculateFunctionTypeEnum.Power,
            //     PieceKey = int.MaxValue,
            //     CoefficientDic =
            //     {
            //         {"numerator", 1}, {"denominator", 4}, {"power", 2}, {"changeSpanBase".ToLower(), 2}, {"weight", 250},
            //         {"weightBase".ToLower(), 40}
            //     }
            // };
            totalParameter.Coefficients.Add(writeFeeParameter1);
            totalParameter.Coefficients.Add(writeFeeParameter2);
            totalParameter.Coefficients.Add(writeFeeParameter3);
            return totalParameter;
        }

        private CalculateFeeCoefficientsOfType GetTrafficFeeInitialCoefficient()
        {
            var totalParameter = new CalculateFeeCoefficientsOfType();
            var netFeeParameter1 = new CalculateFeeCoefficient();
            int[] coefficient1 = {1000000, 1 , 64, 10000 };
            netFeeParameter1.CoefficientArray.AddRange(coefficient1);
            // {
            //     FeeType = FeeTypeEnum.Traffic,
            //     FunctionType = CalculateFunctionTypeEnum.Liner,
            //     PieceKey = 1000000,
            //     CoefficientDic = {{"numerator", 1}, {"denominator", 64}, {"constantValue".ToLower(), 10000}}
            // };
            var netFeeParameter2 = new CalculateFeeCoefficient();
            int[] coefficient2 = {int.MaxValue, 1, 64, 2, 100, 250, 500};
            netFeeParameter2.CoefficientArray.AddRange(coefficient2);
            // {
            //     FeeType = FeeTypeEnum.Traffic,
            //     FunctionType = CalculateFunctionTypeEnum.Power,
            //     PieceKey = int.MaxValue,
            //     CoefficientDic =
            //     {
            //         {"numerator", 1}, {"denominator", 64}, {"power", 2}, {"changeSpanBase".ToLower(), 100}, {"weight", 250},
            //         {"weightBase".ToLower(), 500}
            //     }
            // };
            totalParameter.Coefficients.Add(netFeeParameter1);
            totalParameter.Coefficients.Add(netFeeParameter2);
            return totalParameter;
        }

        private CalculateFeeCoefficientsOfType GetTxFeeInitialCoefficient()
        {
            var totalParameter = new CalculateFeeCoefficientsOfType();
            var txFeeParameter1 = new CalculateFeeCoefficient();
            int[] coefficient1 = { 1000000,1,800,10000 };
            txFeeParameter1.CoefficientArray.AddRange(coefficient1);
            // {
            //     FeeType = FeeTypeEnum.Tx,
            //     FunctionType = CalculateFunctionTypeEnum.Liner,
            //     PieceKey = 1000000,
            //     CoefficientDic = {{"numerator", 1}, {"denominator", 800}, {"constantValue".ToLower(), 10000}}
            // };
            var txFeeParameter2 = new CalculateFeeCoefficient();
            int[] coefficient2 = {int.MaxValue, 1, 800, 2, 100, 1, 1 };
            txFeeParameter2.CoefficientArray.AddRange(coefficient2);
            // {
            //     FeeType = FeeTypeEnum.Tx,
            //     FunctionType = CalculateFunctionTypeEnum.Power,
            //     PieceKey = int.MaxValue,
            //     CoefficientDic =
            //     {
            //         {"numerator", 1}, {"denominator", 800}, {"power", 2}, {"changeSpanBase".ToLower(), 100}, {"weight", 1},
            //         {"weightBase".ToLower(), 1}
            //     }
            // };
            totalParameter.Coefficients.Add(txFeeParameter1);
            totalParameter.Coefficients.Add(txFeeParameter2);
            return totalParameter;
        }
    }
}