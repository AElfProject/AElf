using System;
using System.IO;
using AElf.Common;
using AElf.Cryptography.ECDSA;
using AElf.Kernel;
using AElf.Kernel.Types.Proposal;
using AElf.Kernel.Types.Transaction;
using AElf.Types.CSharp;
using Google.Protobuf;
using Google.Protobuf.WellKnownTypes;
using NLog;
using NServiceKit.Text;
using Xunit;
using Xunit.Frameworks.Autofac;

namespace AElf.Contracts.Authorization.Tests
{
    [UseAutofacTestFramework]
    public class AuthorizationTest
    {
        private AuthorizationContractShim _contract;
        private ILogger _logger;
        private MockSetup Mock;
        
        private void Init()
        {
            Mock = new MockSetup(_logger);
        }
        
        public AuthorizationTest(ILogger logger)
        {
            _logger = logger;
        }

        [Fact]
        public void CreateMSigAccount()
        {
            Init();
            _contract = new AuthorizationContractShim(Mock, ContractHelpers.GetAuthorizationContractAddress(Mock.ChainId1));
            Address msig = Address.Generate();
            var auth = new Kernel.Types.Proposal.Authorization
            {
                ExecutionThreshold = 2,
                MultiSigAccount = msig,
                ProposerThreshold = 1
            };
            var kp1 = new KeyPairGenerator().Generate();
            var kp2 = new KeyPairGenerator().Generate();
            var kp3 = new KeyPairGenerator().Generate();
            auth.Reviewers.AddRange(new[]
            {
                new Reviewer
                {
                    PubKey = ByteString.CopyFrom(kp1.PublicKey.Q.GetEncoded()),
                    Weight = 1
                },
                new Reviewer
                {
                    PubKey = ByteString.CopyFrom(kp2.PublicKey.Q.GetEncoded()),
                    Weight = 1
                },
                new Reviewer
                {
                    PubKey = ByteString.CopyFrom(kp3.PublicKey.Q.GetEncoded()),
                    Weight = 0
                }
            });
            var addr = _contract.CreateMSigAccount(auth).Result;
            Assert.Equal(msig.DumpByteArray(), addr);
        }

        [Fact]
        public void ProposeInvalidProposal()
        {
            Init();
            _contract = new AuthorizationContractShim(Mock, ContractHelpers.GetAuthorizationContractAddress(Mock.ChainId1));
            Address msig = Address.Generate();
            var auth = new Kernel.Types.Proposal.Authorization
            {
                ExecutionThreshold = 2,
                MultiSigAccount = msig,
                ProposerThreshold = 1
            };
            var kp1 = new KeyPairGenerator().Generate();
            var kp2 = new KeyPairGenerator().Generate();
            var kp3 = new KeyPairGenerator().Generate();
            auth.Reviewers.AddRange(new[]
            {
                new Reviewer
                {
                    PubKey = ByteString.CopyFrom(kp1.PublicKey.Q.GetEncoded()),
                    Weight = 1
                },
                new Reviewer
                {
                    PubKey = ByteString.CopyFrom(kp2.PublicKey.Q.GetEncoded()),
                    Weight = 1
                },
                new Reviewer
                {
                    PubKey = ByteString.CopyFrom(kp3.PublicKey.Q.GetEncoded()),
                    Weight = 0
                }
            });
            var addr = _contract.CreateMSigAccount(auth).Result;
            Assert.Equal(msig.DumpByteArray(), addr);
            
            var expiredProposal = new Proposal
            {
                ExpiredTime = Timestamp.FromDateTime(DateTime.UtcNow.AddSeconds(-10)),
                MultiSigAccount = msig,
                Name = "Propose",
                TxnData = new PendingTxn
                {
                    ProposalName = "Propose",
                    TxnData = CreateDemoTxn(msig).ToByteString()
                }
            };
            var res = _contract.Propose(expiredProposal, kp1).Result;
            //Hash hash = Hash.LoadByteArray(res);
            Assert.Null(res);
            
            var notFoundAuthorizationProposal = new Proposal
            {
                ExpiredTime = Timestamp.FromDateTime(DateTime.UtcNow.AddSeconds(10)),
                MultiSigAccount = Address.Generate(),
                Name = "Propose",
                //ProposerPublicKey = ByteString.CopyFrom(kp1.PublicKey.Q.GetEncoded()),
                TxnData = new PendingTxn
                {
                    ProposalName = "Propose",
                    TxnData = CreateDemoTxn(msig).ToByteString()
                }
            };
            res = _contract.Propose(notFoundAuthorizationProposal, kp1).Result;
            Assert.Null(res);
            
            var notAuthorizedProposal = new Proposal
            {
                ExpiredTime = Timestamp.FromDateTime(DateTime.UtcNow.AddSeconds(10)),
                MultiSigAccount = msig,
                Name = "Propose",
                //ProposerPublicKey = ByteString.CopyFrom(kp3.PublicKey.Q.GetEncoded()),
                TxnData = new PendingTxn
                {
                    ProposalName = "Propose",
                    TxnData = CreateDemoTxn(msig).ToByteString()
                }
            };
            res = _contract.Propose(notAuthorizedProposal, kp3).Result;
            Assert.Null(res);
            
            var validProposal = new Proposal
            {
                ExpiredTime = Timestamp.FromDateTime(DateTime.UtcNow.AddSeconds(10)),
                MultiSigAccount = msig,
                Name = "Propose",
                //ProposerPublicKey = ByteString.CopyFrom(kp1.PublicKey.Q.GetEncoded()),
                TxnData = new PendingTxn
                {
                    ProposalName = "Propose",
                    TxnData = CreateDemoTxn(msig).ToByteString()
                },
                Proposer = Address.FromRawBytes(kp1.GetEncodedPublicKey()),
                Status = ProposalStatus.ToBeDecided
            };
            res = _contract.Propose(validProposal, kp1).Result;
            Assert.NotEmpty(res);
        }

        [Fact]
        public void ProposeValidProposal()
        {
            Init();
            _contract = new AuthorizationContractShim(Mock, ContractHelpers.GetAuthorizationContractAddress(Mock.ChainId1));
            Address msig = Address.Generate();
            var auth = new Kernel.Types.Proposal.Authorization
            {
                ExecutionThreshold = 2,
                MultiSigAccount = msig,
                ProposerThreshold = 1
            };
            var kp1 = new KeyPairGenerator().Generate();
            var kp2 = new KeyPairGenerator().Generate();
            var kp3 = new KeyPairGenerator().Generate();
            auth.Reviewers.AddRange(new[]
            {
                new Reviewer
                {
                    PubKey = ByteString.CopyFrom(kp1.PublicKey.Q.GetEncoded()),
                    Weight = 1
                },
                new Reviewer
                {
                    PubKey = ByteString.CopyFrom(kp2.PublicKey.Q.GetEncoded()),
                    Weight = 1
                },
                new Reviewer
                {
                    PubKey = ByteString.CopyFrom(kp3.PublicKey.Q.GetEncoded()),
                    Weight = 0
                }
            });
            var addr = _contract.CreateMSigAccount(auth).Result;
            Assert.Equal(msig.DumpByteArray(), addr);

            var validProposal = new Proposal
            {
                ExpiredTime = Timestamp.FromDateTime(DateTime.UtcNow.AddSeconds(10)),
                MultiSigAccount = msig,
                Name = "Propose",
                Proposer = Address.FromRawBytes(kp1.GetEncodedPublicKey()),
                TxnData = new PendingTxn
                {
                    ProposalName = "Propose",
                    TxnData = CreateDemoTxn(msig).ToByteString()
                }
            };
            var res = _contract.Propose(validProposal, kp1).Result;
            Assert.NotEmpty(res);
        }

        [Fact]
        public void SayYes()
        {
            Init();
            _contract = new AuthorizationContractShim(Mock, ContractHelpers.GetAuthorizationContractAddress(Mock.ChainId1));
            Address msig = Address.Generate();
            var auth = new Kernel.Types.Proposal.Authorization
            {
                ExecutionThreshold = 2,
                MultiSigAccount = msig,
                ProposerThreshold = 1
            };
            var kp1 = new KeyPairGenerator().Generate();
            var kp2 = new KeyPairGenerator().Generate();
            var kp3 = new KeyPairGenerator().Generate();
            auth.Reviewers.AddRange(new[]
            {
                new Reviewer
                {
                    PubKey = ByteString.CopyFrom(kp1.GetEncodedPublicKey()),
                    Weight = 1
                },
                new Reviewer
                {
                    PubKey = ByteString.CopyFrom(kp2.GetEncodedPublicKey()),
                    Weight = 1
                },
                new Reviewer
                {
                    PubKey = ByteString.CopyFrom(kp3.GetEncodedPublicKey()),
                    Weight = 0
                }
            });
            var addr = _contract.CreateMSigAccount(auth).Result;
            Assert.Equal(msig.DumpByteArray(), addr);

            var tx = CreateDemoTxn(msig);
            var validProposal = new Proposal
            {
                ExpiredTime = Timestamp.FromDateTime(DateTime.UtcNow.AddSeconds(10)),
                MultiSigAccount = msig,
                Name = "Propose",
                Proposer = kp1.GetAddress(),
                TxnData = new PendingTxn
                {
                    ProposalName = "Propose",
                    TxnData = tx.ToByteString()
                }
            };
            var res = _contract.Propose(validProposal, kp1).Result;
            Assert.NotEmpty(res);
            
            var newKp = new KeyPairGenerator().Generate();
            ECSigner signer = new ECSigner();
            ECSignature signature = signer.Sign(newKp, tx.GetHash().DumpByteArray());
            var notAuthorizedApproval = new Approval
            {
                ProposalHash = Hash.LoadByteArray(res),
                Signature = new Sig
                {
                    P = ByteString.CopyFrom(newKp.GetEncodedPublicKey()),
                    R = ByteString.CopyFrom(signature.R),
                    S = ByteString.CopyFrom(signature.S)
                }
            };
            var sayYesRes = _contract.SayYes(notAuthorizedApproval, Address.FromRawBytes(newKp.GetEncodedPublicKey())).Result;
            Assert.False(sayYesRes);
            
            signature = signer.Sign(kp1, tx.GetHash().DumpByteArray());
            var validApproval = new Approval
            {
                ProposalHash = Hash.LoadByteArray(res),
                Signature = new Sig
                {
                    P = ByteString.CopyFrom(kp1.GetEncodedPublicKey()),
                    R = ByteString.CopyFrom(signature.R),
                    S = ByteString.CopyFrom(signature.S)
                }
            };
            Console.WriteLine(Hash.FromRawBytes(validApproval.Signature.P.ToByteArray()));
            sayYesRes = _contract.SayYes(validApproval, Address.FromRawBytes(kp1.GetEncodedPublicKey())).Result;
            Assert.True(sayYesRes);
            
            // say yes again
            sayYesRes = _contract.SayYes(validApproval, Address.FromRawBytes(kp1.GetEncodedPublicKey())).Result;
            Assert.False(sayYesRes);
        }
        
        
        [Fact]
        public void Release()
        {
            Init();
            _contract = new AuthorizationContractShim(Mock, ContractHelpers.GetAuthorizationContractAddress(Mock.ChainId1));
            Address msig = Address.Generate();
            var auth = new Kernel.Types.Proposal.Authorization
            {
                ExecutionThreshold = 2,
                MultiSigAccount = msig,
                ProposerThreshold = 1
            };
            var kp1 = new KeyPairGenerator().Generate();
            var kp2 = new KeyPairGenerator().Generate();
            var kp3 = new KeyPairGenerator().Generate();
            auth.Reviewers.AddRange(new[]
            {
                new Reviewer
                {
                    PubKey = ByteString.CopyFrom(kp1.PublicKey.Q.GetEncoded()),
                    Weight = 1
                },
                new Reviewer
                {
                    PubKey = ByteString.CopyFrom(kp2.PublicKey.Q.GetEncoded()),
                    Weight = 1
                },
                new Reviewer
                {
                    PubKey = ByteString.CopyFrom(kp3.PublicKey.Q.GetEncoded()),
                    Weight = 0
                }
            });
            var addr = _contract.CreateMSigAccount(auth).Result;
            Assert.Equal(msig.DumpByteArray(), addr);

            var tx = CreateDemoTxn(msig);
            var validProposal = new Proposal
            {
                ExpiredTime = Timestamp.FromDateTime(DateTime.UtcNow.AddSeconds(10)),
                MultiSigAccount = msig,
                Name = "Propose",
                Proposer = kp1.GetAddress(),
                TxnData = new PendingTxn
                {
                    ProposalName = "Propose",
                    TxnData = tx.ToByteString()
                }
            };
            var res = _contract.Propose(validProposal, kp1).Result;
            Assert.NotEmpty(res);
            
            ECSigner signer = new ECSigner();
            
            // first approval
            ECSignature signature = signer.Sign(kp1, tx.GetHash().DumpByteArray());
            var validApproval1 = new Approval
            {
                ProposalHash = Hash.LoadByteArray(res),
                Signature = new Sig
                {
                    P = ByteString.CopyFrom(kp1.GetEncodedPublicKey()),
                    R = ByteString.CopyFrom(signature.R),
                    S = ByteString.CopyFrom(signature.S)
                }
            };
            var res1 = _contract.SayYes(validApproval1, Address.FromRawBytes(kp1.GetEncodedPublicKey())).Result;
            Assert.True(res1);
            
            // second approval 
            signature = signer.Sign(kp3, tx.GetHash().DumpByteArray());
            var validApproval2 = new Approval
            {
                ProposalHash = Hash.LoadByteArray(res),
                Signature = new Sig
                {
                    P = ByteString.CopyFrom(kp3.GetEncodedPublicKey()),
                    R = ByteString.CopyFrom(signature.R),
                    S = ByteString.CopyFrom(signature.S)
                }
            };
            var res2 = _contract.SayYes(validApproval2, Address.FromRawBytes(kp3.GetEncodedPublicKey())).Result;
            Assert.True(res2);

            // not enough authorization
            var txnHash = _contract.Release(Hash.LoadByteArray(res), Address.Generate()).Result;
            Assert.Null(txnHash);
            
            // third approval 
            signature = signer.Sign(kp2, tx.GetHash().DumpByteArray());
            var validApproval3 = new Approval
            {
                ProposalHash = Hash.LoadByteArray(res),
                Signature = new Sig
                {
                    P = ByteString.CopyFrom(kp2.GetEncodedPublicKey()),
                    R = ByteString.CopyFrom(signature.R),
                    S = ByteString.CopyFrom(signature.S)
                }
            };
            var res3 = _contract.SayYes(validApproval3, Address.FromRawBytes(kp2.GetEncodedPublicKey())).Result;
            Assert.True(res3);
            
            txnHash = _contract.Release(Hash.LoadByteArray(res), Address.Generate()).Result;
            Assert.NotNull(txnHash);
            Assert.Equal(msig, txnHash.From);
            Assert.True(new TxSignatureVerifier().Verify(txnHash));
        }
        
        
        private Transaction CreateDemoTxn(Address msig)
        {
            byte[] code;
            using (var file = File.OpenRead(Path.GetFullPath("../../../../AElf.Contracts.Token/bin/Debug/netstandard2.0/AElf.Contracts.Token.dll")))
            {
                code = file.ReadFully();
            }
            var tx = new Transaction
            {
                From = msig,
                To = ContractHelpers.GetGenesisBasicContractAddress(Mock.ChainId1),
                MethodName = "DeploySmartContract",
                Params = ByteString.CopyFrom(ParamsPacker.Pack(1, code)),
                Type = TransactionType.MsigTransaction
            };
            return tx;
        }
    }
}