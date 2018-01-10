# AELF.Kernel

```
    MINIMAL VIABLE PROCESS FOR AELF.KERNEL


+--------------+   +------------------+   +------------+   +-----------------------+
|              |   |                  |   |            |   |                       |
| Tranactions +-----> Block(unmined) +----->  Mining +------> Block(nonce filled)  |
|              |   |                  |   |            |   |                       |
+--------------+   +------------------+   +------------+   +----------+------------+
                                                                      |
                                                                      |
                           +---------------------+                    |
                           |                     |                    |
         +------------------+ BLOCK VERIFICATION <--------------------+
         |                 |                     |
         |                 +---------------------+
         |
         |
         |
  +------v---------+   +---------------+   +--------------+   +-----------------+
  |                |   |               |   |              |   |                 |
  |   Scheduler   +----->  Workers +---------> Reducer  +------->  Worldstate   |
  |                |   |               |   |              |   |                 |
  +----------------+   +---------------+   +--------------+   +-----------------+


```

A builtin demo of minimal AELF blockchain contains:

1. A Tranaction implemented with dummy content.
2. A PoW miner with nBits(difficulty) equal to 1.
3. A scheduler makes the transaction execute asynchronously but locally.
4. A Reducer collects all the results from workers.
5. A worldstate built on memory key-value set(Dictionary).

(working in progress).
