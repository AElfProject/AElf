(function () {
    request_chain_creation = function (lockedToken, indexingPrice, resource, account, code) {
        //console.log(resource);
        var pair = JSON.parse(resource);
        console.log('pair - ', pair);
        var sideChainInfo = {
            'IndexingPrice' : indexingPrice,
            'LockedTokenAmount' : lockedToken,
            'ResourceBalances' : pair,
            'ContractCode' : code,
            'Proposer' : account
        };
        //console.log('sidechainInfo - ', sideChainInfo);
        var txHash = chain.crossChainContract.ReuqestChainCreation(sideChainInfo).hash;
        console.log('tx_hash is: ' + txHash);
        _repeatedCalls(function () {
            var res = aelf.chain.getTxResult(txHash).result;
            if (res.tx_status !== 'Pending') {
                console.log('TxStatus is: ', res.tx_status);
            }
            if (res.tx_status === 'Mined') {
                console.log('Chain creation proposal created, proposal hash is ', res.return);
            }
            return res.tx_status !== 'Pending';
        }, 3000);
    }
})();