using System.Collections.Generic;
using System.IO;
using System.Threading.Tasks;
using AElf.Common;
using AElf.Configuration;
using AElf.Cryptography.ECDSA;
using AElf.Kernel;
using AElf.Synchronization.BlockExecution;
using Google.Protobuf;
using Moq;
using ServiceStack;
using Xunit;
using Xunit.Frameworks.Autofac;

namespace AElf.Synchronization.Tests
{
    /// <summary>
    /// We assume the block validation will always return success.
    /// </summary>
    [UseAutofacTestFramework]
    public class SynchronizerTests
    {
        public static readonly string TestContractName = "AElf.Kernel.Tests.TestContract";

        public static string TestContractFolder => $"../../../../{TestContractName}/bin/Debug/netstandard2.0";

        public static string TestContractDllPath => $"{TestContractFolder}/{TestContractName}.dll";

        public byte[] ExampleContractCode
        {
            get
            {
                byte[] code;
                using (FileStream file = File.OpenRead(Path.GetFullPath(TestContractDllPath)))
                {
                    code = file.ReadFully();
                }

                return code;
            }
        }

        private readonly MockSetup _mockSetup;

        public SynchronizerTests(MockSetup mockSetup)
        {
            _mockSetup = mockSetup;
        }

        private List<IBlock> MockSeveralBlocks(int number, int firstIndex = 0)
        {
            var list = new List<IBlock>();
            Hash temp = null;
            for (var i = firstIndex; i < number + firstIndex; i++)
            {
                var hash = Hash.Generate();
                list.Add(MockBlock((ulong) i, hash.DumpHex(), temp == null ? Hash.Generate() : temp));
                temp = hash;
            }

            return list;
        }

        private IBlock MockBlock(ulong index, string hashToHex, Hash preBlockHash)
        {
            return new Mock<IBlock>()
                .SetupProperty(b => b.Index, index)
                .SetupProperty(b => b.BlockHashToHex, hashToHex)
                .SetupProperty(b => b.Header, MockBlockHeader(preBlockHash))
                .Object;
        }

        private BlockHeader MockBlockHeader(Hash preBlockHash)
        {
            return new BlockHeader
            {
                MerkleTreeRootOfTransactions = Hash.Generate(),
                SideChainTransactionsRoot = Hash.Generate(),
                SideChainBlockHeadersRoot = Hash.Generate(),
                ChainId = Hash.LoadByteArray(new byte[] { 0x01, 0x02, 0x03 }),
                PreviousBlockHash = preBlockHash,
                MerkleTreeRootOfWorldState = Hash.Generate()
            };
        }

        public List<Transaction> CreateTx(Hash chainId)
        {
            var contractAddressZero =
                AddressHelpers.GetSystemContractAddress(chainId, GlobalConfig.GenesisBasicContract);

            var keyPair = new KeyPairGenerator().Generate();
            var signer = new ECSigner();

            var txPrint = new Transaction()
            {
                From = AddressHelpers.BuildAddress(keyPair.GetEncodedPublicKey()),
                To = contractAddressZero,
                MethodName = "Print",
                Params = ByteString.CopyFrom(new Parameters()
                {
                    Params =
                    {
                        new Param
                        {
                            StrVal = "AElf"
                        }
                    }
                }.ToByteArray()),

                Fee = 0
            };

            var hash = txPrint.GetHash();

            var signature = signer.Sign(keyPair, hash.DumpByteArray());
            txPrint.Sig = new Signature
            {
                P = ByteString.CopyFrom(keyPair.PublicKey.Q.GetEncoded()),
                R = ByteString.CopyFrom(signature.R),
                S = ByteString.CopyFrom(signature.S)
            };

            var txs = new List<Transaction>
            {
                txPrint
            };

            return txs;
        }
    }
}