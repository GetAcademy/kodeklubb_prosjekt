export function sanitizeUrlParams (params: string[]) : void
    {
        if (!params) return;

        let hasChanged: boolean = false;
        const url = new URL(window.location.href)

        params.forEach(param => { url.searchParams.delete(param); hasChanged = !hasChanged; });

        if (hasChanged){window.history.replaceState({}, '', url.toString());}
        /*if (code)
        {
            // Redirect to backend API
            const backendAPIBase = import.meta.env.VITE_BASE_API;
            window.location.href = `${backendAPIBase}/auth/discord/callback?code=${encodeURIComponent(code)}`;
        }*/
    }