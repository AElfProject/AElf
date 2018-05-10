using System;
using System.Collections.Generic;
using AElf.Kernel.KernelAccount;
using Google.Protobuf;

namespace AElf.Kernel
{
    public class GenesisBlockBuilder
    {
        public Block Block { get; set; }

        public List<Transaction> Txs { get; set; } =new List<Transaction>();


        public GenesisBlockBuilder Build(Type smartContractZero)
        {
            var block = new Block(Hash.Zero)
            {
                    Header = new BlockHeader
                    {
                    Index = 0,
                    PreviousHash = Hash.Zero
                },
                Body = new BlockBody()
            };
            
            var registerTx = new Transaction
            {
                IncrementId = 0,
                MethodName = nameof(ISmartContractZero.RegisterSmartContract),
                To = Hash.Zero,
                From = Hash.Zero,
                Params = ByteString.CopyFrom(
                    new SmartContractRegistration
                        {
                            Category = 0,
                            ContractBytes = ByteString.CopyFromUtf8(smartContractZero.FullName),
                            ContractHash = Hash.Zero
                        }
                        .ToByteArray()
                )
            };
            block.AddTransaction(registerTx.GetHash());

            var deployTx = new Transaction
            {
                IncrementId = 1,
                MethodName = nameof(ISmartContractZero.DeploySmartContract),
                From = Hash.Zero,
                To = Hash.Zero,
                Params = ByteString.CopyFrom(
                    new SmartContractDeployment
                    {
                        ContractHash = Hash.Zero
                    }.ToByteArray()
                )
            };
            block.AddTransaction(deployTx.GetHash());

            
            block.FillTxsMerkleTreeRootInHeader();
            
            Block = block;

            Txs.Add(registerTx);
            Txs.Add(deployTx);

            return this;
        }
    }
}