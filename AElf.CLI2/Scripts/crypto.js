// This file will be inject by C#
// function _randomNextInt() {
//     return 0
// }

function Hmac(algo, seed) {
    this.algo = algo;
    this.seed = seed;
    this.data = '';
}

Hmac.prototype.update = function (data) {
    this.data = this.data.concat(data);
    return this;
}

Hmac.prototype.digest = function (format) {
    return _getHmacDigest(this.algo, this.seed, this.data, format);
}

crypto = {
    getRandomValues: function (array) {
        if (array.constructor === Uint8Array) {
            for (var i = 0; i < array.length; ++i) {
                array[i] = _randomNextInt() % (1 << 8);
            }
        } else {
            throw "not implemented";
        }
    },
    createHmac: function (algo, seed) {
        return new Hmac(algo, seed);
    }
};

global = {
    crypto: crypto,
    Uint8Array: Uint8Array
};