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
            var feeType = (int) coefficientInput.FeeType;
            Assert(feeType != (int) FeeTypeEnum.Tx, "coefficient does not exist");
            var allTokenCoefficient = State.CalculateCoefficientOfTokenType.Value;
            var tokenCoefficientList = allTokenCoefficient.CoefficientListOfTokenType;
            var coefficientInfoInState = tokenCoefficientList.FirstOrDefault(x => x.FeeTokenType == feeType);
            Assert(coefficientInfoInState != null, "coefficient does not exist");
            var coefficient = coefficientInput.Coefficient;
            UpdateCoefficientOfOneToken(coefficient, coefficientInfoInState);
            State.CalculateCoefficientOfTokenType.Value = allTokenCoefficient;
            Context.Fire(new NoticeUpdateCalculateFeeAlgorithm
            {
                CoefficientOfAllType = allTokenCoefficient
            });
            return new Empty();
        }

        public override Empty UpdateCoefficientFromSender(CoefficientFromSender coefficientInput)
        {
            if (coefficientInput == null)
                return new Empty();
            AssertUserFeeController();
            var allTokenCoefficient = State.CalculateCoefficientOfTokenType.Value;
            var tokenCoefficientList = allTokenCoefficient.CoefficientListOfTokenType;
            var coefficientInfoInState =
                tokenCoefficientList.FirstOrDefault(x => x.FeeTokenType == (int) FeeTypeEnum.Tx);
            Assert(coefficientInfoInState != null, "coefficient does not exist");
            UpdateCoefficientOfOneToken(coefficientInput, coefficientInfoInState);
            State.CalculateCoefficientOfTokenType.Value = allTokenCoefficient;
            Context.Fire(new NoticeUpdateCalculateFeeAlgorithm
            {
                CoefficientOfAllType = allTokenCoefficient
            });
            return new Empty();
        }

        private void UpdateCoefficientOfOneToken(CoefficientFromSender coefficientInput,
            CalculateFeeCoefficientsOfType coefficientInfoInState)
        {
            var funcCoefficient =
                coefficientInfoInState.Coefficients.SingleOrDefault(x =>
                    x.CoefficientArray[0] == coefficientInput.PieceKey);
            Assert(funcCoefficient != null, $"piece key:{coefficientInput.PieceKey} does not exist");
            AssertValidCoefficients(coefficientInput, funcCoefficient);
            if (coefficientInput.CoefficientArray[0] != funcCoefficient.CoefficientArray[0])
            {
                var oldPieceKey = funcCoefficient.CoefficientArray[0];
                var newPieceKey = coefficientInput.CoefficientArray[0];
                var pieceKeyArray = coefficientInfoInState.Coefficients.Select(x => x.CoefficientArray[0]);
                Assert(IsValidNewPieceKey(newPieceKey, oldPieceKey, pieceKeyArray), "invalid piece key");
            }

            funcCoefficient.CoefficientArray.Clear();
            funcCoefficient.CoefficientArray.AddRange(coefficientInput.CoefficientArray);
        }

        private void AssertValidCoefficients(CoefficientFromSender coefficientInput,
            CalculateFeeCoefficient funcCoefficient)
        {
            IList<int> targetArray = coefficientInput.CoefficientArray;
            IList<int> currentArray = funcCoefficient.CoefficientArray;
            Assert(targetArray.Count == currentArray.Count, "invalid coefficient input");
            Assert(
                targetArray.Any(x => x > 0) || (targetArray.Count == 4 && targetArray[0] > 0 && targetArray[1] > 0 &&
                                                targetArray[2] > 0 && targetArray[3] == 0),
                "invalid coefficient input");
        }

        private bool IsValidNewPieceKey(int newPieceKey, int oldPieceKey, IEnumerable<int> orderPieceKeys)
        {
            if (orderPieceKeys.Contains(newPieceKey))
                return false;
            var pieceKeys = orderPieceKeys as int[] ?? orderPieceKeys.ToArray();
            var index = pieceKeys.Count(x => x < oldPieceKey);
            var prePieceKeyIndex = index - 1 >= 0 ? index - 1 : -1;
            var postPieceKeyIndex = index + 1 <= pieceKeys.Length - 1 ? index + 1 : -1;
            if (prePieceKeyIndex > 0 && pieceKeys[prePieceKeyIndex] > newPieceKey)
                return false;
            if (postPieceKeyIndex > 0 && pieceKeys[postPieceKeyIndex] < newPieceKey)
                return false;
            return true;
        }

        private void InitialParameters()
        {
            if (State.CalculateCoefficientOfTokenType.Value == null)
                State.CalculateCoefficientOfTokenType.Value = new CalculateFeeCoefficientOfAllTokenType();
            var allTokenCoefficient = State.CalculateCoefficientOfTokenType.Value;
            var tokenCoefficientList = allTokenCoefficient.CoefficientListOfTokenType;
            if (tokenCoefficientList.All(x => x.FeeTokenType != (int) FeeTypeEnum.Read))
                tokenCoefficientList.Add(GetReadFeeInitialCoefficient());
            if (tokenCoefficientList.All(x => x.FeeTokenType != (int) FeeTypeEnum.Storage))
                tokenCoefficientList.Add(GetStoFeeInitialCoefficient());
            if (tokenCoefficientList.All(x => x.FeeTokenType != (int) FeeTypeEnum.Write))
                tokenCoefficientList.Add(GetWriteFeeInitialCoefficient());
            if (tokenCoefficientList.All(x => x.FeeTokenType != (int) FeeTypeEnum.Traffic))
                tokenCoefficientList.Add(GetTrafficFeeInitialCoefficient());
            if (tokenCoefficientList.All(x => x.FeeTokenType != (int) FeeTypeEnum.Tx))
                tokenCoefficientList.Add(GetTxFeeInitialCoefficient());
            State.CalculateCoefficientOfTokenType.Value = allTokenCoefficient;
            Context.Fire(new NoticeUpdateCalculateFeeAlgorithm
            {
                CoefficientOfAllType = allTokenCoefficient,
            });
        }

        private CalculateFeeCoefficientsOfType GetReadFeeInitialCoefficient()
        {
            var totalParameter = new CalculateFeeCoefficientsOfType
            {
                FeeTokenType = (int) FeeTypeEnum.Read
            };
            var readFeeParameter1 = new CalculateFeeCoefficient();
            // pieceKey , numerator, denominator, constantValue
            int[] coefficient1 = {10, 1, 8, 1000};
            readFeeParameter1.CoefficientArray.AddRange(coefficient1);
            var readFeeParameter2 = new CalculateFeeCoefficient();
            int[] coefficient2 = {100, 1, 4, 0};
            readFeeParameter2.CoefficientArray.AddRange(coefficient2);
            var readFeeParameter3 = new CalculateFeeCoefficient();
            // pieceKey , numerator, denominator, power, changeSpanBase, weight, weightBase
            int[] coefficient3 = {int.MaxValue, 1, 4, 2, 5, 250, 40};
            readFeeParameter3.CoefficientArray.AddRange(coefficient3);
            totalParameter.Coefficients.Add(readFeeParameter1);
            totalParameter.Coefficients.Add(readFeeParameter2);
            totalParameter.Coefficients.Add(readFeeParameter3);
            return totalParameter;
        }

        private CalculateFeeCoefficientsOfType GetStoFeeInitialCoefficient()
        {
            var totalParameter = new CalculateFeeCoefficientsOfType
            {
                FeeTokenType = (int) FeeTypeEnum.Storage
            };
            var stoFeeParameter1 = new CalculateFeeCoefficient();
            int[] coefficient1 = {1000000, 1, 4, 1000};
            stoFeeParameter1.CoefficientArray.AddRange(coefficient1);
            var stoFeeParameter2 = new CalculateFeeCoefficient();
            int[] coefficient2 = {int.MaxValue, 1, 64, 2, 100, 250, 500};
            stoFeeParameter2.CoefficientArray.AddRange(coefficient2);
            totalParameter.Coefficients.Add(stoFeeParameter1);
            totalParameter.Coefficients.Add(stoFeeParameter2);
            return totalParameter;
        }

        private CalculateFeeCoefficientsOfType GetWriteFeeInitialCoefficient()
        {
            var totalParameter = new CalculateFeeCoefficientsOfType
            {
                FeeTokenType = (int) FeeTypeEnum.Write
            };
            var writeFeeParameter1 = new CalculateFeeCoefficient();
            int[] coefficient1 = {10, 1, 8, 10000};
            writeFeeParameter1.CoefficientArray.AddRange(coefficient1);
            var writeFeeParameter2 = new CalculateFeeCoefficient();
            int[] coefficient2 = {100, 1, 4, 0};
            writeFeeParameter2.CoefficientArray.AddRange(coefficient2);
            var writeFeeParameter3 = new CalculateFeeCoefficient();
            int[] coefficient3 = {int.MaxValue, 1, 4, 2, 2, 250, 40};
            writeFeeParameter3.CoefficientArray.AddRange(coefficient3);
            totalParameter.Coefficients.Add(writeFeeParameter1);
            totalParameter.Coefficients.Add(writeFeeParameter2);
            totalParameter.Coefficients.Add(writeFeeParameter3);
            return totalParameter;
        }

        private CalculateFeeCoefficientsOfType GetTrafficFeeInitialCoefficient()
        {
            var totalParameter = new CalculateFeeCoefficientsOfType
            {
                FeeTokenType = (int) FeeTypeEnum.Traffic
            };
            var netFeeParameter1 = new CalculateFeeCoefficient();
            int[] coefficient1 = {1000000, 1, 64, 10000};
            netFeeParameter1.CoefficientArray.AddRange(coefficient1);
            var netFeeParameter2 = new CalculateFeeCoefficient();
            int[] coefficient2 = {int.MaxValue, 1, 64, 2, 100, 250, 500};
            netFeeParameter2.CoefficientArray.AddRange(coefficient2);
            totalParameter.Coefficients.Add(netFeeParameter1);
            totalParameter.Coefficients.Add(netFeeParameter2);
            return totalParameter;
        }

        private CalculateFeeCoefficientsOfType GetTxFeeInitialCoefficient()
        {
            var totalParameter = new CalculateFeeCoefficientsOfType
            {
                FeeTokenType = (int) FeeTypeEnum.Tx
            };
            var txFeeParameter1 = new CalculateFeeCoefficient();
            int[] coefficient1 = {1000000, 1, 800, 10000};
            txFeeParameter1.CoefficientArray.AddRange(coefficient1);
            var txFeeParameter2 = new CalculateFeeCoefficient();
            int[] coefficient2 = {int.MaxValue, 1, 800, 2, 100, 1, 1};
            txFeeParameter2.CoefficientArray.AddRange(coefficient2);
            totalParameter.Coefficients.Add(txFeeParameter1);
            totalParameter.Coefficients.Add(txFeeParameter2);
            return totalParameter;
        }
    }
}