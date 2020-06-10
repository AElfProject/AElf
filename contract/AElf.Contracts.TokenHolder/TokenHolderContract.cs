using System.Linq;
using AElf.Contracts.MultiToken;
using AElf.Contracts.Profit;
using AElf.CSharp.Core;
using AElf.CSharp.Core.Extension;
using AElf.Sdk.CSharp;
using AElf.Types;
using Google.Protobuf.WellKnownTypes;

namespace AElf.Contracts.TokenHolder
{
    public partial class TokenHolderContract : TokenHolderContractContainer.TokenHolderContractBase
    {
        public override Empty CreateScheme(CreateTokenHolderProfitSchemeInput input)
        {
            if (State.ProfitContract.Value == null)
            {
                State.ProfitContract.Value =
                    Context.GetContractAddressByName(SmartContractConstants.ProfitContractSystemName);
            }

            State.ProfitContract.CreateScheme.Send(new CreateSchemeInput
            {
                Manager = Context.Sender,
                IsReleaseAllBalanceEveryTimeByDefault = true,
                CanRemoveBeneficiaryDirectly = true
            });

            State.TokenHolderProfitSchemes[Context.Sender] = new TokenHolderProfitScheme
            {
                Symbol = input.Symbol,
                MinimumLockMinutes = input.MinimumLockMinutes,
                AutoDistributeThreshold = {input.AutoDistributeThreshold}
            };

            return new Empty();
        }

        public override Empty AddBeneficiary(AddTokenHolderBeneficiaryInput input)
        {
            var scheme = GetValidScheme(Context.Sender);
            var detail = State.ProfitContract.GetProfitDetails.Call(new GetProfitDetailsInput
            {
                SchemeId = scheme.SchemeId,
                Beneficiary = input.Beneficiary
            });
            var shares = input.Shares;
            if (detail.Details.Any())
            {
                // Only keep one detail.

                State.ProfitContract.RemoveBeneficiary.Send(new RemoveBeneficiaryInput
                {
                    SchemeId = scheme.SchemeId,
                    Beneficiary = input.Beneficiary
                });
                shares.Add(detail.Details.Single().Shares);
            }

            State.ProfitContract.AddBeneficiary.Send(new AddBeneficiaryInput
            {
                SchemeId = scheme.SchemeId,
                BeneficiaryShare = new BeneficiaryShare
                {
                    Beneficiary = input.Beneficiary,
                    Shares = shares
                }
            });
            return new Empty();
        }

        public override Empty RemoveBeneficiary(RemoveTokenHolderBeneficiaryInput input)
        {
            var scheme = GetValidScheme(Context.Sender);

            var detail = State.ProfitContract.GetProfitDetails.Call(new GetProfitDetailsInput
            {
                Beneficiary = input.Beneficiary,
                SchemeId = scheme.SchemeId
            }).Details.Single();
            var lockedAmount = detail.Shares;
            State.ProfitContract.RemoveBeneficiary.Send(new RemoveBeneficiaryInput
            {
                SchemeId = scheme.SchemeId,
                Beneficiary = input.Beneficiary
            });
            if (lockedAmount > input.Amount &&
                input.Amount != 0) // If input.Amount == 0, means just remove this beneficiary.
            {
                State.ProfitContract.AddBeneficiary.Send(new AddBeneficiaryInput
                {
                    SchemeId = scheme.SchemeId,
                    BeneficiaryShare = new BeneficiaryShare
                    {
                        Beneficiary = input.Beneficiary,
                        Shares = lockedAmount.Sub(input.Amount)
                    }
                });
            }

            return new Empty();
        }

        public override Empty ContributeProfits(ContributeProfitsInput input)
        {
            var scheme = GetValidScheme(input.SchemeManager);
            if (State.TokenContract.Value == null)
            {
                State.TokenContract.Value =
                    Context.GetContractAddressByName(SmartContractConstants.TokenContractSystemName);
            }

            State.TokenContract.TransferFrom.Send(new TransferFromInput
            {
                From = Context.Sender,
                To = Context.Self,
                Symbol = input.Symbol,
                Amount = input.Amount
            });

            State.TokenContract.Approve.Send(new ApproveInput
            {
                Spender = State.ProfitContract.Value,
                Symbol = input.Symbol,
                Amount = input.Amount
            });

            State.ProfitContract.ContributeProfits.Send(new Profit.ContributeProfitsInput
            {
                SchemeId = scheme.SchemeId,
                Symbol = input.Symbol,
                Amount = input.Amount
            });
            return new Empty();
        }

        public override Empty DistributeProfits(DistributeProfitsInput input)
        {
            var scheme = GetValidScheme(input.SchemeManager, true);
            Assert(Context.Sender == Context.GetContractAddressByName(SmartContractConstants.TokenContractSystemName) ||
                   Context.Sender == input.SchemeManager, "No permission to distribute profits.");
            var distributeProfitsInput = new Profit.DistributeProfitsInput
            {
                SchemeId = scheme.SchemeId,
                Period = scheme.Period
            };
            if (input.AmountsMap != null && input.AmountsMap.Any())
            {
                distributeProfitsInput.AmountsMap.Add(input.AmountsMap);
            }

            State.ProfitContract.DistributeProfits.Send(distributeProfitsInput);
            scheme.Period = scheme.Period.Add(1);
            State.TokenHolderProfitSchemes[input.SchemeManager] = scheme;
            return new Empty();
        }

        public override Empty RegisterForProfits(RegisterForProfitsInput input)
        {
            Assert(State.LockIds[input.SchemeManager][Context.Sender] == null, "Already registered.");
            var scheme = GetValidScheme(input.SchemeManager);
            if (State.TokenContract.Value == null)
            {
                State.TokenContract.Value =
                    Context.GetContractAddressByName(SmartContractConstants.TokenContractSystemName);
            }

            var lockId = Context.GenerateId(Context.Self,
                ByteArrayHelper.ConcatArrays(input.SchemeManager.ToByteArray(), Context.Sender.ToByteArray()));
            State.TokenContract.Lock.Send(new LockInput
            {
                LockId = lockId,
                Symbol = scheme.Symbol,
                Address = Context.Sender,
                Amount = input.Amount,
            });
            State.LockIds[input.SchemeManager][Context.Sender] = lockId;
            State.LockTimestamp[lockId] = Context.CurrentBlockTime;
            State.ProfitContract.AddBeneficiary.Send(new AddBeneficiaryInput
            {
                SchemeId = scheme.SchemeId,
                BeneficiaryShare = new BeneficiaryShare
                {
                    Beneficiary = Context.Sender,
                    Shares = input.Amount
                }
            });

            // Check auto-distribute threshold.
            if (scheme.AutoDistributeThreshold != null && scheme.AutoDistributeThreshold.Any())
            {
                foreach (var threshold in scheme.AutoDistributeThreshold)
                {
                    var originScheme = State.ProfitContract.GetScheme.Call(scheme.SchemeId);
                    var balance = State.TokenContract.GetBalance.Call(new GetBalanceInput
                    {
                        Owner = originScheme.VirtualAddress,
                        Symbol = threshold.Key
                    }).Balance;
                    if (balance < threshold.Value) continue;
                    State.ProfitContract.DistributeProfits.Send(new Profit.DistributeProfitsInput
                    {
                        SchemeId = scheme.SchemeId,
                        Period = scheme.Period.Add(1)
                    });
                    scheme.Period = scheme.Period.Add(1);
                    State.TokenHolderProfitSchemes[input.SchemeManager] = scheme;
                }
            }

            return new Empty();
        }

        public override Empty Withdraw(Address input)
        {
            var scheme = GetValidScheme(input);
            if (State.TokenContract.Value == null)
            {
                State.TokenContract.Value =
                    Context.GetContractAddressByName(SmartContractConstants.TokenContractSystemName);
            }

            var amount = State.TokenContract.GetLockedAmount.Call(new GetLockedAmountInput
            {
                Address = Context.Sender,
                LockId = State.LockIds[input][Context.Sender],
                Symbol = scheme.Symbol
            }).Amount;

            var lockId = State.LockIds[input][Context.Sender];
            Assert(State.LockTimestamp[lockId].AddMinutes(scheme.MinimumLockMinutes) < Context.CurrentBlockTime,
                "Cannot withdraw.");

            State.TokenContract.Unlock.Send(new UnlockInput
            {
                Address = Context.Sender,
                LockId = lockId,
                Amount = amount,
                Symbol = scheme.Symbol
            });

            State.LockIds[input].Remove(Context.Sender);
            State.ProfitContract.RemoveBeneficiary.Send(new RemoveBeneficiaryInput
            {
                SchemeId = scheme.SchemeId,
                Beneficiary = Context.Sender
            });
            return new Empty();
        }

        public override Empty ClaimProfits(ClaimProfitsInput input)
        {
            var scheme = GetValidScheme(input.SchemeManager);
            var beneficiary = input.Beneficiary ?? Context.Sender;
            State.ProfitContract.ClaimProfits.Send(new Profit.ClaimProfitsInput
            {
                SchemeId = scheme.SchemeId,
                Beneficiary = beneficiary
            });
            return new Empty();
        }

        public override TokenHolderProfitScheme GetScheme(Address input)
        {
            return State.TokenHolderProfitSchemes[input];
        }

        public override ReceivedProfitsMap GetProfitsMap(ClaimProfitsInput input)
        {
            var scheme = State.TokenHolderProfitSchemes[input.SchemeManager];
            var profitsMap = State.ProfitContract.GetProfitsMap.Call(new Profit.ClaimProfitsInput
            {
                SchemeId = scheme.SchemeId,
                Beneficiary = input.Beneficiary ?? Context.Sender
            });
            return new ReceivedProfitsMap
            {
                Value = {profitsMap.Value}
            };
        }

        private TokenHolderProfitScheme GetValidScheme(Address manager, bool updateSchemePeriod = false)
        {
            var scheme = State.TokenHolderProfitSchemes[manager];
            Assert(scheme != null, "Token holder profit scheme not found.");
            UpdateTokenHolderProfitScheme(ref scheme, manager, updateSchemePeriod);
            return scheme;
        }

        private void UpdateTokenHolderProfitScheme(ref TokenHolderProfitScheme scheme, Address manager,
            bool updateSchemePeriod)
        {
            if (scheme.SchemeId != null && !updateSchemePeriod) return;
            var originSchemeId = State.ProfitContract.GetManagingSchemeIds.Call(new GetManagingSchemeIdsInput
            {
                Manager = manager
            }).SchemeIds.FirstOrDefault();
            Assert(originSchemeId != null, "Origin scheme not found.");
            var originScheme = State.ProfitContract.GetScheme.Call(originSchemeId);
            scheme.SchemeId = originScheme.SchemeId;
            scheme.Period = originScheme.CurrentPeriod;
            State.TokenHolderProfitSchemes[Context.Sender] = scheme;
        }
    }
}