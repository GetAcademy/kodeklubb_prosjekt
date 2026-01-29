export interface User
{
    id: string;
    email: string;
    flags: number;
    banner: string;
    locale: string;
    avatar: string;
    username: string;
    verified: boolean;
    banner_color: string;
    accent_color: number;
    mfa_enabled: boolean;
    premium_type: number;
    public_flags: number;
    discriminator: string;
}

export interface AuthState
{
    user: User;
    isAuthenticated: boolean;
    loading: boolean;
}