(function () {
    create_multi_sig_account = function (decidedThreshold, proposerThreshold, reviewers) {
        console.log('Start creating multi sig address..');        
        var auth = {
            'ExecutionThreshold' : decidedThreshold,
            'ProposerThreshold' : proposerThreshold,
            'Reviewers' : JSON.parse(reviewers)
        };
        var txHash = chain.authorizationContract.CreateMultiSigAccount(auth).hash;
        
        console.log('tx_hash is: ' + txHash);
        _repeatedCalls(function () {
            var res = aelf.chain.getTxResult(txHash).result;
            if (res.tx_status !== 'Pending') {
                console.log('TxStatus is: ', res.tx_status);
            }
            if (res.tx_status === 'Mined') {
                console.log('Multi sig address is: ', res.return);
            }
            return res.tx_status !== 'Pending';
        }, 3000);
    };
    
    propose = function (proposalName, multiSigAccount, expiredTime, to, methodName, params, proposer) {
        var abi = aelf.chain.getContractAbi(to);
        var methodAbi = abi.Methods.find(x => x.Name === methodName);
        var abiStr = JSON.stringify(methodAbi, null, 2);
        var packedTxn = {
            'From': multiSigAccount,
            'To': to,
            'MethodName': methodName,
            'Params': JSON.parse(params),
            'MethodAbi' : methodAbi
        };
        //console.log('packedtxn - ', packedTxn);
        var time = new Date();
        time.setSeconds(new Date().getSeconds() + expiredTime);
        var proposal = {
            'MultiSigAccount' : multiSigAccount,
            'Name' : proposalName,
            'ExpiredTime' : time.toISOString(),
            'Proposer' : proposer,
            'TxnData' : packedTxn
        };
        
        var txHash = chain.authorizationContract.Propose(proposal).hash;
        console.log('tx_hash is: ' + txHash);
        _repeatedCalls(function () {
            var res = aelf.chain.getTxResult(txHash).result;
            if (res.tx_status !== 'Pending') {
                console.log('TxStatus is: ', res.tx_status);
            }
            if (res.tx_status === 'Mined') {
                console.log('Proposal hash is ', res.return);
            }
            return res.tx_status !== 'Pending';
        }, 3000);
    };
    
    approve = function (proposalHash) {
        var res = aelf.chain.checkProposal(proposalHash);
        if(!res['result'])
        {
            console.log('Proposal not found.');
            return;
        }
        var txndata = res['result']['TxnData'];
        var sig = Aelf.wallet.sign(txndata, _account.keyPair);

        var approval ={
            'ProposalHash' : proposalHash,
            'Signature' : sig
        };
        var txHash = chain.authorizationContract.SayYes(approval).hash;
        console.log('tx_hash is: ' + txHash);
        _repeatedCalls(function () {
            var res = aelf.chain.getTxResult(txHash).result;
            if (res.tx_status !== 'Pending') {
                console.log('TxStatus is: ', res.tx_status);
            }
            if (res.tx_status === 'Mined') {
                console.log('Success.');
            }
            return res.tx_status !== 'Pending';
        }, 3000);
    };
    
    release = function (proposalHash) {
        var txHash = chain.authorizationContract.Release(proposalHash).hash;
        console.log('tx_hash is: ' + txHash);
        _repeatedCalls(function () {
            var res = aelf.chain.getTxResult(txHash).result;
            if (res.tx_status !== 'Pending') {
                console.log('TxStatus is: ', res.tx_status);
            }
            if (res.tx_status === 'Mined') {
                console.log('Released tx hash is ', res.return);
            }
            return res.tx_status !== 'Pending';
        }, 3000);
    };
})();