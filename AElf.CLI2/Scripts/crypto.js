// This file will be inject by C#
// function _randomNextInt() {
//     return 0
// }

(function () {
    function Hmac(algo, seed) {
        this.algo = algo;
        this.seed = seed;
        this.data = '';
    }

    Hmac.prototype.update = function (data) {
        this.data = this.data.concat(data);
        return this;
    };

    Hmac.prototype.digest = function (format) {
        return __getHmacDigest__(this.algo, this.seed, this.data, format);
    };

    function createHmac(algo, seed) {
        return new Hmac(algo, seed);
    }

    function getRandomValues(array) {
        if (array.constructor === Uint8Array) {
            for (var i = 0; i < array.length; ++i) {
                array[i] = __randomNextInt__() % (1 << 8);
            }
        } else {
            throw "not implemented";
        }
    }

    crypto = {
        createHmac: createHmac,
        getRandomValues: getRandomValues
    };
    if (typeof global === 'undefined') {
        global = {};
    }
    global.crypto = crypto;
    global.Uint8Array = Uint8Array;
})();
