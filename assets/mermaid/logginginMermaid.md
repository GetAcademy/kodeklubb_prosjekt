```mermaid
graph TD
    A[Bruker logger inn] --> B(Discord OAuth2)
    B --> C[if auth good]
    C --> D[Yes -> go to '/'] & E
    E[No -> Send message of auth result]
    D --> F[Sjef]
```