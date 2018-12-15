using System;
using System.Linq;
using System.Reflection;
using AElf.CLI2.Commands;
using AElf.CLI2.Commands.CrossChain;
using AElf.CLI2.Commands.Proposal;
using AElf.CLI2.JS;
using AElf.CLI2.JS.IO;
using Autofac;
using CommandLine;
using Console = System.Console;

namespace AElf.CLI2
{
    class Program
    {
        static int Main(string[] args)
        {

            #region proposal

            if (args[0].Contains("proposal"))
            {
                return Parser.Default
                    .ParseArguments<ProposalOption, CheckProposalOption, ApprovalOption, ReleaseProposalOption>(args)
                    .MapResult((ProposalOption opt) =>
                        {
                            using (var cmd = new ProposeCommand(opt))
                            {
                                cmd.Execute();
                            }

                            return 0;
                        },
                        (CheckProposalOption opt) =>
                        {
                            using (var cmd = new CheckProposalCommand(opt))
                            {
                                cmd.Execute();
                            }

                            return 0;
                        },
                        (ApprovalOption opt) =>
                        {
                            using (var cmd = new ApproveCommand(opt))
                            {
                                cmd.Execute();
                            }

                            return 0;
                        },
                        (ReleaseProposalOption opt) =>
                        {
                            using (var cmd = new ReleaseProposalCommand(opt))
                            {
                                cmd.Execute();
                            }

                            return 0;
                        }, errs => 1);
            }
            
            #endregion

            #region sidechain

            if (args[0].Contains("sidechain"))
            {
                return Parser.Default
                    .ParseArguments<ChainCreationRequestOption, ChainDisposalRequestOption, CheckChainStatusOption
                    >(args).MapResult((ChainCreationRequestOption opt) =>
                        {
                            using (var cmd = new ChainCreationRequestCommand(opt))
                            {
                                cmd.Execute();
                            }

                            return 0;
                        },
                        (ChainDisposalRequestOption opt) =>
                        {
                            using (var cmd = new ChainDisposalRequestCommand(opt))
                            {
                                cmd.Execute();
                            }

                            return 0;
                        },
                        (CheckChainStatusOption opt) =>
                        {
                            using (var cmd = new CheckChainStatusCommand(opt))
                            {
                                cmd.Execute();
                            }

                            return 0;
                        }, errs => 1);
            }

            #endregion

            return Parser.Default
                .ParseArguments<CreateOption, InteractiveOption, DeployOption, GetAbiOption, SendTransactionOption,
                    GetTxResultOption, GetBlockHeightOption, GetBlockInfoOption, GetMerkelPathOption, 
                    CreateMultiSigOption>(args)
                .MapResult(
                    (CreateOption opt) =>
                    {
                        using (var cmd = new CreateCommand(opt))
                        {
                            cmd.Execute();
                        }

                        return 0;
                    },
                    (InteractiveOption opt) =>
                    {
                        using (var cmd = new InteractiveCommand(opt))
                        {
                            cmd.Execute();
                        }

                        return 0;
                    },
                    (DeployOption opt) =>
                    {
                        using (var cmd = new DeployCommand(opt))
                        {
                            cmd.Execute();
                        }

                        return 0;
                    },
                    (GetAbiOption opt) =>
                    {
                        using (var cmd = new GetAbiCommand(opt))
                        {
                            cmd.Execute();
                        }

                        return 0;
                    },
                    (SendTransactionOption opt) =>
                    {
                        using (var cmd = new SendTransactionCommand(opt))
                        {
                            cmd.Execute();
                        }

                        return 0;
                    },
                    (GetTxResultOption opt) =>
                    {
                        using (var cmd = new GetTxResultCommand(opt))
                        {
                            cmd.Execute();
                        }

                        return 0;
                    },
                    (GetBlockHeightOption opt) =>
                    {
                        using (var cmd = new GetBlockHeightCommand(opt))
                        {
                            cmd.Execute();
                        }

                        return 0;
                    },
                    (GetBlockInfoOption opt) =>
                    {
                        using (var cmd = new GetBlockInfoCommand(opt))
                        {
                            cmd.Execute();
                        }

                        return 0;
                    },
                    (GetMerkelPathOption opt) =>
                    {
                        using (var cmd = new GetMerkelPathCommand(opt))
                        {
                            cmd.Execute();
                        }

                        return 0;
                    },
                    (CreateMultiSigOption opt) =>
                    {
                        using (var cmd = new CreateMultiSigAddressCommand(opt))
                        {
                            cmd.Execute();
                        }

                        return 0;
                    },
                    errs => 1);
        }
    }
}