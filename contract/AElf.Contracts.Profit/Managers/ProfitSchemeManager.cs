using System.Linq;
using AElf.CSharp.Core;
using AElf.Sdk.CSharp;
using AElf.Sdk.CSharp.State;
using AElf.Types;

namespace AElf.Contracts.Profit.Managers
{
    public class ProfitSchemeManager : IProfitSchemeManager
    {
        private readonly CSharpSmartContractContext _context;
        private readonly MappedState<Hash, Scheme> _schemeMap;
        private readonly MappedState<Address, CreatedSchemeIds> _managingSchemeIdsMap;

        public ProfitSchemeManager(CSharpSmartContractContext context, MappedState<Hash, Scheme> schemeMap,
            MappedState<Address, CreatedSchemeIds> managingSchemeIdsMap)
        {
            _context = context;
            _schemeMap = schemeMap;
            _managingSchemeIdsMap = managingSchemeIdsMap;
        }

        public void CreateNewScheme(Scheme scheme)
        {
            if (scheme.ProfitReceivingDuePeriodCount == 0)
            {
                scheme.ProfitReceivingDuePeriodCount = ProfitContractConstants.DefaultProfitReceivingDuePeriodCount;
            }
            else
            {
                if (scheme.ProfitReceivingDuePeriodCount <= 0 || scheme.ProfitReceivingDuePeriodCount >
                    ProfitContractConstants.MaximumProfitReceivingDuePeriodCount)
                {
                    throw new AssertionException("Invalid profit receiving due period count.");
                }
            }

            if (_schemeMap[scheme.SchemeId] != null)
            {
                throw new AssertionException("Already exists.");
            }

            _schemeMap[scheme.SchemeId] = scheme;

            if (_managingSchemeIdsMap[scheme.Manager] == null)
            {
                _managingSchemeIdsMap[scheme.Manager] = new CreatedSchemeIds
                {
                    SchemeIds = { scheme.SchemeId }
                };
            }
            else
            {
                _managingSchemeIdsMap[scheme.Manager].SchemeIds.Add(scheme.SchemeId);
            }

            _context.LogDebug(() => $"Created scheme {scheme}");

            _context.Fire(new SchemeCreated
            {
                SchemeId = scheme.SchemeId,
                Manager = scheme.Manager,
                IsReleaseAllBalanceEveryTimeByDefault = scheme.IsReleaseAllBalanceEveryTimeByDefault,
                ProfitReceivingDuePeriodCount = scheme.ProfitReceivingDuePeriodCount,
                VirtualAddress = scheme.VirtualAddress
            });
        }

        public void AddSubScheme(Hash schemeId, Hash subSchemeId, long shares)
        {
            if (schemeId == subSchemeId)
            {
                throw new AssertionException("Two schemes cannot be same.");
            }

            if (shares <= 0)
            {
                throw new AssertionException("Shares of sub scheme should greater than 0.");
            }

            var scheme = GetScheme(schemeId);

            if (_context.Sender != scheme.Manager)
            {
                throw new AssertionException("Only manager can add sub-scheme.");
            }

            if (scheme.SubSchemes.Any(s => s.SchemeId == subSchemeId))
            {
                throw new AssertionException($"Sub scheme {subSchemeId} already exist.");
            }

            CheckSchemeExists(subSchemeId);

            _schemeMap[schemeId].SubSchemes.Add(new SchemeBeneficiaryShare
            {
                SchemeId = subSchemeId,
                Shares = shares
            });
        }

        public void RemoveSubScheme(Hash schemeId, Hash subSchemeId)
        {
            if (schemeId == subSchemeId)
            {
                throw new AssertionException("Two schemes cannot be same.");
            }

            var scheme = GetScheme(schemeId);

            if (_context.Sender != scheme.Manager)
            {
                throw new AssertionException("Only manager can remove sub-scheme.");
            }

            var subSchemeShare = scheme.SubSchemes.SingleOrDefault(d => d.SchemeId == subSchemeId);
            if (subSchemeShare == null)
            {
                // Won't do anything. (like previews)
                return;
            }

            CheckSchemeExists(subSchemeId);

            _schemeMap[schemeId].SubSchemes.Remove(subSchemeShare);
        }

        public void AddShares(Hash schemeId, long shares)
        {
            var scheme = GetScheme(schemeId);
            _schemeMap[schemeId].TotalShares = scheme.TotalShares.Add(shares);
        }

        public void RemoveShares(Hash schemeId, long shares)
        {
            var scheme = GetScheme(schemeId);
            _schemeMap[schemeId].TotalShares = scheme.TotalShares.Sub(shares);
        }

        public void AddReceivedTokenSymbol(Hash schemeId, string symbol)
        {
            CheckSchemeExists(schemeId);
            if (!_schemeMap[schemeId].ReceivedTokenSymbols.Contains(symbol))
            {
                _schemeMap[schemeId].ReceivedTokenSymbols.Add(symbol);
            }
        }

        /// <summary>
        /// Won't return null.
        /// </summary>
        /// <param name="schemeId"></param>
        /// <returns></returns>
        /// <exception cref="AssertionException"></exception>
        public Scheme GetScheme(Hash schemeId)
        {
            var scheme = _schemeMap[schemeId];
            if (scheme == null)
            {
                throw new AssertionException($"Scheme {schemeId} not found.");
            }

            return scheme;
        }

        public void CheckSchemeExists(Hash schemeId)
        {
            if (_schemeMap[schemeId] == null)
            {
                throw new AssertionException($"Scheme {schemeId} not found.");
            }
        }
    }
}