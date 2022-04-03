using AElf.Sdk.CSharp;
using AElf.Sdk.CSharp.State;
using AElf.Types;

namespace AElf.Contracts.Profit.Managers
{
    public class ProfitSchemeManager
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

        public void CreateScheme(Scheme scheme)
        {
            if (_schemeMap[scheme.SchemeId] != null)
            {
                throw new AssertionException("Already exists.");
            }

            _schemeMap[scheme.SchemeId] = scheme;

            if (_managingSchemeIdsMap[scheme.Manager] == null)
            {
                _managingSchemeIdsMap[scheme.Manager] = new CreatedSchemeIds
                {
                    SchemeIds = {scheme.SchemeId}
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
    }
}