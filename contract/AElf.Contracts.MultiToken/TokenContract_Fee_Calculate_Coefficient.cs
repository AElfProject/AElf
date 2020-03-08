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
            // when restart, initialize fee calculate coefficient cache
            InitialCoefficientsAboutCharging();
            Assert(!State.Initialized.Value, "Already initialized.");

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
            Assert(input.Coefficients.FeeTokenType != (int) FeeTypeEnum.Tx, "Invalid fee type.");
            UpdateCoefficients(input);
            return new Empty();
        }

        public override Empty UpdateCoefficientsForSender(UpdateCoefficientsInput input)
        {
            if (input == null)
                return new Empty();
            AssertUserFeeController();
            input.Coefficients.FeeTokenType = (int) FeeTypeEnum.Tx; // The only possible for now.
            UpdateCoefficients(input);
            return new Empty();
        }

        private void UpdateCoefficients(UpdateCoefficientsInput input)
        {
            var feeType = input.Coefficients.FeeTokenType;
            var currentAllCoefficients = State.AllCalculateFeeCoefficients.Value;
            // Coefficients only for this fee type.
            var currentCoefficients = currentAllCoefficients.Value.SingleOrDefault(x =>
                x.FeeTokenType == feeType);
            Assert(currentCoefficients != null, "Specific fee type not existed before.");
            var inputPieceCoefficientsList = input.Coefficients.PieceCoefficientsList;
            var currentPieceCoefficientList = currentCoefficients.PieceCoefficientsList;
            var inputPieceCount = input.PieceNumbers.Count;
            Assert(inputPieceCount == inputPieceCoefficientsList.Count,
                "Piece numbers not match.");
            AssertInputValidOrderForPiece(input.PieceNumbers); // valid order for piece count
            foreach (var coefficients in inputPieceCoefficientsList)
                AssertValidCoefficient(coefficients);
            AssertInputValidOrderForPiece(
                inputPieceCoefficientsList.Select(x => x.Value[1])); //valid order for piece key
            // ReSharper disable once PossibleNullReferenceException
            for (var i = 0; i < inputPieceCount; i++)
            {
                Assert(currentPieceCoefficientList.Count >= input.PieceNumbers[i],
                    "Piece number exceeded.");
                var pieceIndex = input.PieceNumbers[i].Sub(1);
                var pieceCoefficients = inputPieceCoefficientsList[i];
                currentPieceCoefficientList[pieceIndex] = pieceCoefficients;
            }

            var startIndex = input.PieceNumbers[0].Sub(1);
            var endIndex = input.PieceNumbers[inputPieceCount.Sub(1)];
            if (startIndex > 0)
                Assert(currentPieceCoefficientList[startIndex - 1].Value[1] <
                       currentPieceCoefficientList[startIndex].Value[1]); // order piece key

            if (endIndex < currentPieceCoefficientList.Count - 1)
                Assert(currentPieceCoefficientList[endIndex].Value[1] <
                       currentPieceCoefficientList[endIndex + 1].Value[1]); // order piece key
            State.AllCalculateFeeCoefficients.Value = currentAllCoefficients;

            Context.Fire(new CalculateFeeAlgorithmUpdated
            {
                AllTypeFeeCoefficients = currentAllCoefficients
            });
        }

        private void AssertInputValidOrderForPiece(IEnumerable<int> pieceList)
        {
            var isValidOrder = true;
            int pre = -1;
            foreach (var pieceCount in pieceList)
            {
                if (pieceCount <= 0 || pre >= pieceCount)
                {
                    isValidOrder = false;
                    break;
                }

                pre = pieceCount;
            }

            Assert(isValidOrder, " input invalid piece count");
        }

        private void AssertValidCoefficient(CalculateFeePieceCoefficients coefficients)
        {
            var count = coefficients.Value.Count;
            Assert(count > 0 &&
                   (count - 1) % 3 == 0, "invalid coefficient num");
            for (var i = 1; i < count;)
            {
                Assert(coefficients.Value[i] >= 0 && coefficients.Value[i + 1] >= 0 && coefficients.Value[i + 2] > 0,
                    "invalid coefficient");
                i += 3;
            }
        }

        /// <summary>
        /// Initialize coefficients of every type of tokens supporting charging fee.
        /// Currently for acs1, charge primary token, like ELF;
        /// for acs8, charge resource tokens including READ, STO, WRITE, TRAFFIC.
        /// </summary>
        private void InitialCoefficientsAboutCharging()
        {
            var allCalculateFeeCoefficients = State.AllCalculateFeeCoefficients.Value;
            if (allCalculateFeeCoefficients == null)
                allCalculateFeeCoefficients = new AllCalculateFeeCoefficients();
            if (allCalculateFeeCoefficients.Value.All(x => x.FeeTokenType != (int) FeeTypeEnum.Read))
                allCalculateFeeCoefficients.Value.Add(GetReadFeeInitialCoefficient());
            if (allCalculateFeeCoefficients.Value.All(x => x.FeeTokenType != (int) FeeTypeEnum.Storage))
                allCalculateFeeCoefficients.Value.Add(GetStorageFeeInitialCoefficient());
            if (allCalculateFeeCoefficients.Value.All(x => x.FeeTokenType != (int) FeeTypeEnum.Write))
                allCalculateFeeCoefficients.Value.Add(GetWriteFeeInitialCoefficient());
            if (allCalculateFeeCoefficients.Value.All(x => x.FeeTokenType != (int) FeeTypeEnum.Traffic))
                allCalculateFeeCoefficients.Value.Add(GetTrafficFeeInitialCoefficient());
            if (allCalculateFeeCoefficients.Value.All(x => x.FeeTokenType != (int) FeeTypeEnum.Tx))
                allCalculateFeeCoefficients.Value.Add(GetTxFeeInitialCoefficient());
            State.AllCalculateFeeCoefficients.Value = allCalculateFeeCoefficients;

            Context.Fire(new CalculateFeeAlgorithmUpdated
            {
                AllTypeFeeCoefficients = allCalculateFeeCoefficients,
            });
        }

        private CalculateFeeCoefficients GetReadFeeInitialCoefficient()
        {
            return new CalculateFeeCoefficients
            {
                FeeTokenType = (int) FeeTypeEnum.Read,
                PieceCoefficientsList =
                {
                    new CalculateFeePieceCoefficients
                    {
                        // Liner function on the interval [0, 10],    x/8 + 1000/100000000 
                        Value =
                        {
                            10,
                            1, 1, 8, 0, 1000, 100000000 
                        }
                    },
                    new CalculateFeePieceCoefficients
                    {
                        // Liner function on the interval (10, 100]   x/4 
                        Value =
                        {
                            100,
                            1, 1, 4
                        }
                    },
                    new CalculateFeePieceCoefficients
                    {
                        // Power function on the interval (100, +∞)       25/16 * x^2 + 1 /4 * x 
                        Value =
                        {
                            int.MaxValue,
                            2, 25, 16, 1, 1, 4
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
                        // Liner function on the interval [0, 1000000]  1/4 *x + 1000/100000000 
                        Value =
                        {
                            1000000,
                            1, 1, 4, 0, 1000, 100000000 
                        }
                    },
                    new CalculateFeePieceCoefficients
                    {
                        // Power function on the interval (1000000, +∞)    x^2 * 1/20000 + 1/64 * x 
                        Value =
                        {
                            int.MaxValue,
                            2, 1, 20000, 1, 1, 64
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
                        // Liner function on the interval [0, 10]    1/8 *x + 10000/100000000 
                        Value =
                        {
                            10,
                            1, 1, 8, 0, 10000, 100000000 
                        }
                    },
                    new CalculateFeePieceCoefficients
                    {
                        // Liner function on the interval (10, 100]  1/4 *x 
                        Value =
                        {
                            100,
                            1, 1, 4
                        }
                    },
                    new CalculateFeePieceCoefficients
                    {
                        // Power function on the interval (100, +∞)  1/4 *x + 25/16 * x^2
                        Value =
                        {
                            int.MaxValue,
                            1, 1, 4, 2, 25, 16
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
                        // Liner function on the interval [0, 1000000]    1/64 *x + 10000/100000000 
                        Value =
                        {
                            1000000,
                            1, 1, 64, 0, 10000, 100000000 
                        }
                    },
                    new CalculateFeePieceCoefficients
                    {
                        // Power function on the interval (1000000, +∞)  1/64 * x + x^2 /20000
                        Value =
                        {
                            int.MaxValue,
                            1, 1, 64, 2, 1, 20000
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
                        // Liner function on the interval [0, 1000000]   1/800 *x + 10000/100000000 
                        Value =
                        {
                            1000000,
                            1, 1, 800, 0, 10000, 100000000 
                        }
                    },
                    new CalculateFeePieceCoefficients
                    {
                        // Power function on the interval (1000000, ∞)  1/ 800* x + x^2 /10000
                        Value =
                        {
                            int.MaxValue,
                            1, 1, 800, 2, 1, 10000
                        }
                    }
                }
            };
        }
    }
}