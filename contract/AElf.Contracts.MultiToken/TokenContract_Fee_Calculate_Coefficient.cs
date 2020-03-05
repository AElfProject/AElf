using System.Collections.Generic;
using System.Linq;
using AElf.Sdk.CSharp;
using Google.Protobuf.Collections;
using Google.Protobuf.WellKnownTypes;

namespace AElf.Contracts.MultiToken
{
    public partial class TokenContract
    {
        public override Empty Initialize(InitializeInput input)
        {
            Assert(!State.Initialized.Value, "Already initialized.");
            InitialCoefficientsAboutCharging();

            // Initialize resources usage status.
            foreach (var pair in input.ResourceAmount)
            {
                State.ResourceAmount[pair.Key] = pair.Value;
            }

            State.Initialized.Value = true;
            return new Empty();
        }

        /// <summary>
        /// Can only update one token at one time.
        /// </summary>
        /// <param name="input"></param>
        /// <returns></returns>
        public override Empty UpdateCoefficientsForContract(UpdateCoefficientsInput input)
        {
            if (input == null)
                return new Empty();
            AssertDeveloperFeeController();
            UpdateCoefficients(input);
            return new Empty();
        }

        public override Empty UpdateCoefficientsForSender(UpdateCoefficientsInput input)
        {
            if (input == null)
                return new Empty();
            AssertUserFeeController();
            UpdateCoefficients(input);
            return new Empty();
        }

        private void UpdateCoefficients(UpdateCoefficientsInput input)
        {
            var feeType = input.Coefficients.FeeTokenType;
            Assert(feeType != (int) FeeTypeEnum.Tx, "Invalid fee type.");
            var currentAllCoefficients = State.AllCalculateFeeCoefficients.Value;
            // Coefficients only for this fee type.
            var currentCoefficients = currentAllCoefficients.Value.SingleOrDefault(x =>
                x.FeeTokenType == feeType);
            Assert(currentCoefficients != null, "Specific fee type not existed before.");
            Assert(input.PieceNumbers.Count == input.Coefficients.PieceCoefficientsList.Count,
                "Piece numbers not match.");

            // ReSharper disable once PossibleNullReferenceException
            for (var i = 0; i < input.PieceNumbers.Count; i++)
            {
                Assert(currentCoefficients.PieceCoefficientsList.Count >= input.PieceNumbers[i],
                    "Piece number exceeded.");
                var pieceIndex = input.PieceNumbers[i].Sub(1);
                var pieceCoefficients = input.Coefficients.PieceCoefficientsList[i];
                Assert(pieceCoefficients.Value[0] == 0 || pieceCoefficients.Value[0] == 1,
                    "Invalid piece-wise function type.");
                currentCoefficients.PieceCoefficientsList[pieceIndex] = pieceCoefficients;
            }

            State.AllCalculateFeeCoefficients.Value = currentAllCoefficients;

            Context.Fire(new CalculateFeeAlgorithmUpdated
            {
                FeeCoefficients = currentCoefficients
            });
        }

        /// <summary>
        /// Initialize coefficients of every type of tokens supporting charging fee.
        /// Currently for acs1, charge primary token, like ELF;
        /// for acs8, charge resource tokens including READ, STO, WRITE, TRAFFIC.
        /// </summary>
        private void InitialCoefficientsAboutCharging()
        {
            State.AllCalculateFeeCoefficients.Value = new AllCalculateFeeCoefficients
            {
                Value =
                {
                    new List<CalculateFeeCoefficients>
                    {
                        GetReadFeeInitialCoefficient(),
                        GetStorageFeeInitialCoefficient(),
                        GetWriteFeeInitialCoefficient(),
                        GetTrafficFeeInitialCoefficient(),
                        GetTxFeeInitialCoefficient()
                    }
                }
            };

            foreach (var coefficients in State.AllCalculateFeeCoefficients.Value.Value)
            {
                Context.Fire(new CalculateFeeAlgorithmUpdated
                {
                    FeeCoefficients = coefficients,
                    IsUpdateAll = true
                });
            }
        }

        private CalculateFeeCoefficients GetReadFeeInitialCoefficient()
        {
            return new CalculateFeeCoefficients
            {
                FeeTokenType = (int) FeeTypeEnum.Write,
                PieceCoefficientsList =
                {
                    new CalculateFeePieceCoefficients
                    {
                        // Liner function on the interval [0, 10]
                        Value =
                        {
                            0, 10,
                            1, 8, 1000
                        }
                    },
                    new CalculateFeePieceCoefficients
                    {
                        // Liner function on the interval (10, 100]
                        Value =
                        {
                            0, 100,
                            1, 4, 0
                        }
                    },
                    new CalculateFeePieceCoefficients
                    {
                        // Power function on the interval (100, +∞)
                        Value =
                        {
                            1, int.MaxValue,
                            1, 4, 2, 2, 250, 40
                        }
                    }
                }
            };
        }

        private CalculateFeeCoefficients GetStorageFeeInitialCoefficient()
        {
            return new CalculateFeeCoefficients
            {
                FeeTokenType = (int) FeeTypeEnum.Storage,
                PieceCoefficientsList =
                {
                    new CalculateFeePieceCoefficients
                    {
                        // Liner function on the interval [0, 1000000]
                        Value =
                        {
                            0, 1000000,
                            1, 4, 1000
                        }
                    },
                    new CalculateFeePieceCoefficients
                    {
                        // Power function on the interval (1000000, +∞)
                        Value =
                        {
                            1, int.MaxValue,
                            1, 64, 2, 100, 250, 500
                        }
                    }
                }
            };
        }

        private CalculateFeeCoefficients GetWriteFeeInitialCoefficient()
        {
            return new CalculateFeeCoefficients
            {
                FeeTokenType = (int) FeeTypeEnum.Write,
                PieceCoefficientsList =
                {
                    new CalculateFeePieceCoefficients
                    {
                        // Liner function on the interval [0, 10]
                        Value =
                        {
                            0, 10,
                            1, 8, 10000
                        }
                    },
                    new CalculateFeePieceCoefficients
                    {
                        // Liner function on the interval (10, 100]
                        Value =
                        {
                            0, 100,
                            1, 4, 0
                        }
                    },
                    new CalculateFeePieceCoefficients
                    {
                        // Power function on the interval (100, +∞)
                        Value =
                        {
                            1, int.MaxValue,
                            1, 4, 2, 2, 250, 40
                        }
                    }
                }
            };
        }

        private CalculateFeeCoefficients GetTrafficFeeInitialCoefficient()
        {
            return new CalculateFeeCoefficients
            {
                FeeTokenType = (int) FeeTypeEnum.Traffic,
                PieceCoefficientsList =
                {
                    new CalculateFeePieceCoefficients
                    {
                        // Liner function on the interval [0, 1000000]
                        Value =
                        {
                            0, 1000000,
                            1, 64, 10000
                        }
                    },
                    new CalculateFeePieceCoefficients
                    {
                        // Power function on the interval (1000000, +∞)
                        Value =
                        {
                            1, int.MaxValue,
                            1, 64, 2, 100, 250, 500
                        }
                    }
                }
            };
        }

        private CalculateFeeCoefficients GetTxFeeInitialCoefficient()
        {
            return new CalculateFeeCoefficients
            {
                FeeTokenType = (int) FeeTypeEnum.Tx,
                PieceCoefficientsList =
                {
                    new CalculateFeePieceCoefficients
                    {
                        // Liner function on the interval [0, 1000000]
                        Value =
                        {
                            0, 1000000,
                            1, 800, 10000
                        }
                    },
                    new CalculateFeePieceCoefficients
                    {
                        // Power function on the interval (1000000, ∞)
                        Value =
                        {
                            1, int.MaxValue,
                            1, 800, 2, 100, 1, 1
                        }
                    }
                }
            };
        }

        #region Archive

        /*
       private void UpdateCoefficientOfOneToken(int pieceKey, RepeatedField<int> coefficients,
           CalculateFeeCoefficients coefficientInfoInState)
       {
           var funcCoefficient =
               coefficientInfoInState.FeeCoefficient.SingleOrDefault(x =>
                   x.CoefficientArray[0] == pieceKey);
           Assert(funcCoefficient != null, $"piece key: {pieceKey} does not exist");
           AssertValidCoefficients(coefficientInput, funcCoefficient);
           if (coefficients[0] != funcCoefficient.Coefficients[0])
           {
               var oldPieceKey = funcCoefficient.Coefficients[0];
               var newPieceKey = coefficients[0];
               var pieceKeyArray = coefficientInfoInState.FeeCoefficient.Select(x => x.Coefficients[0]);
               Assert(IsValidNewPieceKey(newPieceKey, oldPieceKey, pieceKeyArray), "invalid piece key");
           }

           funcCoefficient.Coefficients.Clear();
           funcCoefficient.Coefficients.AddRange(coefficients);
       }
       
       private void AssertValidCoefficients(CoefficientFromSender coefficientInput,
           CalculateFeeCoefficient funcCoefficient)
       {
           IList<int> targetArray = coefficientInput.Coefficients;
           IList<int> currentArray = funcCoefficient.Coefficients;
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
           if (postPieceKeyIndex > 0 && pieceKeys[prePieceKeyIndex] < newPieceKey)
               return false;
           return true;
       }
*/
        #endregion
    }
}