function _getArrayLength(array) {
    return array.length;
}

function _getOwnPropertyNames(obj) {
    return Object.getOwnPropertyNames(obj).join(",");
}

function _get(obj, propertyName) {
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
