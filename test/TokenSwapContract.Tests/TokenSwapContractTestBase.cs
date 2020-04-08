using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Acs0;
using AElf;
using AElf.Contracts.MultiToken;
using AElf.Contracts.TestKit;
using AElf.Cryptography.ECDSA;
using AElf.CSharp.CodeOps;
using AElf.Kernel;
using AElf.Kernel.SmartContract.Application;
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
        protected ECKeyPair NormalKeyPair => SampleECKeyPairs.KeyPairs[1];
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
            var tokenSwapContractCode = _patchedCodes.Single(kv => kv.Key.EndsWith("TokenSwapContract")).Value;
            TokenSwapContractAddress = AsyncHelper.RunSync(async () =>
                await DeployContractAsync(
                    KernelConstants.CodeCoverageRunnerCategory,
                    tokenSwapContractCode,
                    Hash.FromString("TokenSwapContract"),
                    DefaultSenderKeyPair));
            TokenSwapContractStub = GetTester<TokenSwapContractContainer.TokenSwapContractStub>(
                TokenSwapContractAddress,
                DefaultSenderKeyPair);
            CheckCode(tokenSwapContractCode);
        }


        protected void CheckCode(byte[] code)
        {
            var auditor = new CSharpContractAuditor(null, null);
            auditor.Audit(code, new RequiredAcs
            {
                AcsList = new List<string>()
            });
        }

        protected async Task CreateAndApproveTokenAsync(string tokenName, string symbol, int decimals, long totalSupply,
            long approveAmount)
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
                To = DefaultSenderAddress
            };
            await TokenContractStub.Issue.SendAsync(issueInput);

            var approveInput = new ApproveInput
            {
                Amount = approveAmount,
                Spender = TokenSwapContractAddress,
                Symbol = symbol
            };
            await TokenContractStub.Approve.SendAsync(approveInput);
        }

        internal async Task<Hash> CreateSwapAsync(string symbol = "ELF", int originTokenSizeInByte = 32,
            SwapRatio ratio = null, long depositAmount = 0, bool isBigEndian = true)
        {
            var swapRatio = ratio ?? new SwapRatio
            {
                OriginShare = 10_000_000_000, //1e18
                TargetShare = 1 // 1e8
            };
            var addSwapPairTx = await TokenSwapContractStub.CreateSwap.SendAsync(new CreateSwapInput()
            {
                OriginTokenSizeInByte = originTokenSizeInByte,

                OriginTokenNumericBigEndian = isBigEndian,
                SwapTargetTokenList =
                {
                    new SwapTargetToken
                    {
                        SwapRatio = swapRatio,
                        TargetTokenSymbol = symbol,
                        DepositAmount = depositAmount == 0 ? TotalSupply : depositAmount,
                    }
                }
            });
            var swapId = addSwapPairTx.Output;
            return swapId;
        }
        
        internal async Task<Hash> CreateSwapWithMultiTargetTokenAsync(int originTokenSizeInByte = 32,
            bool isBigEndian = true, params SwapTargetToken[] swapTargetTokens)
        {
            var addSwapPairTx = await TokenSwapContractStub.CreateSwap.SendAsync(new CreateSwapInput()
            {
                OriginTokenSizeInByte = originTokenSizeInByte,

                OriginTokenNumericBigEndian = isBigEndian,
                SwapTargetTokenList =
                {
                    swapTargetTokens
                }
            });
            var swapId = addSwapPairTx.Output;
            return swapId;
        }

        protected async Task AddSwapRound(Hash swapId, Hash merkleTreeRoot)
        {
            var addSwapRoundInput = new AddSwapRoundInput
            {
                MerkleTreeRoot = merkleTreeRoot,
                SwapId = swapId
            };
            await TokenSwapContractStub.AddSwapRound.SendAsync(addSwapRoundInput);
        }

        internal TokenSwapContractContainer.TokenSwapContractStub GetTokenSwapContractStub(ECKeyPair ecKeyPair)
        {
            return GetTester<TokenSwapContractContainer.TokenSwapContractStub>(TokenSwapContractAddress, ecKeyPair);
        }

        protected Hash GetHashTokenAmountData(decimal amount, int originTokenSizeInByte)
        {
            var preHolderSize = originTokenSizeInByte - 16;
            var amountInIntegers = decimal.GetBits(amount).Reverse().ToArray();

            if (preHolderSize < 0)
                amountInIntegers = amountInIntegers.TakeLast(originTokenSizeInByte / 4).ToArray();

            var amountBytes = new List<byte>();
            amountInIntegers.Aggregate(amountBytes, (cur, i) =>
            {
                while (cur.Count < preHolderSize)
                {
                    cur.Add(new byte());
                }

                cur.AddRange(i.ToBytes());
                return cur;
            });
            return Hash.FromRawBytes(amountBytes.ToArray());
        }

        protected bool TryGetOriginTokenAmount(string amountInString, out decimal amount)
        {
            return decimal.TryParse(amountInString, out amount);
        }

        protected async Task CreatAndIssueDefaultTokenAsync()
        {
            await CreateAndApproveTokenAsync(TokenName1, DefaultSymbol1, 8, TotalSupply, TotalSupply);
            await CreateAndApproveTokenAsync(TokenName2, DefaultSymbol2, 8, TotalSupply, TotalSupply);
        }

        protected string DefaultSymbol1 { get; set; } = "ELF";
        protected string DefaultSymbol2 { get; set; } = "ABC";

        protected string TokenName1 { get; set; } = "ELF";
        protected string TokenName2 { get; set; } = "ABC";

        protected long TotalSupply { get; set; } = 100_000_000_000_000_000;
    }
}