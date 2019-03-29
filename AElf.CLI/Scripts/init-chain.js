(function () {
    function getContractZero(cRes) {
        var addressZero = cRes.GenesisContractAddress;
        if (typeof addressZero === 'undefined') {
            addressZero = cRes['AElf.Contracts.Genesis'];
        }
        if(typeof addressZero === 'undefined'){
            throw "Cannot find contract zero's address.";
        }
        return aelf.chain.contractAt(addressZero, _account);
    }

    function getAuthorization(cRes) {
        var authorizationAddress = cRes['AElf.Contracts.Authorization'];
        return aelf.chain.contractAt(authorizationAddress, _account);
    }

    function getCrossChain(cRes) {
        var crossChainAddress = cRes['AElf.Contracts.CrossChain'];
        if(!crossChainAddress){
            return;
        }
        return aelf.chain.contractAt(crossChainAddress, _account);
    }

    if (aelf.isConnected()) {
        console.log("connect...");
        var cRes = aelf.chain.getChainInformation();
        chain = {
            contractZero: getContractZero(cRes),
            // authorizationContract: getAuthorization(cRes),
            // crossChainContract: getCrossChain(cRes)
        };
    }
})();