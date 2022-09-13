using System.Collections.Generic;
using System.Linq;

namespace AElf.Kernel;

[Trait("Category", AElfBlockchainModule)]
public class TransactionTraceExtensionsTests : AElfKernelTestBase
{
    [Theory]
    [InlineData(ExecutionStatus.Executed, true)]
    [InlineData(ExecutionStatus.Canceled, false)]
    [InlineData(ExecutionStatus.Postfailed, false)]
    [InlineData(ExecutionStatus.Prefailed, false)]
    [InlineData(ExecutionStatus.Undefined, false)]
    [InlineData(ExecutionStatus.ContractError, false)]
    [InlineData(ExecutionStatus.SystemError, false)]
    [InlineData(ExecutionStatus.ExceededMaxCallDepth, false)]
    public void IsSuccessful_Test(ExecutionStatus executionStatus, bool isSuccess)
    {
        {
            var transactionTrace = new TransactionTrace
            {
                ExecutionStatus = executionStatus
            };
            transactionTrace.IsSuccessful().ShouldBe(isSuccess);
        }

        {
            var transactionTrace = new TransactionTrace
            {
                ExecutionStatus = ExecutionStatus.Executed,
                PreTraces =
                {
                    new TransactionTrace
                    {
                        ExecutionStatus = ExecutionStatus.Executed
                    },
                    new TransactionTrace
                    {
                        ExecutionStatus = executionStatus
                    }
                }
            };
            transactionTrace.IsSuccessful().ShouldBe(isSuccess);
        }

        {
            var transactionTrace = new TransactionTrace
            {
                ExecutionStatus = ExecutionStatus.Executed,
                InlineTraces =
                {
                    new TransactionTrace
                    {
                        ExecutionStatus = ExecutionStatus.Executed
                    },
                    new TransactionTrace
                    {
                        ExecutionStatus = executionStatus
                    }
                }
            };
            transactionTrace.IsSuccessful().ShouldBe(isSuccess);
        }

        {
            var transactionTrace = new TransactionTrace
            {
                ExecutionStatus = ExecutionStatus.Executed,
                PostTraces =
                {
                    new TransactionTrace
                    {
                        ExecutionStatus = ExecutionStatus.Executed
                    },
                    new TransactionTrace
                    {
                        ExecutionStatus = executionStatus
                    }
                }
            };
            transactionTrace.IsSuccessful().ShouldBe(isSuccess);
        }
    }

    [Fact]
    public void GetPluginLogs_Test()
    {
        var logEvents = new List<LogEvent>();
        for (var i = 0; i < 5; i++) logEvents.Add(new LogEvent { Name = "LogEvent" + i });

        var transactionTrace = new TransactionTrace
        {
            PreTraces =
            {
                new TransactionTrace
                {
                    ExecutionStatus = ExecutionStatus.Executed,
                    Logs = { logEvents[0] }
                },
                new TransactionTrace
                {
                    ExecutionStatus = ExecutionStatus.Canceled,
                    Logs = { logEvents[1] }
                }
            },
            InlineTraces =
            {
                new TransactionTrace
                {
                    ExecutionStatus = ExecutionStatus.Executed,
                    Logs = { logEvents[2] }
                }
            },
            PostTraces =
            {
                new TransactionTrace
                {
                    ExecutionStatus = ExecutionStatus.Executed,
                    Logs = { logEvents[3] }
                },
                new TransactionTrace
                {
                    ExecutionStatus = ExecutionStatus.Canceled,
                    Logs = { logEvents[4] }
                }
            }
        };

        var logs = transactionTrace.GetPluginLogs().ToList();
        logs.Count.ShouldBe(2);
        logs.ShouldContain(logEvents[0]);
        logs.ShouldContain(logEvents[3]);
    }

    [Fact]
    public void SurfaceUpError_Test()
    {
        var transactionTrace = new TransactionTrace
        {
            ExecutionStatus = ExecutionStatus.Executed,
            InlineTraces =
            {
                new TransactionTrace
                {
                    ExecutionStatus = ExecutionStatus.Executed
                }
            }
        };
        transactionTrace.SurfaceUpError();
        transactionTrace.Error.ShouldBeEmpty();
        transactionTrace.ExecutionStatus.ShouldBe(ExecutionStatus.Executed);

        transactionTrace.ExecutionStatus = ExecutionStatus.Canceled;
        transactionTrace.SurfaceUpError();
        transactionTrace.Error.ShouldBeEmpty();
        transactionTrace.ExecutionStatus.ShouldBe(ExecutionStatus.Canceled);

        transactionTrace = new TransactionTrace
        {
            ExecutionStatus = ExecutionStatus.Executed,
            InlineTraces =
            {
                new TransactionTrace
                {
                    ExecutionStatus = ExecutionStatus.ExceededMaxCallDepth,
                    Error = "ExceededMaxCallDepth"
                }
            }
        };
        transactionTrace.SurfaceUpError();
        transactionTrace.Error.ShouldContain("ExceededMaxCallDepth");
        transactionTrace.ExecutionStatus.ShouldBe(ExecutionStatus.ExceededMaxCallDepth);

        transactionTrace = new TransactionTrace
        {
            ExecutionStatus = ExecutionStatus.Prefailed,
            PreTraces =
            {
                new TransactionTrace
                {
                    ExecutionStatus = ExecutionStatus.Executed
                }
            }
        };
        transactionTrace.SurfaceUpError();
        transactionTrace.Error.ShouldBeEmpty();
        transactionTrace.ExecutionStatus.ShouldBe(ExecutionStatus.Prefailed);

        transactionTrace.PreTraces.Add(new TransactionTrace
        {
            ExecutionStatus = ExecutionStatus.Canceled,
            Error = "Canceled"
        });
        transactionTrace.SurfaceUpError();
        transactionTrace.Error.ShouldContain("Canceled");
        transactionTrace.ExecutionStatus.ShouldBe(ExecutionStatus.Prefailed);

        transactionTrace = new TransactionTrace
        {
            ExecutionStatus = ExecutionStatus.Postfailed,
            PostTraces =
            {
                new TransactionTrace
                {
                    ExecutionStatus = ExecutionStatus.Executed
                }
            }
        };
        transactionTrace.SurfaceUpError();
        transactionTrace.Error.ShouldBeEmpty();
        transactionTrace.ExecutionStatus.ShouldBe(ExecutionStatus.Postfailed);

        transactionTrace.PostTraces.Add(new TransactionTrace
        {
            ExecutionStatus = ExecutionStatus.Canceled,
            Error = "Canceled"
        });
        transactionTrace.SurfaceUpError();
        transactionTrace.Error.ShouldContain("Canceled");
        transactionTrace.ExecutionStatus.ShouldBe(ExecutionStatus.Postfailed);
    }
}