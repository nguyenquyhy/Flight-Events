const isObject = value => typeof value === 'object' && value !== null;

const compareObjects = (A, B) => {
    const keysA = Object.keys(A);
    const keysB = Object.keys(B);

    if (keysA.length !== keysB.length) {
        return false;
    }

    return !keysA.some(key => !B.hasOwnProperty(key) || A[key] !== B[key]);
};

const shallowEqual = (A, B) => {
    if (A === B) {
        return true;
    }

    if ([A, B].every(Number.isNaN)) {
        return true;
    }

    if (![A, B].every(isObject)) {
        return false;
    }

    return compareObjects(A, B);
};

export const deepEqual = (A, B) => {
    if (A === B) {
        return true;
    }

    if ([A, B].every(Number.isNaN)) {
        return true;
    }

    if ([A, B].every(isObject)) {
        const keysA = Object.keys(A);
        const keysB = Object.keys(B);

        if (keysA.length === keysB.length) {
            return keysA.every(prop => B.hasOwnProperty(prop) && deepEqual(A[prop], B[prop]));
        }
    }

    return false;
};

export const propsShallowEqual = (A, B) =>
    Object.keys(A).every(prop => shallowEqual(A[prop], B[prop]));