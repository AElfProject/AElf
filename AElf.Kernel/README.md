# AELF.Kernel

```
             MINIMAL VIABLE PROCESS PIPELINE FOR AELF.KERNEL



      +------------------+    +---------------+    +--------------+
      |                  |    |               |    |              +------+
  +-----+ TX Receiver <---------  TX Sender  <------+ Tranactions |------|
  |   |                  |    |               |    |              +------+
  |   +------------------+    +---------------+    +--------------+
  |
  |
  |   +------------------+    +---------------+     +-----------------------+
  |   |                  |    |               |     |                       |
  +----> Block Producer +------->  Mining   +-------->  Block(nonce filled) |
      |                  |    |               |     |                       |
      +------------------+    +---------------+     +------------+----------+
                                                                 |
                                                                 |
                                                                 |
 +-----------------------+   +---------------------+      +------v--------+
 |                       |   |                     |      |               |
 |  Block Verification <-------  Block Receiver    <-------+ Block Sender |
 |                       |   |                     |      |               |
 +-----+-----------------+   +---------------------+      +---------------+
       |
       |
       |
+------v---------+   +---------------+   +--------------+   +-----------------+
|                |   |               |   |              |   |                 |
|   Scheduler   +----->  Workers +---------> Reducer  +------->  WorldState   |
|                |   |               |   |              |   |                 |
+----------------+   +---------------+   +--------------+   +-----------------+


```

A builtin demo of minimal AELF blockchain contains:

* A Tranaction implemented with dummy content.
* A PoW miner with nBits(difficulty) equal to 1.
* A FIFO for TX sender & receiver
* A FIFO for block sender & receiver
* A scheduler makes the transaction execute asynchronously but locally.
* A Reducer collects all the results from workers.
* A worldstate built on memory key-value set(Dictionary).

(working in progress).
