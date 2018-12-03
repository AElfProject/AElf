function _getArrayLength(array) {
    return array.length;
}

function _getOwnPropertyNames(obj) {
    function onlyUnique(value, index, self) {
        return self.indexOf(value) === index;
    }

    var props = Object.getOwnPropertyNames(obj);
    if (obj.constructor !== undefined && obj.constructor.prototype !== undefined) {
        props = props.concat(Object.getOwnPropertyNames(obj.constructor.prototype));
    }

    return props.filter(onlyUnique).join(",");
}

function _getOwnProperty(obj, propertyName) {
    return obj[propertyName];
}

function _toString(v) {
    if (typeof v == 'object')
        return JSON.stringify(v);
    return v;
}

function _fromString(v) {
    return JSON.parse(v);
}

function _assignToUnderscore(obj) {
    _ = obj;
}
