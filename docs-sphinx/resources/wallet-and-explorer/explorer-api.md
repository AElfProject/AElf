# Explorer-api

[To get more informantion by code](https://github.com/AElfProject/aelf-block-api)

## Block

### Get Block List

```js
URL: api/all/blocks?limit={limit}&page={page}
Method: GET
SuccessResponse:
{
    "total": 5850,
    "blocks": [
        {
            "block_hash": "7e1c2fb6d3cc5e8c2cef7d75de9c1adf0e25e9d17d4f22e543fa20f5f23b20e9",
            "pre_block_hash": "6890fa74156b1a88a3ccef1fef72f4f78ff2755ffcd4fb5434ed7b3c153061f5",
            "chain_id": "AELF",
            "block_height": 5719,
            "tx_count": 1,
            "merkle_root_tx": "47eabbc7a499764d0b25c7216ba75fe39717f9866a0716c8a0d1798e64852d84",
            "merkle_root_state": "d14e78dc3c7811b7c17c8b04ebad9e547c35b3faa8bfcc9189b8c12e9f6a4aae",
            "time": "2019-04-27T02:00:34.691118Z"
        },
        {
            "block_hash": "6890fa74156b1a88a3ccef1fef72f4f78ff2755ffcd4fb5434ed7b3c153061f5",
            "pre_block_hash": "f1098bd6df58acf74d8877529702dffc444cb401fc8236519606aa9165d945ae",
            "chain_id": "AELF",
            "block_height": 5718,
            "tx_count": 1,
            "merkle_root_tx": "b29b416148b4fb79060eb80b49bb6ac25a82da2d7a1c5d341e0bf279a7c57362",
            "merkle_root_state": "4dbef401f6d9ed303cf1b5e609a64b1c06a7fb77620b9d13b0e4649719e2fe55",
            "time": "2019-04-27T02:00:34.691118Z"
        },
        {
            "block_hash": "f1098bd6df58acf74d8877529702dffc444cb401fc8236519606aa9165d945ae",
            "pre_block_hash": "1fbdf3a4fb3c41e9ddf25958715815d9d643dfb39e1aaa94631d197e9b1a94bb",
            "chain_id": "AELF",
            "block_height": 5717,
            "tx_count": 1,
            "merkle_root_tx": "776abba03d66127927edc6437d406f708b64c1653a1cc22af9c490aa4f0c22dc",
            "merkle_root_state": "ccc32ab23d619b2b8e0e9b82a53bb66b3a6d168993188b5d3f7f0ac2cb17206f",
            "time": "2019-04-27T02:00:26.690003Z"
        },
    ]
}

```

### Get Block List By Bock Hash

```js
URL: api/block/transactions?limit={limit}&page={page}&order={order}&block_hash={block_hash}
Method: GET
SuccessResponse:
{
    "transactions": [
        {
            "tx_id": "209ceb8ee88eeb2c55db7783c48ec0b1adf6badba89fc7ddb86e968601027cbb",
            "params_to": "",
            "chain_id": "AELF",
            "block_height": 590,
            "address_from": "csoxW4vTJNT9gdvyWS6W7UqEdkSo9pWyJqBoGSnUHXVnj4ykJ",
            "address_to": "2gaQh4uxg6tzyH1ADLoDxvHA14FMpzEiMqsQ6sDG5iHT8cmjp8",
            "params": "",
            "method": "DeploySmartContract",
            "block_hash": "79584a99b7f5da5959a26ff02cbe174d632eb5ef3c6c8d5192de48b6f5584c8d",
            "quantity": 0,
            "tx_status": "Mined",
            "time": "2019-04-26T06:47:00.265604Z"
        },
        {
            "tx_id": "d9398736920a5c87ea7cae46c265efa84ac7be4cf8edd37bea54078abef1b44c",
            "params_to": "",
            "chain_id": "AELF",
            "block_height": 590,
            "address_from": "2EyPedNTscFK5EwR8FqTrCeW2LZzuPQ7vr18Y5QWuEUApdCkM6",
            "address_to": "xw6U3FRE5H8rU3z8vAgF9ivnWSkxULK5cibdZzMC9UWf7rPJf",
            "params": "",
            "method": "NextRound",
            "block_hash": "79584a99b7f5da5959a26ff02cbe174d632eb5ef3c6c8d5192de48b6f5584c8d",
            "quantity": 0,
            "tx_status": "Mined",
            "time": "2019-04-26T06:47:00.265604Z"
        }
    ]
}
```

## Transactions

### Get Transactions List

```js
URL: api/all/transactions?limit={limit}&page={limit}
Method: GET
SuccessResponse:
{
    "total": 1179,
    "transactions": [
        {
            "tx_id": "c65d1206e65aaf2e7e08cc818c372ff2c2947cb6cbec746efe6a5e20b7adefa9",
            "params_to": "",
            "chain_id": "AELF",
            "block_height": 1064,
            "address_from": "grSAEQ5vJ7UfCN2s1v4fJJnk98bu4SHa2hpQkQ9HT88rmaZLz",
            "address_to": "xw6U3FRE5H8rU3z8vAgF9ivnWSkxULK5cibdZzMC9UWf7rPJf",
            "params": "",
            "method": "NextRound",
            "block_hash": "8c922b20164ad3774b56d19673154f383ed89656cbd56433d1681c8c3a4dcab9",
            "quantity": 0,
            "tx_status": "Mined",
            "time": "2019-04-26T07:18:36.636701Z"
        },
        {
            "tx_id": "4780a7b2737b6f044894719b9bb4cb09862c0b4a7cae267131a0b5c3e7c12850",
            "params_to": "",
            "chain_id": "AELF",
            "block_height": 1063,
            "address_from": "QUYYqzTQmugruHYmuJVftwmVjnUM82pXnMTnT5jh55qwZKrMw",
            "address_to": "xw6U3FRE5H8rU3z8vAgF9ivnWSkxULK5cibdZzMC9UWf7rPJf",
            "params": "",
            "method": "UpdateValue",
            "block_hash": "381114b86b09886f59956851a1d47d8442b29f44f3785dade3c667ca320e23bb",
            "quantity": 0,
            "tx_status": "Mined",
            "time": "2019-04-26T07:18:36.636701Z"
        },
        {
            "tx_id": "0230385e3f060059d2a62addac09ad6d01f96d32ec076cfbf44c6a3b70c6e092",
            "params_to": "",
            "chain_id": "AELF",
            "block_height": 1062,
            "address_from": "zizPhdDpQCZxMChMxn1oZ4ttJGJUo61Aocg5BpTYvzLQGmBjT",
            "address_to": "xw6U3FRE5H8rU3z8vAgF9ivnWSkxULK5cibdZzMC9UWf7rPJf",
            "params": "",
            "method": "NextRound",
            "block_hash": "06a3ceb783480f4cf5b8402f6749617093d9ea5f9a053f65e86554aeed6d98f4",
            "quantity": 0,
            "tx_status": "Mined",
            "time": "2019-04-26T07:18:28.635113Z"
        },
    ]
}
```

### Get Transactions List By Address

```js
URL: api/address/transactions?contract_address={contract_address}&limit={limit}&page={page}&address={address}
Method: GET
SuccessResponse:
{
    "total": 1179,
    "transactions": [
        {
            "tx_id": "c65d1206e65aaf2e7e08cc818c372ff2c2947cb6cbec746efe6a5e20b7adefa9",
            "params_to": "",
            "chain_id": "AELF",
            "block_height": 1064,
            "address_from": "grSAEQ5vJ7UfCN2s1v4fJJnk98bu4SHa2hpQkQ9HT88rmaZLz",
            "address_to": "xw6U3FRE5H8rU3z8vAgF9ivnWSkxULK5cibdZzMC9UWf7rPJf",
            "params": "",
            "method": "NextRound",
            "block_hash": "8c922b20164ad3774b56d19673154f383ed89656cbd56433d1681c8c3a4dcab9",
            "quantity": 0,
            "tx_status": "Mined",
            "time": "2019-04-26T07:18:36.636701Z"
        },
        {
            "tx_id": "4780a7b2737b6f044894719b9bb4cb09862c0b4a7cae267131a0b5c3e7c12850",
            "params_to": "",
            "chain_id": "AELF",
            "block_height": 1063,
            "address_from": "QUYYqzTQmugruHYmuJVftwmVjnUM82pXnMTnT5jh55qwZKrMw",
            "address_to": "xw6U3FRE5H8rU3z8vAgF9ivnWSkxULK5cibdZzMC9UWf7rPJf",
            "params": "",
            "method": "UpdateValue",
            "block_hash": "381114b86b09886f59956851a1d47d8442b29f44f3785dade3c667ca320e23bb",
            "quantity": 0,
            "tx_status": "Mined",
            "time": "2019-04-26T07:18:36.636701Z"
        },
        {
            "tx_id": "0230385e3f060059d2a62addac09ad6d01f96d32ec076cfbf44c6a3b70c6e092",
            "params_to": "",
            "chain_id": "AELF",
            "block_height": 1062,
            "address_from": "zizPhdDpQCZxMChMxn1oZ4ttJGJUo61Aocg5BpTYvzLQGmBjT",
            "address_to": "xw6U3FRE5H8rU3z8vAgF9ivnWSkxULK5cibdZzMC9UWf7rPJf",
            "params": "",
            "method": "NextRound",
            "block_hash": "06a3ceb783480f4cf5b8402f6749617093d9ea5f9a053f65e86554aeed6d98f4",
            "quantity": 0,
            "tx_status": "Mined",
            "time": "2019-04-26T07:18:28.635113Z"
        },
    ]
}
```

## TPS

### Get TPS Record

```js
URL: api/tps/list?start_time={unix_timestamp}&end_time={unix_timestamp}&order={order}
Method: GET
SuccessResponse:
{
    "total": 178,
    "tps": [
        {
            "id": 12498,
            "start": "2019-11-22T01:12:14Z",
            "end": "2019-11-22T01:13:14Z",
            "txs": 1878,
            "blocks": 120,
            "tps": 31,
            "tpm": 1878,
            "type": 1
        },
        {
            "id": 12499,
            "start": "2019-11-22T01:13:14Z",
            "end": "2019-11-22T01:14:14Z",
            "txs": 1889,
            "blocks": 117,
            "tps": 31,
            "tpm": 1889,
            "type": 1
        },
        {
            "id": 12500,
            "start": "2019-11-22T01:14:14Z",
            "end": "2019-11-22T01:15:14Z",
            "txs": 1819,
            "blocks": 114,
            "tps": 30,
            "tpm": 1819,
            "type": 1
        },
        {
            "id": 12501,
            "start": "2019-11-22T01:15:14Z",
            "end": "2019-11-22T01:16:14Z",
            "txs": 1779,
            "blocks": 105,
            "tps": 30,
            "tpm": 1779,
            "type": 1
        }
    ]
}
```
