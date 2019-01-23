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
        console.log('tx_hash : ' + hash);
        _repeatedCalls(function () {
            var txRes = aelf.chain.getTxResult(hash).result;
            if (txRes.tx_status !== 'Pending') {
                console.log('tx_status : ', txRes.tx_status);
            }
            if (txRes.tx_status === 'Mined') {
                var res = JSON.parse(txRes.return);
                console.log('Chain creation proposal created.');
                console.log('proposal_hash : ', res['proposal_hash'] );
                console.log('chain_id :', res['chain_id']);
            }
            return txRes.tx_status !== 'Pending';
        }, 3000);
    };

    request_chain_disposal = function (chain_id) {
        var hash = chain.crossChainContract.RequestChainDisposal(chain_id).hash;
        console.log('tx_hash : ', hash);
        _repeatedCalls(function () {
            var res = aelf.chain.getTxResult(hash).result;
            if (res.tx_status !== 'Pending') {
                console.log('tx_status : ', res.tx_status);
            }
            if (res.tx_status === 'Mined') {
                console.log('Chain disposal proposal created');
                console.log('proposal_hash :', res.return);
            }
            return res.tx_status !== 'Pending';
        }, 3000);
    };

    withdraw_chain_creation_request = function (chain_id) {
        var hash = chain.crossChainContract.WithdrawRequest(chain_id).hash;
        console.log('tx_hash : ', hash);
        _repeatedCalls(function () {
            var res = aelf.chain.getTxResult(hash).result;
            if (res.tx_status !== 'Pending') {
                console.log('tx_status : ', res.tx_status);
            }
            if (res.tx_status === 'Mined') {
                console.log('Chain creation request is already withdrawn now.');
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
        console.log('tx_hash : ' + hash);
        _repeatedCalls(function () {
            var res = aelf.chain.getTxResult(hash).result;
            if (res.tx_status !== 'Pending') {
                console.log('tx_status : ', res.tx_status);
            }
            if (res.tx_status === 'Mined') {
                if(res.return.toString() === '01') {
                    console.log('Verification succeed.');
                }
                else
                {
                    console.log('Verification failed.');
                }
            }
            return res.tx_status !== 'Pending';
        }, 3000);
    };

    recharge_sidechain = function (chain_id, amount){
        var hash = chain.crossChainContract.Recharge(chain_id, amount).hash;
        console.log('tx_hash : ' + hash);
        _repeatedCalls(function () {
            var res = aelf.chain.getTxResult(hash).result;
            if (res.tx_status !== 'Pending') {
                console.log('tx_status : ', res.tx_status);
            }
            if (res.tx_status === 'Failed') {
                console.log('Recharge failed. Unable to recharge chain ', chain_id, ".");
            }
            return res.tx_status !== 'Pending';
        }, 3000);
    }
})();