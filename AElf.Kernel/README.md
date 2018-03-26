# AELF.Kernel

```
             MINIMAL VIABLE PROCESS PIPELINE FOR AELF.KERNEL



       +------------------+    +---------------+    +--------------+
       |                  |    |               |    |              +------+
+--------+ TX Receiver <---------  TX Sender  <------+ Transactions|------|
|      |                  |    |               |    |              +------+
|      +------------------+    +---------------+    +--------------+
|
|
|      +------------------+   +----------------+   +---------------+
|      |                  |   |                |   |               |
+-------> Block Producer +------> Scheduler   +----->  Workers     +-------+
       |                  |   |                |   |               |       |
       +------------------+   +----------------+   +---------------+       |
                                                                           |
                                                                           |
                                                                           |
 +-----------------------+    +----------------+   +----------------+      |
 |                       |    |                |   |                |      |
 |  Block(nonce filled)  <------+  Mining   <--------+  Reducer     <------+
 |                       |    |                |   |                |
 +-------+---------------+    +----------------+   +----------------+
         |
         |
         |
+------------------+  +---------------------+   -----------------------+     +-----------------+
|        v         |  |                     |  |                       |     |                 |
|   Block Sender  +-----> Block Receiver  +-----> Block-Verification +--------->  WorldState   |
|                  |  |                     |  |                  +    |     |                 |
+------------------+  +---------------------+  +------------------+----+     +-----------------+

```

A builtin demo of minimal AELF blockchain contains:

* A Transaction implemented with dummy content.
* A PoW miner with nBits(difficulty) equal to 1.
* A FIFO for TX sender & receiver
* A FIFO for block sender & receiver
* A scheduler makes the transaction execute asynchronously but locally.
* A Reducer collects all the results from workers.
* A world-state built on memory key-value set(Dictionary).

(working in progress).
