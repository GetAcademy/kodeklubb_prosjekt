```mermaid
graph TD
    %% Main-structure
    App --> Header
    App --> MainContent
    App --> Footer

    %% Header Section
    subgraph Header_Section [Header]
        Header --> Nav[Main.Navigation]
        Header --> Logo[GET-KodeKlubb-logo]
        Header --> Tittel[GET-KodeKlubb-title]
    end

    %% Main Content Forgrening
    MainContent --> Profile
    MainContent --> Dashboard
    MainContent --> Discover-Teams

    %% Dashboard Flyt
    subgraph Dashboard_Flyt [Dashboard]
        Dashboard --> Mine-teams
        Mine-teams --> API_Get[CALL-Backend-API-(GetUserTeams)]
        API_Get --> Arduino_Card[Arduino Team Kort]
        Arduino_Card --> Artikkel-Ingress
        Arduino_Card --> Link-til-side
    end

    %% Profil Flyt
    subgraph Profil_Flyt [User Profile]
        Profile --> Edit-Info[Edit Profile Information]
        Profile --> Discord-Section[Discord Integrasjon]

        %% Redigering
        Edit-Info --> Schema[Validate-Schema]
        Schema --> Fields[E-mail / County / City / Postnr]
        Fields --> POST[POST-Req-Backend]

        %% Discord
        Discord-Section --> Discord-userName
        Discord-Section --> Discord-Email
        Discord-userName --> Link-Discord[Link Discord Profile]
        Discord-Email --> Link-Email[Link E-mail-Service]
    end

    %% Discover Teams Flyt
    subgraph Discover_Flyt [Oppdag Teams]
        Discover-Teams --> View-Available[View Available Teams]
        View-Available --> Team-Arduino[Arduino Team]
        Team-Arduino --> Small-description
        Team-Arduino --> Request-to-join
    end
```