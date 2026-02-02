export function sanitizeUrlParams (params: string[]) : void
    {
        if (!params) return;
        let hasChanged: boolean = false;
        const url = new URL(window.location.href)
        params.forEach(param => { url.searchParams.delete(param); hasChanged = true; });
        if (hasChanged){window.history.replaceState({}, '/', url.pathname.toString());}
    }