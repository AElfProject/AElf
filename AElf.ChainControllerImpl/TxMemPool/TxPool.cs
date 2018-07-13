using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Linq;
using System.Runtime.InteropServices.WindowsRuntime;
using AElf.Common.Attributes;
using AElf.Kernel;
using NLog;

namespace AElf.ChainController
{
    [LoggerName("TxPool")]
    public class TxPool : ITxPool
    {
        private readonly Dictionary<Hash, List<ITransaction>> _executable =
            new Dictionary<Hash, List<ITransaction>>();

        private readonly Dictionary<Hash, Dictionary<ulong, ITransaction>> _waiting =
            new Dictionary<Hash, Dictionary<ulong, ITransaction>>();

        //private readonly Dictionary<Hash, ITransaction> _pool = new Dictionary<Hash, ITransaction>();
        private readonly ILogger _logger;

        private readonly ITxPoolConfig _config;


        public TxPool(ITxPoolConfig config, ILogger logger)
        {
            _config = config;
            _logger = logger;
        }

        /// <inheritdoc />
        public Hash ChainId => _config.ChainId;

        /// <inheritdoc />
        public uint TxLimitSize => _config.TxLimitSize;

        /// <inheritdoc/>
        public ulong MinimalFee => _config.FeeThreshold;

        private ConcurrentDictionary<Hash, ulong> _nonces  = new ConcurrentDictionary<Hash, ulong>();


        /// <inheritdoc />
        public ulong Size => GetPoolSize();

        /// <inheritdoc/>
        public void ClearAll()
        {
            ClearWaiting();
            ClearExecutable();
            //Tmp.Clear();
            //_pool.Clear();
        }

        /// <inheritdoc/>
        public void ClearWaiting()
        {
            _waiting.Clear();
        }

        /// <inheritdoc/>
        public void ClearExecutable()
        {
            _executable.Clear();
        }


        /// <summary>
        /// return pool size of executable, waiting
        /// </summary>
        public ulong GetPoolSize()
        {
            return GetExecutableSize() + GetWaitingSize();
        }

        /*/// <inheritdoc/>
        public bool AddTx(ITransaction tx)
        {
            var txHash = tx.GetHash();
            
            // TODO: validate tx
            
            if (Contains(txHash) || GetNonce(tx.From) > tx.IncrementId)
                return false;
            
            _pool.Add(txHash, tx);
            Tmp.Add(txHash);
            
            return true;
        }*/

        /// <inheritdoc/>
        public List<ITransaction> ReadyTxs(ulong limit)
        {
            var res = new List<ITransaction>();
            foreach (var kv in _executable)
            {
                if ((ulong) res.Count >= limit)
                    break;
                var nonce = GetNonce(kv.Key);
                var r = 0;
                foreach (var tx in kv.Value)
                {
                    if (tx.IncrementId < nonce) continue;
                    r++;
                    res.Add(tx);
                    if ((ulong) res.Count >= limit)
                        break;
                }

                // update incrementId in account data context
                AddNonce(kv.Key, (ulong) r);

                //remove txs from executable list 
                kv.Value.RemoveRange(0, r);
            }

            return res;
        }

        /// <inheritdoc/>
        public void EnQueueTxs(HashSet<ITransaction> tmp)
        {
            foreach (var tx in tmp)
            {
                EnQueueTx(tx);
            }
        }

        public TxValidation.TxInsertionAndBroadcastingError EnQueueTx(ITransaction tx)
        {
            // disgard the tx if too old
            if (tx.IncrementId < GetNonce(tx.From))
                return TxValidation.TxInsertionAndBroadcastingError.AlreadyExecuted;
            
            var error = this.ValidateTx(tx);
            if (error == TxValidation.TxInsertionAndBroadcastingError.Valid)
            {
                var res = AddWaitingTx(tx);
                if (res)
                {
                    Promote(tx.From);
                }
                
                // return success directlly if the incrementid already inserted
                return TxValidation.TxInsertionAndBroadcastingError.Success;
            }
            _logger.Error("InValid transaction: " + error);
            return error;
        }

        /// <inheritdoc/>
        public bool DiscardTx(ITransaction tx)
        {
            // executable
            if (RemoveFromExecutable(out var unValidTxList, tx))
            {
                // case 1: tx in executable list
                // move unvalid txs to waiting List
                foreach (var hash in unValidTxList)
                {
                    AddWaitingTx(hash);
                }

                return true;
            }

            // in waiting 
            return RemoveFromWaiting(tx);
        }

        /// <inheritdoc/>
        public ulong GetExecutableSize()
        {
            return _executable.Values.Aggregate<List<ITransaction>, ulong>(0,
                (current, p) => current + (ulong) p.Count);
        }

        public void GetPoolState(out ulong executable, out ulong waiting)
        {
            waiting = GetWaitingSize();
            executable = GetExecutableSize();
        }

        /// <inheritdoc/>
        public ulong GetWaitingSize()
        {
            return _waiting.Values.Aggregate<Dictionary<ulong, ITransaction>, ulong>(0,
                (current, p) => current + (ulong) p.Count);
        }


        /// <summary>
        /// replace tx in pool with higher fee
        /// </summary>
        /// <param name="tx"></param>
        /// <param name="oldTx"></param>
        /// <returns></returns>
        private bool ReplaceTx(ITransaction tx, ITransaction oldTx)
        {
            // TODO: compare two tx's fee, choose higher one and disgard the lower
            /*var transaction = _pool[executableList[(int) (tx.IncrementId - nonce)]];
            if (tx.Fee < transaction.Fee)
            {
                
            }*/
            _logger.Error("Replacing transaction failed");
            return false;
        }

        /// <summary>
        /// add tx to waiting list
        /// </summary>
        /// <param name="tx"></param>
        /// <returns></returns>
        private bool AddWaitingTx(ITransaction tx)
        {
            var addr = tx.From;

            // disgard it if already pushed to exectuable list
            if (_executable.TryGetValue(addr, out var executableList) && executableList.Count > 0 &&
                executableList.Last().IncrementId >= tx.IncrementId)
            {
                // todo: try to replace the old one
                return false;
            }

            if (!_waiting.TryGetValue(tx.From, out var waitingList))
            {
                waitingList = _waiting[tx.From] = new Dictionary<ulong, ITransaction>();
            }
            
            if (waitingList.TryGetValue(tx.IncrementId, out var oldTx))
            {
                // todo: try to replace the old one
                return false;
            }

            // add to waiting list
            waitingList.Add(tx.IncrementId, tx);

            return true;
        }


        /// <summary>
        /// remove tx from executable list
        /// </summary>
        /// <param name="unValidTxList">invalid txs because removing this tx</param>
        /// <param name="tx"></param>
        /// <returns></returns>
        private bool RemoveFromExecutable(out IEnumerable<ITransaction> unValidTxList, ITransaction tx = null)
        {
            unValidTxList = null;
            // remove the tx 
            var addr = tx.From;
            var nonce = GetNonce(addr);

            // fail if not exist
            if (!_executable.TryGetValue(addr, out var executableList) ||
                executableList.Count <= (int) (tx.IncrementId - nonce) ||
                !executableList[(int) (tx.IncrementId - nonce)].Equals(tx))
                return false;

            // return unvalid tx because removing 
            unValidTxList = executableList.GetRange((int) (tx.IncrementId - nonce + 1),
                executableList.Count - (int) (tx.IncrementId - nonce + 1));

            // remove
            executableList.RemoveRange((int) (tx.IncrementId - nonce),
                executableList.Count - (int) (tx.IncrementId - nonce));

            // remove the entry if empty
            if (executableList.Count == 0)
                _executable.Remove(addr);

            // Update the account nonce if needed
            /*var context = _accountContextService.GetAccountDataContext(addr, _context.ChainId);
            context.IncreasementId = Math.Min(context.IncreasementId, tx.IncrementId);
            */
            return true;
        }

        /// <inheritdoc/>
        public void Withdraw(Hash addr, ulong withdraw)
        {
            Demote(addr);
            WithdrawNonce(addr, withdraw);
        }


        private void Demote(Hash addr)
        {
            var txs = RemoveExecutableList(addr);
            if (txs == null)
                return;
            foreach (var tx in txs)
            {
                AddWaitingTx(tx);
            }
        }

        private List<ITransaction> RemoveExecutableList(Hash addr)
        {
            // fail if not exist
            if (!_executable.TryGetValue(addr, out var executableList))
                return null;

            _executable.TryRemove(addr, out executableList);
            return executableList;
        }

        /// <summary>
        /// remove tx from waiting list
        /// </summary>
        /// <param name="tx"></param>
        /// <returns></returns>
        private bool RemoveFromWaiting(ITransaction tx)
        {
            var addr = tx.From;
            if (!_waiting.TryGetValue(addr, out var waitingList) ||
                !waitingList.Keys.Contains(tx.IncrementId)) return false;

            // remove the tx from waiting list
            waitingList.Remove(tx.IncrementId);

            // remove from pool
            //_pool.Remove(tx.GetHash());

            // remove the entry if empty
            if (waitingList.Count == 0)
                _waiting.Remove(addr);
            return true;
        }


        /// <summary>
        /// promote txs from waiting to executable list
        /// </summary>
        /// <param name="addrs"></param>
        public void Promote(List<Hash> addrs = null)
        {
            if (addrs == null)
            {
                addrs = _waiting.Keys.ToList();
            }

            foreach (var addr in addrs)
            {
                Promote(addr);
            }
        }


        //public ulong ReadyTxCount { get; private set; }

        private ulong? GetNextPromotableTxId(Hash addr)
        {
            if (!_waiting.TryGetValue(addr, out var waitingList))
            {
                return null;
            }

            // no tx left
            if (waitingList.Count <= 0)
                return null;

            ulong next = waitingList.Keys.Min();

            ulong w = 0;

            if (_executable.TryGetValue(addr, out var executableList) && executableList.Count != 0)
            {
                w = executableList.Last().IncrementId + 1;
            }
            else if (_nonces.TryGetValue(addr, out var n))
            {
                w = n;
                _executable[addr] = new List<ITransaction>();
            }
            else
            {
                return null;
            }


            if (next != w)
                return null;
            return next;
        }

        /// <summary>
        /// remove tx before nonce w
        /// </summary>
        /// <param name="addr"></param>
        /// <param name="w"></param>
        private void DiscardOldTransaction(Hash addr, ulong w)
        {
            var waitingList = _waiting[addr];

            var oldList = waitingList.Keys.Where(n => n < w).Select(n => waitingList[n]);

            // discard
            foreach (var h in oldList)
            {
                RemoveFromWaiting(h);
            }
        }

        /// <summary>
        /// promote ready tx from waiting to exectuable
        /// </summary>
        /// <param name="addr">From account addr</param>
        private void Promote(Hash addr)
        {
            if (!_waiting.ContainsKey(addr))
                return;
            ulong? next = GetNextPromotableTxId(addr);

            if (!next.HasValue)
                return;

            DiscardOldTransaction(addr, next.Value);

            if (!_executable.TryGetValue(addr, out var executableList) ||
                !_waiting.TryGetValue(addr, out var waitingList))
            {
                return;
            }

            ulong incr = next.Value;

            do
            {
                var tx = waitingList[incr];

                // add to executable list
                executableList.Add(tx);
                // remove from waiting list
                waitingList.Remove(incr);
            } while (waitingList.Count > 0 && waitingList.Keys.Contains(++incr));
        }
        
        /// <summary>
        /// update nonce
        /// </summary>
        /// <param name="addr"></param>
        /// <param name="increment"></param>
        /// <returns></returns>
        private void AddNonce(Hash addr, ulong increment)
        {
            var n = GetNonce(addr);
            if(n.HasValue)
                _nonces[addr] = n.Value + increment;
        }

        private void WithdrawNonce(Hash addr, ulong increment)
        {
            var n = GetNonce(addr);
            if(n.HasValue)
                _nonces[addr] = Math.Max(increment, 0);
        }


        /// <inheritdoc/>
        public ulong GetPendingIncrementId(Hash addr)
        {
            return _nonces.TryGetValue(addr, out var incrementId) ? incrementId : (ulong) 0;
        }

        /// <inheritdoc/>
        public bool ReadyTxs(Hash addr, ulong start, ulong count)
        {
            if (!_executable.TryGetValue(addr, out var list) || (ulong) list.Count < count ||
                list[0].IncrementId != start)
            {
                return false;
            }

            // update incrementId in account data context
            AddNonce(addr, count);
            //remove txs from executable list  
            list.RemoveRange(0, (int) count);

            return true;
        }

        /// <inheritdoc/>
        public bool TrySetNonce(Hash addr, ulong incrementId)
        {
            if (!_nonces.TryGetValue(addr, out var id))
            {
                _nonces.TryAdd(addr, incrementId);
                return true;
            }

            return false;
        }
        
        /// <inheritdoc/>
        public ulong? GetNonce(Hash addr)
        {
            if (_nonces.TryGetValue(addr, out var n))
            {
                return n;
            }

            return null;
        }
    }
}