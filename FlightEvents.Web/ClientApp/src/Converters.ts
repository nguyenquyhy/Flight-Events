var pascalCasePattern = new RegExp("^([A-Z])([a-z]+)");

export function pascalCaseToCamelCase(propname) {
    if (pascalCasePattern.test(propname)) {
        return propname.charAt(0).toLowerCase() + propname.slice(1);
    }
    else {
        return propname;
    }
}

export function convertPropertyNames(obj, converterFn) {
    var r, value, t = Object.prototype.toString.apply(obj);
    if (t === "[object Object]") {
        r = {};
        for (var propname in obj) {
            value = obj[propname];
            r[converterFn(propname)] = convertPropertyNames(value, converterFn);
        }
        return r;
    }
    else if (t === "[object Array]") {
        r = [];
        for (var i = 0, L = obj.length; i < L; ++i) {
            value = obj[i];
            r[i] = convertPropertyNames(value, converterFn);
        }
        return r;
    }
    return obj;
}