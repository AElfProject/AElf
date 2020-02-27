using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Acs0;
using AElf.Contracts.MultiToken;
using AElf.Contracts.TestKit;
using AElf.Cryptography.ECDSA;
using AElf.Kernel;
using AElf.Kernel.Token;
using AElf.Types;
using Tokenswap;
using Volo.Abp.Threading;

namespace TokenSwapContract.Tests
{
    public class TokenSwapContractTestBase : ContractTestBase<TokenSwapContractTestModule>
    {
        internal ACS0Container.ACS0Stub BasicContractZeroStub { get; set; }

        internal TokenContractContainer.TokenContractStub TokenContractStub { get; set; }
        protected ECKeyPair DefaultSenderKeyPair => SampleECKeyPairs.KeyPairs[0];
        protected Address DefaultSenderAddress => Address.FromPublicKey(DefaultSenderKeyPair.PublicKey);
        protected Address TokenSwapContractAddress { get; set; }

        private IReadOnlyDictionary<string, byte[]> _patchedCodes;

        internal TokenSwapContractContainer.TokenSwapContractStub TokenSwapContractStub { get; set; }

        private const string ContractPatchedDllDir = "../../../../patched/";

        public TokenSwapContractTestBase()
        {
            _patchedCodes = GetPatchedCodes(ContractPatchedDllDir);
        }

        internal ACS0Container.ACS0Stub GetContractZeroTester(ECKeyPair keyPair)
        {
            return GetTester<Acs0.ACS0Container.ACS0Stub>(ContractZeroAddress, keyPair);
        }

        protected void InitializePatchedContracts()
        {
            BasicContractZeroStub = GetContractZeroTester(DefaultSenderKeyPair);

            var tokenContractAddress = AsyncHelper.RunSync(async () =>
                await DeploySystemSmartContract(
                    KernelConstants.CodeCoverageRunnerCategory,
                    _patchedCodes.Single(kv => kv.Key.EndsWith("MultiToken")).Value,
                    TokenSmartContractAddressNameProvider.Name,
                    DefaultSenderKeyPair));
            TokenContractStub =
                GetTester<TokenContractContainer.TokenContractStub>(tokenContractAddress, DefaultSenderKeyPair);

            //deploy token swap contract
            TokenSwapContractAddress = AsyncHelper.RunSync(async () =>
                await DeployContractAsync(
                    KernelConstants.CodeCoverageRunnerCategory,
                    _patchedCodes.Single(kv => kv.Key.EndsWith("TokenSwapContract")).Value,
                    Hash.FromString("TokenSwapContract"),
                    DefaultSenderKeyPair));
            TokenSwapContractStub = GetTester<TokenSwapContractContainer.TokenSwapContractStub>(
                TokenSwapContractAddress,
                DefaultSenderKeyPair);
        }

        public async Task CreateAndIssueTokenAsync(string tokenName, string symbol, int decimals, long totalSupply)
        {
            var createInput = new CreateInput
            {
                Symbol = symbol,
                TokenName = tokenName,
                IsBurnable = true,
                Decimals = decimals,
                Issuer = DefaultSenderAddress,
                TotalSupply = totalSupply
            };
            await TokenContractStub.Create.SendAsync(createInput);

            var issueInput = new IssueInput
            {
                Amount = totalSupply,
                Symbol = symbol,
                To = TokenSwapContractAddress
            };
            await TokenContractStub.Issue.SendAsync(issueInput);
        }

        protected async Task<Hash> AddSwapPairAsync()
        {
            var tokenName = "ELF";
            var symbol = "ELF";
            var totalSupply = 100_000_000_000_000_000;
            await CreateAndIssueTokenAsync(tokenName, symbol, 8, totalSupply);
            var swapRatio = new SwapRatio
            {
                OriginShare = 10_000_000_000,
                TargetShare = 1
            };
            var originTokenSizeInByte = 32;
            var addSwapPairTx = await TokenSwapContractStub.AddSwapPair.SendAsync(new AddSwapPairInput
            {
                OriginTokenSizeInByte = originTokenSizeInByte,
                SwapRatio = swapRatio,
                TargetTokenSymbol = symbol
            });
            var pairId = addSwapPairTx.Output;
            return pairId;
        }

        protected async Task AddSwapRound(Hash pairId, Hash merkleTreeRoot)
        {
            var addSwapRoundInput = new AddSwapRoundInput
            {
                MerkleTreeRoot = merkleTreeRoot,
                SwapPairId = pairId
            };
            await TokenSwapContractStub.AddSwapRound.SendAsync(addSwapRoundInput);
        }
    }
}