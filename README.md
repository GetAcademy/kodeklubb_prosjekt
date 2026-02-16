# kodeklubb_prosjekt

## Verktøy for å kjøre prosjektet
- [Node.js](https://nodejs.org/en)
- [Docker](https://www.docker.com/)

## Sette opp .env

Du trenger to `.env` filer i dette prosjektet - en for frontend og en for backend. Eksempel på innhold i `.env` finner du i `.env.example`.
1) Naviger til frontend mappen:
```bash
cd frontend
```
2) Lag en `.env` fil med innholda fra `frontend/.env.example`.

## For backend:

Naviger til `root` og lag en `.env` fil med innholda fra `.env.example`.

For å få Discord-innlogging til å fungere, har du to valg:
1) Lag din egen OAuth2 applikasjon
    - Gå til https://discord.com/developers/home og logg inn med Discord hvis den spør
    - Gå til "Applications", deretter "New Application"
    - Gå til "OAuth2" i din applikasjon - her finner du ClientId og ClientSecret
    - Under "Redirects" må du ha:
    ```http://localhost:5154/auth/discord/callback```
2) Ta kontakt med en av oss for `ClientId` og `ClientSecret` :P

## Starte prosjekt:

### Starte backend

1) Naviger i terminal til root (`/kodeklubb_prosjekt`)
2) Skriv inn i terminal:
```bash
docker compose up --build
```

### Starte frontend

1) Naviger til frontend mappen:
```bash
cd frontend
```
2) Installer prosjekt:
```bash
npm install
```
3) Start frontend
```bash
npm run dev
```

## pgAdmin - legge til server

Logg inn med detaljer fra `.env` til pgAdmin dashboard. Legg til en server ("Add new server"):
1) Name - kan være hva som helst
2) Host, username og password må samsvare med `.env`

## Prosjekt oversikt

Du vil nå kunne se / administrere database via pgAdmin på http://localhost:8080 - email og passord settes i .env i root. 

Frontenden kjøres på http://localhost:3000 
