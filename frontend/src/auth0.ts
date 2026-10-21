import { createAuth0 } from '@auth0/auth0-vue';

// These three values are public (they are not secrets). Same as in the Auth0 dashboard.
// Domain = our Auth0 tenant. ClientId = the "Kodeklubb" application. Audience = the Kodeklubb API.
export const auth0Config = {
    domain: import.meta.env.VITE_AUTH0_DOMAIN ?? 'getacademy.eu.auth0.com',
    clientId: import.meta.env.VITE_AUTH0_CLIENT_ID ?? 'iqOqy0VVfN1ATMBFbb3V1fVYvOkAdvaw',
    audience: import.meta.env.VITE_AUTH0_AUDIENCE ?? 'https://kodeklubb-api',
};

// The Auth0 Vue SDK does the login (Authorization Code flow with PKCE), the token
// exchange and the token cache. We only configure it.
export const auth0 = createAuth0({
    domain: auth0Config.domain,
    clientId: auth0Config.clientId,
    // Keep the login in the browser, so a page reload does not log you out
    cacheLocation: 'localstorage',
    authorizationParams: {
        redirect_uri: window.location.origin,
        audience: auth0Config.audience,
    },
});
