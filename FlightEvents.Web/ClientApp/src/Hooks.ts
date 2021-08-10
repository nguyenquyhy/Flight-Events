import * as React from "react";

export function useStateOnce<T>(initialValue: T) {
    const [state, setState] = React.useState(initialValue);
    const [alreadySet, setAlreadySet] = React.useState(false);
    return [state, function (newValue: T) {
        if (!alreadySet) {
            setAlreadySet(true);
            setState(newValue);
            return true;
        }
        return false;
    }] as const;
}

export function useStateFromProps<T>(propsValue: T) {
    const [state, setState] = React.useState(propsValue);
    React.useEffect(() => {
        setState(propsValue);
    }, [propsValue]);
    return [state, setState] as const;
}