using System.Collections.Generic;
using System.Linq;
using AElf.CSharp.Core;
using AElf.Sdk.CSharp;
using Google.Protobuf.WellKnownTypes;

namespace AElf.Contracts.MultiToken;

public partial class TokenContract
{
    /// <summary>
    ///     Can only update one token at one time.
    /// </summary>
    /// <param name="input"></param>
    /// <returns></returns>
    public override Empty UpdateCoefficientsForContract(UpdateCoefficientsInput input)
    {
        Assert(input.Coefficients != null, "Invalid input coefficients.");
        Assert(input.Coefficients.FeeTokenType != (int)FeeTypeEnum.Tx, "Invalid fee type.");
        AssertDeveloperFeeController();
        UpdateCoefficients(input);
        return new Empty();
    }

    public override Empty UpdateCoefficientsForSender(UpdateCoefficientsInput input)
    {
        Assert(input.Coefficients != null, "Invalid input coefficients.");
        AssertUserFeeController();
        input.Coefficients.FeeTokenType = (int)FeeTypeEnum.Tx; // The only possible for now.
        UpdateCoefficients(input);
        return new Empty();
    }

    private void UpdateCoefficients(UpdateCoefficientsInput input)
    {
        var feeType = input.Coefficients.FeeTokenType;
        var currentAllCoefficients = State.AllCalculateFeeCoefficients.Value;

        // Get coefficients for specific fee type.
        var currentCoefficients = currentAllCoefficients.Value.SingleOrDefault(x =>
            x.FeeTokenType == feeType);
        Assert(currentCoefficients != null, "Specific fee type not existed before.");

        var inputPieceCoefficientsList = input.Coefficients.PieceCoefficientsList;
        // ReSharper disable once PossibleNullReferenceException
        var currentPieceCoefficientList = currentCoefficients.PieceCoefficientsList;

        var inputPieceCount = input.PieceNumbers.Count;
        Assert(inputPieceCount == inputPieceCoefficientsList.Count,
            "Piece numbers not match.");

        foreach (var coefficients in inputPieceCoefficientsList)
            AssertCoefficientsValid(coefficients);

        for (var i = 0; i < inputPieceCount; i++)
        {
            Assert(currentPieceCoefficientList.Count >= input.PieceNumbers[i],
                "Piece number exceeded.");
            var pieceIndex = input.PieceNumbers[i].Sub(1);
            var pieceCoefficients = inputPieceCoefficientsList[i];
            currentPieceCoefficientList[pieceIndex] = pieceCoefficients;
        }

        AssertPieceUpperBoundsIsInOrder(currentPieceCoefficientList);

        State.AllCalculateFeeCoefficients.Value = currentAllCoefficients;

        Context.Fire(new CalculateFeeAlgorithmUpdated
        {
            AllTypeFeeCoefficients = currentAllCoefficients
        });
    }

    private void AssertCoefficientsValid(CalculateFeePieceCoefficients coefficients)
    {
        // Assert the count should be (3n + 1), n >= 1.
        var count = coefficients.Value.Count;
        Assert(count > 0 && (count - 1) % 3 == 0, "Coefficients count should be (3n + 1), n >= 1.");

        // Assert every unit. one [(B / C) * x ^ A] means one unit.
        for (var i = 1; i < count; i += 3)
        {
            var power = coefficients.Value[i];
            var divisor = coefficients.Value[i + 1];
            var dividend = coefficients.Value[i + 2];
            Assert(power >= 0 && divisor >= 0 && dividend > 0, "Invalid coefficient.");
        }
    }

    private void AssertPieceUpperBoundsIsInOrder(
        IReadOnlyCollection<CalculateFeePieceCoefficients> calculateFeePieceCoefficientsList)
    {
        // No same piece upper bound.
        Assert(!calculateFeePieceCoefficientsList.GroupBy(i => i.Value[0]).Any(g => g.Count() > 1),
            "Piece upper bounds contains same elements.");

        var pieceUpperBounds = calculateFeePieceCoefficientsList.Select(l => l.Value[0]).ToList();
        var orderedEnumerable = pieceUpperBounds.OrderBy(i => i).ToList();
        for (var i = 0; i < calculateFeePieceCoefficientsList.Count; i++)
            Assert(pieceUpperBounds[i] == orderedEnumerable[i], "Piece upper bounds not in order.");
    }

    /// <summary>
    ///     Initialize coefficients of every type of tokens supporting charging fee.
    ///     Currently for acs1, charge primary token, like ELF;
    ///     for acs8, charge resource tokens including READ, STO, WRITE, TRAFFIC.
    /// </summary>
    public override Empty InitialCoefficients(Empty input)
    {
        Assert(State.AllCalculateFeeCoefficients.Value == null, "Coefficient already initialized");
        var allCalculateFeeCoefficients = new AllCalculateFeeCoefficients();
        if (allCalculateFeeCoefficients.Value.All(x => x.FeeTokenType != (int)FeeTypeEnum.Read))
            allCalculateFeeCoefficients.Value.Add(GetReadFeeInitialCoefficient());
        if (allCalculateFeeCoefficients.Value.All(x => x.FeeTokenType != (int)FeeTypeEnum.Storage))
            allCalculateFeeCoefficients.Value.Add(GetStorageFeeInitialCoefficient());
        if (allCalculateFeeCoefficients.Value.All(x => x.FeeTokenType != (int)FeeTypeEnum.Write))
            allCalculateFeeCoefficients.Value.Add(GetWriteFeeInitialCoefficient());
        if (allCalculateFeeCoefficients.Value.All(x => x.FeeTokenType != (int)FeeTypeEnum.Traffic))
            allCalculateFeeCoefficients.Value.Add(GetTrafficFeeInitialCoefficient());
        if (allCalculateFeeCoefficients.Value.All(x => x.FeeTokenType != (int)FeeTypeEnum.Tx))
            allCalculateFeeCoefficients.Value.Add(GetTxFeeInitialCoefficient());
        State.AllCalculateFeeCoefficients.Value = allCalculateFeeCoefficients;

        Context.Fire(new CalculateFeeAlgorithmUpdated
        {
            AllTypeFeeCoefficients = allCalculateFeeCoefficients
        });

        return new Empty();
    }

    private CalculateFeeCoefficients GetReadFeeInitialCoefficient()
    {
        return new CalculateFeeCoefficients
        {
            FeeTokenType = (int)FeeTypeEnum.Read,
            PieceCoefficientsList =
            {
                new CalculateFeePieceCoefficients
                {
                    // Interval [0, 10]: x/8 + 1 / 100000
                    Value =
                    {
                        10,
                        1, 1, 8,
                        0, 1, 100000
                    }
                },
                new CalculateFeePieceCoefficients
                {
                    // Interval (10, 100]: x/4 
                    Value =
                    {
                        100,
                        1, 1, 4
                    }
                },
                new CalculateFeePieceCoefficients
                {
                    // Interval (100, +∞): 25 / 16 * x^2 + x / 4
                    Value =
                    {
                        int.MaxValue,
                        2, 25, 16,
                        1, 1, 4
                    }
                }
            }
        };
    }

    private CalculateFeeCoefficients GetStorageFeeInitialCoefficient()
    {
        return new CalculateFeeCoefficients
        {
            FeeTokenType = (int)FeeTypeEnum.Storage,
            PieceCoefficientsList =
            {
                new CalculateFeePieceCoefficients
                {
                    // Interval [0, 1000000]: x / 4 + 1 / 100000
                    Value =
                    {
                        1000000,
                        1, 1, 4,
                        0, 1, 100000
                    }
                },
                new CalculateFeePieceCoefficients
                {
                    // Interval (1000000, +∞): x ^ 2 / 20000 + x / 64
                    Value =
                    {
                        int.MaxValue,
                        2, 1, 20000,
                        1, 1, 64
                    }
                }
            }
        };
    }

    private CalculateFeeCoefficients GetWriteFeeInitialCoefficient()
    {
        return new CalculateFeeCoefficients
        {
            FeeTokenType = (int)FeeTypeEnum.Write,
            PieceCoefficientsList =
            {
                new CalculateFeePieceCoefficients
                {
                    // Interval [0, 10]: x / 8 + 1 / 10000
                    Value =
                    {
                        10,
                        1, 1, 8,
                        0, 1, 10000
                    }
                },
                new CalculateFeePieceCoefficients
                {
                    // Interval (10, 100]: x / 4
                    Value =
                    {
                        100,
                        1, 1, 4
                    }
                },
                new CalculateFeePieceCoefficients
                {
                    // Interval (100, +∞): x / 4 + x^2 * 25 / 16
                    Value =
                    {
                        int.MaxValue,
                        1, 1, 4,
                        2, 25, 16
                    }
                }
            }
        };
    }

    private CalculateFeeCoefficients GetTrafficFeeInitialCoefficient()
    {
        return new CalculateFeeCoefficients
        {
            FeeTokenType = (int)FeeTypeEnum.Traffic,
            PieceCoefficientsList =
            {
                new CalculateFeePieceCoefficients
                {
                    // Interval [0, 1000000]: x / 64 + 1 / 10000
                    Value =
                    {
                        1000000,
                        1, 1, 64,
                        0, 1, 10000
                    }
                },
                new CalculateFeePieceCoefficients
                {
                    // Interval (1000000, +∞): x / 64 + x^2 / 20000
                    Value =
                    {
                        int.MaxValue,
                        1, 1, 64,
                        2, 1, 20000
                    }
                }
            }
        };
    }

    private CalculateFeeCoefficients GetTxFeeInitialCoefficient()
    {
        return new CalculateFeeCoefficients
        {
            FeeTokenType = (int)FeeTypeEnum.Tx,
            PieceCoefficientsList =
            {
                new CalculateFeePieceCoefficients
                {
                    // Interval [0, 1000000]: x / 800 + 1 / 10000
                    Value =
                    {
                        1000000,
                        1, 1, 800,
                        0, 1, 10000
                    }
                },
                new CalculateFeePieceCoefficients
                {
                    // Interval (1000000, 5000000): x / 80
                    Value =
                    {
                        5000000,
                        1, 1, 80
                    }
                },
                new CalculateFeePieceCoefficients
                {
                    // Interval (5000000, ∞): x / 80 + x^2 / 100000
                    Value =
                    {
                        int.MaxValue,
                        1, 1, 80,
                        2, 1, 100000
                    }
                }
            }
        };
    }
}