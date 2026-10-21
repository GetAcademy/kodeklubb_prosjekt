-- Auth0 identity ("sub" in the token). Our own users.id stays the main key.
ALTER TABLE users
    ADD COLUMN IF NOT EXISTS auth0_user_id VARCHAR(100);

CREATE UNIQUE INDEX IF NOT EXISTS idx_users_auth0_user_id
    ON users(auth0_user_id)
    WHERE auth0_user_id IS NOT NULL;
