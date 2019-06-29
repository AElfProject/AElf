(function () {
    _requestor.isConnected = function () {
        try {
            this.send({
                method: 'GET',
                url: 'blockChain/chainStatus'
            });
            return true;
        } catch (e) {
            return false;
        }
    }
})();