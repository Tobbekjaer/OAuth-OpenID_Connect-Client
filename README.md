# OpenID Connect Client (Keycloak)

En simpel ASP.NET MVC applikation, der implementerer OAuth 2.0 + OpenID Connect med Keycloak som Identity Provider.

Formålet er at forstå SSO-flowet og de vigtigste sikkerhedsprincipper.

---

## Formål

Applikationen viser, hvordan en bruger logger ind via Keycloak i stedet for lokal authentication.

- MVC-app = client  
- Keycloak = authorization server + identity provider  
- Browser = user agent  

---

## Flow (kort forklaret)

- Bruger klikker login  
- Browser redirectes til Keycloak (front-channel)  
- Brugeren logger ind  
- Browser sendes tilbage med en authorization code  
- Serveren udveksler code til tokens (back-channel)  
- ID token valideres  
- Brugeren logges ind via lokal cookie  

---

## Implementering (trin)

1. MVC grundstruktur  
2. Konfiguration i appsettings.json  
3. `/login` redirect til Keycloak  
4. `/auth/callback` modtager code + state  
5. Exchange code → tokens  
6. Valider ID token + hent userinfo  

---

## Centrale begreber

### Front-channel vs Back-channel
- Front-channel: browser redirects (login flow)  
- Back-channel: server → Keycloak (token exchange)  

### ID Token vs UserInfo
- ID token: beviser brugerens identitet  
- UserInfo: henter ekstra brugerdata  

### PKCE
Beskytter authorization code flow mod interception af koden.

### Lokal cookie-session
Efter login opretter appen sin egen session:

- Keycloak håndterer authentication  
- MVC-appen håndterer sin egen login-session  

---

## Vigtig pointe

Der findes to sessions:

- Keycloak session (Identity Provider)  
- Lokal cookie-session (din app)  

---

## Resultat

- Brugeren logger ind via Keycloak  
- Appen validerer ID token korrekt  
- Appen opretter en sikker lokal session  
- Claims kan ses via `/me`  

---

## Kort opsummering

ID token beviser identitet, userinfo giver data, PKCE beskytter flowet, og appen opretter sin egen session efter login.
