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
   |   +------------------+   +----------------+   +---------------+
   |   |                  |   |                |   |               |
   +----> Block Producer +------> Scheduler   +----->  Workers     +-------+
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
+--------+---------+  +---------------------+   -----------------------+     +-----------------+
|                  |  |                     |  |                       |     |                 |
|   Block Sender  +-----> Block Receiver  +-----> Block-Verification +--------->  WorldState   |
|                  |  |                     |  |                  +    |     |                 |
+------------------+  +---------------------+  +------------------+----+     +-----------------+


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
