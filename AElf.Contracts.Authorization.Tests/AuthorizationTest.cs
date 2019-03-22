//using System;
//using System.IO;
//using AElf.Common;
//using AElf.Cryptography;
//using AElf.Cryptography.ECDSA;
//using AElf.Kernel;
//using AElf.Kernel.Types;
//using AElf.Types.CSharp;
//using Google.Protobuf;
//using Google.Protobuf.WellKnownTypes;
//using Microsoft.Extensions.Logging;
//using Microsoft.Extensions.Logging.Abstractions;
//using Xunit;


/*
namespace AElf.Contracts.Authorization.Tests
{
public class AuthorizationTest : AuthroizationContractTestBase
    {
        private AuthorizationContractShim _contract;
        public ILogger<AuthorizationTest> Logger {get;set;}
        private MockSetup Mock;

        //private static byte[] ChainId = ChainHelpers.GetRandomChainId();
        
        public AuthorizationTest()
        {
            
            Logger = NullLogger<AuthorizationTest>.Instance;
            Mock = GetRequiredService<MockSetup>();

        }

        [Fact]
        public void CreateMSigAccount()
        {
            _contract = new AuthorizationContractShim(Mock, ContractHelpers.GetAuthorizationContractAddress(Mock.ChainId), Mock.ChainId.DumpByteArray());
            Address msig = Address.Generate();
            var auth = new Kernel.Authorization
            {
                ExecutionThreshold = 2,
                MultiSigAccount = msig,
                ProposerThreshold = 1
            };
            var kp1 = CryptoHelpers.GenerateKeyPair();
            var kp2 = CryptoHelpers.GenerateKeyPair();
            var kp3 = CryptoHelpers.GenerateKeyPair();
            auth.Reviewers.AddRange(new[]
            {
                new Reviewer
                {
                    PubKey = ByteString.CopyFrom(kp1.PublicKey),
                    Weight = 1
                },
                new Reviewer
                {
                    PubKey = ByteString.CopyFrom(kp2.PublicKey),
                    Weight = 1
                },
                new Reviewer
                {
                    PubKey = ByteString.CopyFrom(kp3.PublicKey),
                    Weight = 0
                }
            });
            var addr = _contract.CreateMSigAccount(auth).Result;
            Assert.Equal(msig.DumpByteArray(), addr);
        }

        [Fact]
        public void ProposeInvalidProposal()
        {
            _contract = new AuthorizationContractShim(Mock, ContractHelpers.GetAuthorizationContractAddress(Mock.ChainId), Mock.ChainId.DumpByteArray());
            
            // todo review link a keypair to msig account, for now just to generate the address from pubkey
            var kpMsig = CryptoHelpers.GenerateKeyPair();
            Address msig = Address.FromPublicKey(kpMsig.PublicKey);
            
            var auth = new Kernel.Authorization
            {
                ExecutionThreshold = 2,
                MultiSigAccount = msig,
                ProposerThreshold = 1
            };
            
            var kp1 = CryptoHelpers.GenerateKeyPair();
            var kp2 = CryptoHelpers.GenerateKeyPair();
            var kp3 = CryptoHelpers.GenerateKeyPair();
            
            auth.Reviewers.AddRange(new[]
            {
                new Reviewer
                {
                    PubKey = ByteString.CopyFrom(kp1.PublicKey),
                    Weight = 1
                },
                new Reviewer
                {
                    PubKey = ByteString.CopyFrom(kp2.PublicKey),
                    Weight = 1
                },
                new Reviewer
                {
                    PubKey = ByteString.CopyFrom(kp3.PublicKey),
                    Weight = 0
                }
            });
            
            var addr = _contract.CreateMSigAccount(auth).Result;
            Assert.Equal(msig.DumpByteArray(), addr);
            
            var expiredProposal = new Proposal
            {
                ExpiredTime = TimerHelper.ConvertToUnixTimestamp(DateTime.UtcNow.AddSeconds(-10)),
                MultiSigAccount = msig,
                Name = "Propose",
                TxnData = CreateDemoTxn(msig).ToByteString()
            };
            
            var res = _contract.Propose(expiredProposal, kp1).Result;
            //Hash hash = Hash.LoadByteArray(res);
            Assert.Null(res);
            
            var notFoundAuthorizationProposal = new Proposal
            {
                ExpiredTime = TimerHelper.ConvertToUnixTimestamp(DateTime.UtcNow.AddSeconds(10)),
                MultiSigAccount = Address.Generate(),
                Name = "Propose",
                //ProposerPublicKey = ByteString.CopyFrom(kp1.PublicKey),
                TxnData = CreateDemoTxn(msig).ToByteString()
            };
            
            res = _contract.Propose(notFoundAuthorizationProposal, kp1).Result;
            Assert.Null(res);
            
            var notAuthorizedProposal = new Proposal
            {
                ExpiredTime = TimerHelper.ConvertToUnixTimestamp(DateTime.UtcNow.AddSeconds(10)),
                MultiSigAccount = msig,
                Name = "Propose",
                //ProposerPublicKey = ByteString.CopyFrom(kp3.PublicKey),
                TxnData = CreateDemoTxn(msig).ToByteString()
            };
            
            res = _contract.Propose(notAuthorizedProposal, kp3).Result;
            Assert.Null(res);
            
            var validProposal = new Proposal
            {
                ExpiredTime = TimerHelper.ConvertToUnixTimestamp(DateTime.UtcNow.AddSeconds(10)),
                MultiSigAccount = msig,
                Name = "Propose",
                TxnData = CreateDemoTxn(msig).ToByteString(),
                Proposer = Address.FromPublicKey(kp1.PublicKey),
                Status = ProposalStatus.ToBeDecided
            };
            
            res = _contract.Propose(validProposal, kp1).Result;
            Assert.NotEmpty(res);
        }

        [Fact]
        public void ProposeValidProposal()
        {
            _contract = new AuthorizationContractShim(Mock, ContractHelpers.GetAuthorizationContractAddress(Mock.ChainId), Mock.ChainId.DumpByteArray());
            
            // todo review link a keypair to msig account, for now just to generate the address from pubkey
            var kpMsig = CryptoHelpers.GenerateKeyPair();
            Address msig = Address.FromPublicKey(kpMsig.PublicKey);
            
            var auth = new Kernel.Authorization
            {
                ExecutionThreshold = 2,
                MultiSigAccount = msig,
                ProposerThreshold = 1
            };
            
            var kp1 = CryptoHelpers.GenerateKeyPair();
            var kp2 = CryptoHelpers.GenerateKeyPair();
            var kp3 = CryptoHelpers.GenerateKeyPair();
            
            auth.Reviewers.AddRange(new[]
            {
                new Reviewer
                {
                    PubKey = ByteString.CopyFrom(kp1.PublicKey),
                    Weight = 1
                },
                new Reviewer
                {
                    PubKey = ByteString.CopyFrom(kp2.PublicKey),
                    Weight = 1
                },
                new Reviewer
                {
                    PubKey = ByteString.CopyFrom(kp3.PublicKey),
                    Weight = 0
                }
            });
            
            var addr = _contract.CreateMSigAccount(auth).Result;
            Assert.Equal(msig.DumpByteArray(), addr);

            var validProposal = new Proposal
            {
                ExpiredTime = TimerHelper.ConvertToUnixTimestamp(DateTime.UtcNow.AddSeconds(10)),
                MultiSigAccount = msig,
                Name = "Propose",
                Proposer = Address.FromPublicKey(kp1.PublicKey),
                TxnData = CreateDemoTxn(msig).ToByteString()
            };
            
            var res = _contract.Propose(validProposal, kp1).Result;
            
            Assert.NotEmpty(res);
        }

        [Fact]
        public void SayYes()
        {
            _contract = new AuthorizationContractShim(Mock, ContractHelpers.GetAuthorizationContractAddress(Mock.ChainId), Mock.ChainId.DumpByteArray());
            
            // todo review link a keypair to msig account, for now just to generate the address from pubkey
            var kpMsig = CryptoHelpers.GenerateKeyPair();
            Address msig = Address.FromPublicKey(kpMsig.PublicKey);
            
            var auth = new Kernel.Authorization
            {
                ExecutionThreshold = 2,
                MultiSigAccount = msig,
                ProposerThreshold = 1
            };
            
            var kp1 = CryptoHelpers.GenerateKeyPair();
            var kp2 = CryptoHelpers.GenerateKeyPair();
            var kp3 = CryptoHelpers.GenerateKeyPair();
            
            auth.Reviewers.AddRange(new[]
            {
                new Reviewer
                {
                    PubKey = ByteString.CopyFrom(kp1.PublicKey),
                    Weight = 1
                },
                new Reviewer
                {
                    PubKey = ByteString.CopyFrom(kp2.PublicKey),
                    Weight = 1
                },
                new Reviewer
                {
                    PubKey = ByteString.CopyFrom(kp3.PublicKey),
                    Weight = 0
                }
            });
            
            var addr = _contract.CreateMSigAccount(auth).Result;
            Assert.Equal(msig.DumpByteArray(), addr);

            var tx = CreateDemoTxn(msig);
            
            var validProposal = new Proposal
            {
                ExpiredTime = TimerHelper.ConvertToUnixTimestamp(DateTime.UtcNow.AddSeconds(10)),
                MultiSigAccount = msig,
                Name = "Propose",
                Proposer = Address.FromPublicKey(kp1.PublicKey),
                TxnData = tx.ToByteString()
            };
            
            var res = _contract.Propose(validProposal, kp1).Result;
            Assert.NotEmpty(res);
            
            var newKp = CryptoHelpers.GenerateKeyPair();
            
            var signature = CryptoHelpers.SignWithPrivateKey(newKp.PrivateKey, tx.GetHash().DumpByteArray());
            
            var notAuthorizedApproval = new Approval
            {
                ProposalHash = Hash.LoadByteArray(res),
                Signature = ByteString.CopyFrom(signature)
            };
            
            var sayYesRes = _contract.SayYes(notAuthorizedApproval, newKp).Result;
            Assert.False(sayYesRes);
            
            var signatureKp1 = CryptoHelpers.SignWithPrivateKey(kp1.PrivateKey, tx.GetHash().DumpByteArray());
            var validApproval = new Approval
            {
                ProposalHash = Hash.LoadByteArray(res),
                Signature = ByteString.CopyFrom(signatureKp1) 
            };
            
            // todo review print
            //Console.WriteLine(Hash.FromRawBytes(validApproval.Signature.P.ToByteArray()));
            
            sayYesRes = _contract.SayYes(validApproval, kp1).Result;
            Assert.True(sayYesRes);
            
            // say yes again
            sayYesRes = _contract.SayYes(validApproval, kp2).Result;
            Assert.False(sayYesRes);
        }
        
        
        [Fact]
        public void Release()
        {
            _contract = new AuthorizationContractShim(Mock, ContractHelpers.GetAuthorizationContractAddress(Mock.ChainId), Mock.ChainId.DumpByteArray());
            
            // todo review link a keypair to msig account, for now just to generate the address from pubkey
            var kpMsig = CryptoHelpers.GenerateKeyPair();
            Address msig = Address.FromPublicKey(kpMsig.PublicKey);
            
            var auth = new Kernel.Authorization
            {
                ExecutionThreshold = 2,
                MultiSigAccount = msig,
                ProposerThreshold = 1
            };
            
            var kp1 = CryptoHelpers.GenerateKeyPair();
            var kp2 = CryptoHelpers.GenerateKeyPair();
            var kp3 = CryptoHelpers.GenerateKeyPair();
            
            auth.Reviewers.AddRange(new[]
            {
                new Reviewer
                {
                    PubKey = ByteString.CopyFrom(kp1.PublicKey),
                    Weight = 1
                },
                new Reviewer
                {
                    PubKey = ByteString.CopyFrom(kp2.PublicKey),
                    Weight = 1
                },
                new Reviewer
                {
                    PubKey = ByteString.CopyFrom(kp3.PublicKey),
                    Weight = 0
                }
            });
            
            var addr = _contract.CreateMSigAccount(auth).Result;
            Assert.Equal(msig.DumpByteArray(), addr);

            var tx = CreateDemoTxn(msig);
            
            var validProposal = new Proposal
            {
                ExpiredTime = TimerHelper.ConvertToUnixTimestamp(DateTime.UtcNow.AddSeconds(10)),
                MultiSigAccount = msig,
                Name = "Propose",
                Proposer = Address.FromPublicKey(kp1.PublicKey),
                TxnData = tx.ToByteString()
            };
            
            var res = _contract.Propose(validProposal, kp1).Result;
            Assert.NotEmpty(res);
                        
            // first approval
            var signature = CryptoHelpers.SignWithPrivateKey(kp1.PrivateKey, tx.GetHash().DumpByteArray());
            var validApproval1 = new Approval
            {
                ProposalHash = Hash.LoadByteArray(res),
                Signature = ByteString.CopyFrom(signature)
            };
            
            var res1 = _contract.SayYes(validApproval1, kp1).Result;
            Assert.True(res1);
            
            // second approval 
            var signatureKp3 = CryptoHelpers.SignWithPrivateKey(kp3.PrivateKey, tx.GetHash().DumpByteArray());
            var validApproval2 = new Approval
            {
                ProposalHash = Hash.LoadByteArray(res),
                Signature = ByteString.CopyFrom(signatureKp3)
            };
            
            var res2 = _contract.SayYes(validApproval2, kp3).Result;
            Assert.True(res2);

            // not enough authorization
            var txnHash = _contract.Release(Hash.LoadByteArray(res), CryptoHelpers.GenerateKeyPair()).Result;
            Assert.Null(txnHash);
            
            // third approval 
            var signatureKp2 = CryptoHelpers.SignWithPrivateKey(kp2.PrivateKey, tx.GetHash().DumpByteArray());
            var validApproval3 = new Approval
            {
                ProposalHash = Hash.LoadByteArray(res),
                Signature = ByteString.CopyFrom(signatureKp2)
            };
            var res3 = _contract.SayYes(validApproval3, kp2).Result;
            Assert.True(res3);
            
            txnHash = _contract.Release(Hash.LoadByteArray(res), CryptoHelpers.GenerateKeyPair()).Result;
            
            Assert.NotNull(txnHash);
            Assert.Equal(msig, txnHash.From);
            Assert.True(txnHash.VerifySignature());
        }
        
        
        private Transaction CreateDemoTxn(Address msig)
        {
            byte[] code = File.ReadAllBytes(Path.GetFullPath("../../../../AElf.Contracts.Token/bin/Debug/netstandard2.0/AElf.Contracts.Token.dll"));
            var tx = new Transaction
            {
                From = msig,
                To = ContractHelpers.GetGenesisBasicContractAddress(Mock.ChainId),
                MethodName = "DeploySmartContract",
                Params = ByteString.CopyFrom(ParamsPacker.Pack(1, code)),

            };
            return tx;
        }
    }
}
*/