(function () {
    request_chain_creation = function (lockedToken, indexingPrice, resource, account, code) {
        var pair = JSON.parse(resource);
        var sideChainInfo = {
            'IndexingPrice' : indexingPrice,
            'LockedTokenAmount' : lockedToken,
            'ResourceBalances' : pair,
            'ContractCode' : code,
            'Proposer' : account
        };
        var hash = chain.crossChainContract.ReuqestChainCreation(sideChainInfo).hash;
        console.log('tx_hash is: ' + hash);
        _repeatedCalls(function () {
            var res = aelf.chain.getTxResult(hash).result;
            if (res.tx_status !== 'Pending') {
                console.log('TxStatus is: ', res.tx_status);
            }
            if (res.tx_status === 'Mined') {
                console.log('Chain creation proposal created, proposal hash is ', res.return);
            }
            return res.tx_status !== 'Pending';
        }, 3000);
    };

    request_chain_disposal = function (chain_id) {
        var hash = chain.crossChainContract.RequestChainDisposal(chain_id).hash;
        console.log('tx_hash is: ', hash);
        _repeatedCalls(function () {
            var res = aelf.chain.getTxResult(hash).result;
            if (res.tx_status !== 'Pending') {
                console.log('TxStatus is: ', res.tx_status);
            }
            if (res.tx_status === 'Mined') {
                console.log('Chain disposal proposal created, proposal hash is ', res.return);
            }
            return res.tx_status !== 'Pending';
        }, 3000);
    };

    check_sidechain_status = function (chain_id) {
        var res =  chain.crossChainContract.GetChainStatus(chain_id);
        var resStr = JSON.stringify(res, null, 2);
        
        if(resStr['error']){
            console.log('Unable to check this side chain.');
            return;
        }
        console.log(resStr);
    };
    
    verify_crosschain_transaction = function (txid, merklepath, parent_height){
        var hash = chain.crossChainContract.VerifyTransaction(txid, merklepath, parent_height).hash;
        console.log('tx_hash is: ' + hash);
        _repeatedCalls(function () {
            var res = aelf.chain.getTxResult(hash).result;
            if (res.tx_status !== 'Pending') {
                console.log('TxStatus is: ', res.tx_status);
            }
            if (res.tx_status === 'Mined') {
                if(res.return.toString() === '01') {
                    console.log('Verification result: ', 'success');
                }
                else
                {
                    console.log('Verification result: ', 'fail');
                }
            }
            return res.tx_status !== 'Pending';
        }, 3000);
    };
})();