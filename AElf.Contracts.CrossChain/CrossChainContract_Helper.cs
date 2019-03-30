using System.Collections.Generic;
using System.Linq;
using AElf.Common;
using AElf.Consensus.DPoS;
using AElf.Contracts.Consensus.DPoS.SideChain;
using AElf.Contracts.MultiToken.Messages;
using AElf.CrossChain;
using AElf.Kernel;
using AElf.Sdk.CSharp.State;
using AElf.Types.CSharp.Utils;
using Google.Protobuf;
using Google.Protobuf.WellKnownTypes;

namespace AElf.Contracts.CrossChain
{
    public partial class CrossChainContract
    {
        private Hash Propose(string proposalName, int waitingPeriod, Address fromAddress,
            Address targetAddress, string invokingMethod, IMessage input)
        {
            // packed txn
            byte[] txnData = new Transaction
            {
                From = fromAddress,
                To = targetAddress,
                MethodName = invokingMethod,
                Params = input.ToByteString(),
            }.ToByteArray();
            var expiredTime = Context.CurrentBlockTime.AddSeconds(waitingPeriod).ToUniversalTime();
            Proposal proposal = new Proposal
            {
                MultiSigAccount = fromAddress,
                Name = proposalName,
                TxnData = ByteString.CopyFrom(txnData),
                ExpiredTime = Timestamp.FromDateTime(expiredTime),
                Status = ProposalStatus.ToBeDecided,
                Proposer = Context.Sender
            };
            //State.AuthorizationContract.Propose(proposal);
            return proposal.GetHash();
        }

//        private bool IsMiner()
//        {
//            var roundNumber = State.ConsensusContract.GetCurrentRoundNumber();
//            var round = State.ConsensusContract.GetRoundInformation(roundNumber);
//            var miners = new Miners {PublicKeys = {round.RealTimeMinersInformation.Keys}};
//            return miners.PublicKeys.Any(p => ByteArrayHelpers.FromHexString(p).BytesEqual(Context.RecoverPublicKey()));
//        }
        private void CheckAuthority(Address fromAddress = null)
        {
//            Assert(fromAddress == null || fromAddress.Equals(Context.Sender), "Not authorized transaction.");
//            if (Context.Transaction.Sigs.Count == 1)
//                // No need to verify signature again if it is not multi sig account.
//                return;
//            var auth = State.AuthorizationContract.GetAuthorization(Context.Sender);
//
//            // Get tx hash
//            var hash = Context.TransactionId.DumpByteArray();
//
//            // Get pub keys
//            var publicKeys = new List<byte[]>();
//            foreach (var sig in Context.Transaction.Sigs)
//            {
//                var publicKey = Context.RecoverPublicKey(sig.ToByteArray(), hash);
//                Assert (publicKey != null, "Invalid signature."); // this should never happen.
//                publicKeys.Add(publicKey);
//            }
//            
//            // review correctness
//            uint provided = publicKeys
//                .Select(pubKey => auth.Reviewers.FirstOrDefault(r => r.PubKey.ToByteArray().SequenceEqual(pubKey)))
//                .Where(r => !(r is default(Reviewer))).Aggregate<Reviewer, uint>(0, (current, r) => current + r.Weight);
//            Assert(provided >= auth.ExecutionThreshold, "Authorization failed without enough approval.");
        }
        
        /// <summary>
        /// Bind parent chain height together with self height.
        /// </summary>
        /// <param name="childHeight"></param>
        /// <param name="parentHeight"></param>
        private void BindParentChainHeight(long childHeight, long parentHeight)
        {
            Assert(State.ChildHeightToParentChainHeight[childHeight] == 0,
                $"Already bound at height {childHeight} with parent chain");
            State.ChildHeightToParentChainHeight[childHeight] = parentHeight;
        }


        private Hash ComputeRootWithTransactionStatusMerklePath(Hash txId, IEnumerable<Hash> path)
        {
            var txResultStatusRawBytes =
                EncodingHelper.GetBytesFromUtf8String(TransactionResultStatus.Mined.ToString());
            return new MerklePath(path).ComputeRootWith(
                Hash.FromRawBytes(txId.DumpByteArray().Concat(txResultStatusRawBytes).ToArray()));
        }

        private Hash ComputeRootWirhMultiHash(IEnumerable<Hash> nodes)
        {
            var binaryMerkleTree = new BinaryMerkleTree();
            binaryMerkleTree.AddNodes(nodes);
            return binaryMerkleTree.ComputeRootHash();
        }
        
        /// <summary>
        /// Record merkle path of self chain block, which is from parent chain. 
        /// </summary>
        /// <param name="height"></param>
        /// <param name="path"></param>
        private void AddIndexedTxRootMerklePathInParentChain(long height, MerklePath path)
        {
            var existing = State.TxRootMerklePathInParentChain[height];
            Assert(existing == null,
                $"Merkle path already bound at height {height}.");
            State.TxRootMerklePathInParentChain[height] = path;
        }

        private void LockTokenAndResource(SideChainInfo sideChainInfo)
        {
            //Api.Assert(request.Proposer.Equals(Api.GetFromAddress()), "Unable to lock token or resource.");
            // update locked token balance
            
            var balance = GetBalance(new GetBalanceInput
            {
                Owner = Context.Sender,
                Symbol = "ELF"
            });

            Assert(balance > 0);
            
            TransferFrom(new TransferFromInput
            {
                From = Context.Sender,
                To = Context.Self,
                Amount = sideChainInfo.LockedTokenAmount,
                Symbol = "ELF"
            });
            var chainId = sideChainInfo.SideChainId;
            State.IndexingBalance[chainId] = sideChainInfo.LockedTokenAmount;
            // Todo: enable resource
            // lock 
            /*foreach (var resourceBalance in sideChainInfo.ResourceBalances)
            {
                Api.LockResource(resourceBalance.Amount, resourceBalance.Type);
            }*/
        }

        private void UnlockTokenAndResource(SideChainInfo sideChainInfo)
        {
            //Api.Assert(sideChainInfo.LockedAddress.Equals(Api.GetFromAddress()), "Unable to withdraw token or resource.");
            // unlock token
            var chainId = sideChainInfo.SideChainId;
            var balance = State.IndexingBalance[chainId];
            if (balance != 0)
                Transfer(new TransferInput
                {
                    To = sideChainInfo.Proposer,
                    Amount = balance,
                    Symbol = "ELF"
                });
            State.IndexingBalance[chainId] = 0;

            // unlock resource 
            /*foreach (var resourceBalance in sideChainInfo.ResourceBalances)
            {
                Api.UnlockResource(resourceBalance.Amount, resourceBalance.Type);
            }*/
        }

        private void ValidateContractState(ContractReferenceState state, Hash contractSystemName)
        {
            if (state.Value != null)
                return;
            state.Value = State.BasicContractZero.GetContractAddressByName.Call(contractSystemName);
        }

        private void Transfer(TransferInput input)
        {
            ValidateContractState(State.TokenContract, State.TokenContractSystemName.Value);
            State.TokenContract.Transfer.Send(input);
        }

        private void TransferFrom(TransferFromInput input)
        {
            ValidateContractState(State.TokenContract, State.TokenContractSystemName.Value);
            State.TokenContract.TransferFrom.Send(input);
        }

        private long GetBalance(GetBalanceInput input)
        {
            ValidateContractState(State.TokenContract, State.TokenContractSystemName.Value);
            var output = State.TokenContract.GetBalance.Call(input);
            return output.Balance;
        }

        private MinerList GetCurrentMiners()
        {
            ValidateContractState(State.ConsensusContract, State.ConsensusContractSystemName.Value);
            var miners = State.ConsensusContract.GetCurrentMiners.Call(new Empty());
            var minerList = new MinerList
            {
                TermNumber = miners.TermNumber
            };
            minerList.Addresses.AddRange(miners.Addresses);
            minerList.PublicKeys.AddRange(miners.PublicKeys);
            return minerList;
        }

        // only for side chain
        private void UpdateCurrentMiners(ByteString bytes)
        {
            ValidateContractState(State.ConsensusContract, State.ConsensusContractSystemName.Value);
            State.ConsensusContract.UpdateMainChainConsensus.Send(new ConsensusInformation{Bytes = bytes});
        }
        
        private Hash GetParentChainMerkleTreeRoot(long parentChainHeight)
        {
            return State.ParentChainTransactionStatusMerkleTreeRoot[parentChainHeight];
        }
        
        private Hash GetSideChainMerkleTreeRoot(long parentChainHeight)
        {
            var indexedSideChainData = State.IndexedCrossChainBlockData[parentChainHeight];
            return ComputeRootWirhMultiHash(
                indexedSideChainData.SideChainBlockData.Select(d => d.TransactionMerkleTreeRoot));
        }
        
        private Hash GetCousinChainMerkleTreeRoot(long parentChainHeight)
        {
            return State.TransactionMerkleTreeRootRecordedInParentChain[parentChainHeight];
        }

        private Hash GetMerkleTreeRoot(int chainId, long parentChainHeight)
        {
            if (State.ParentChainId.Value == 0)
            {
                // Local is main chain
                return GetSideChainMerkleTreeRoot(parentChainHeight);
            }

            return chainId != State.ParentChainId.Value
                ? GetCousinChainMerkleTreeRoot(parentChainHeight)
                : GetParentChainMerkleTreeRoot(parentChainHeight);
        }
    }
}