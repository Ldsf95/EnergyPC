# EnergyPC — Dashboard énergétique de l'ordinateur

Application client/serveur **100 % C# / .NET 8** qui mesure en temps réel la
consommation énergétique du PC (CPU, GPU, RAM, batterie, puissance totale
estimée), historise les relevés et détecte les pics par seuil configurable.

> Projet pédagogique — Bachelor 2 (ESIEE-IT), 2025–2026.
> Stack : WinForms · ASP.NET Core Web API · Entity Framework Core · SQLite ·
> LibreHardwareMonitorLib.

---

## Sommaire

- [Architecture](#architecture)
- [Prérequis](#prérequis)
- [Démarrage rapide](#démarrage-rapide)
- [Configuration](#configuration)
- [API REST](#api-rest)
- [Tests](#tests)
- [Déploiement local](#déploiement-local)
- [Numérique Responsable](#numérique-responsable)
- [Structure du dépôt](#structure-du-dépôt)
- [Licence](#licence)

---

## Architecture

L'application est découpée en **quatre projets** (couches Présentation / API /
Données) plus un projet de tests, déployés sur la même machine mais isolés.

```
            +-------------------+        HTTP/JSON local        +-------------------+
            |  EnergyPC.WinForms| <---------------------------> |   EnergyPC.Api    |
            |  (UI, privilèges  |   GET /api/readings/latest    | (Web API + tâche  |
            |   standards)      |   GET /api/readings/energie   |  de fond)         |
            +-------------------+                               +---------+---------+
                                                                          |
                                            +-----------------------------+------------------+
                                            |                                                |
                                  +---------v---------+                          +-----------v-----------+
                                  | EnergyPC.Collector|                          |    EnergyPC.Data      |
                                  | (LibreHardware-   |                          | (EF Core, DbContext,  |
                                  |  Monitor)         |                          |  migrations, SQLite)  |
                                  +-------------------+                          +-----------+-----------+
                                                                                             |
                                                                                       energy.db (SQLite)
```

| Composant | Projet | Responsabilité |
|-----------|--------|----------------|
| Collecteur | `EnergyPC.Collector` | Lecture cyclique des capteurs via LibreHardwareMonitor. |
| API REST | `EnergyPC.Api` | Exposition HTTP/JSON, accès BDD, agrégations, collecte en tâche de fond. |
| Données | `EnergyPC.Data` | Entités EF Core (`SensorType`, `Reading`), `DbContext`, migrations, seed. |
| UI Desktop | `EnergyPC.WinForms` | Dashboard temps réel (cartes KPI, graphique défilant, compteurs Wh). |
| Tests | `EnergyPC.Tests` | Tests unitaires xUnit (contrôleurs, seed, intégration trapèze). |

**Flux** : toutes les `IntervalSeconds` secondes, le collecteur produit une rafale
de mesures que l'API persiste en base. Le client WinForms interroge l'API à 1 Hz
pour rafraîchir les jauges et faire défiler le graphique.

> **Modèle de données générique** — Plutôt qu'une table par grandeur, deux tables :
> `SensorType` (ce qu'on mesure) et `Reading` (chaque valeur horodatée). Ajouter une
> grandeur ne modifie pas le schéma.

---

## Prérequis

- **Windows 10/11** (contrainte du projet : WinForms + capteurs matériels).
- **.NET 8 SDK** (`dotnet --version` → `8.0.x`).
- **Visual Studio 2022** (charges « Développement .NET Desktop » et « ASP.NET et
  développement web »). *Visual Studio est requis, pas VS Code.*
- Outil EF Core : `dotnet tool install --global dotnet-ef`.
- Optionnel : *DB Browser for SQLite* pour inspecter `energy.db`.

> ⚠️ La lecture de certains capteurs (puissance/température via MSR, SuperI/O)
> nécessite que **l'API soit lancée en administrateur**. L'interface WinForms, elle,
> tourne en privilèges standards (elle ne fait que consommer l'API HTTP locale).

---

## Démarrage rapide

```bash
# 1. Cloner
git clone https://github.com/<votre-compte>/EnergyPC.git
cd EnergyPC

# 2. (Une seule fois) faire confiance au certificat HTTPS de dev
dotnet dev-certs https --trust

# 3. Restaurer + compiler
dotnet build
```

### Lancer l'API (en administrateur)

```bash
cd EnergyPC.Api
dotnet run
```

La base `energy.db` est créée automatiquement au premier lancement
(`Database.Migrate()` + seed des types de capteurs). Swagger est disponible sur :

```
https://localhost:5001/swagger
```

### Lancer le client WinForms

Dans Visual Studio : définir `EnergyPC.Api` comme **projet de démarrage n°1**, puis
`EnergyPC.WinForms`. En ligne de commande, une fois l'API démarrée :

```bash
cd EnergyPC.WinForms
dotnet run
```

Le dashboard se rafraîchit chaque seconde. Si l'API est arrêtée, l'UI reste
utilisable et affiche l'erreur sans geler.

---

## Configuration

### API — `EnergyPC.Api/appsettings.json`

| Clé | Rôle | Défaut |
|-----|------|--------|
| `ConnectionStrings:Default` | Fichier SQLite | `Data Source=energy.db` |
| `Collector:IntervalSeconds` | Période de collecte | `2` |
| `Jwt:Key` | Clé de signature JWT (**à changer**) | clé de démo |
| `Auth:Utilisateur` / `Auth:MotDePasse` | Identifiants `/api/auth/login` | `admin` / `energy` |

### Client — `EnergyPC.WinForms/appsettings.json`

| Clé | Rôle | Défaut |
|-----|------|--------|
| `Api:BaseUrl` | URL de l'API | `https://localhost:5001` |
| `Alerte:SeuilSystemPowerW` | Seuil de détection de pic (W) — la carte « Total estimé » passe en rouge au-delà | `60` |
| `Auth:*` | Identifiants pour récupérer un token JWT (optionnel) | `admin` / `energy` |

---

## API REST

| Méthode | Endpoint | Description |
|---------|----------|-------------|
| `GET` | `/api/sensortypes` | Liste des grandeurs mesurées. |
| `GET` | `/api/readings/latest` | Dernière valeur de chaque capteur (snapshot). |
| `GET` | `/api/readings/history?code=CPU_POWER&minutes=5` | Historique brut sur une fenêtre. |
| `GET` | `/api/readings/history-minute?code=CPU_POWER&minutes=1440` | Historique « downsamplé » à la minute (24 h). |
| `GET` | `/api/readings/aggregate?code=CPU_POWER&bucket=minute\|heure\|jour&minutes=60` | Agrégation (moyenne) par minute, heure ou jour. |
| `GET` | `/api/readings/energie?code=SYSTEM_POWER&minutes=60` | Énergie cumulée (Wh) — intégration par trapèzes. |
| `DELETE` | `/api/maintenance/purge?jours=7` | Purge des relevés de plus de N jours. |
| `POST` | `/api/auth/login` | Échange identifiants → token JWT. |

> Le calcul de l'énergie (Wh) intègre la puissance (W) par la **méthode des
> trapèzes** : `Wh += moyenne(P[i], P[i-1]) × Δt(h)`.

---

## Tests

```bash
dotnet test
```

Couverture (xUnit + EF Core InMemory) :

- `Latest_RetourneTousLesSensorTypes` — l'endpoint snapshot renvoie tous les capteurs.
- `Energie_IntegrationTrapeze_Correct` — 60 min à 50 W ⇒ 50 Wh (± 0,5).
- `SensorTypes_Get_RetourneLes12Defauts` — les 12 types par défaut sont exposés.
- `Initialize_EstIdempotent` — le seed ne duplique pas les types.

---

## Déploiement local

Publier l'API (self-contained, exécutable unique) :

```bash
dotnet publish EnergyPC.Api -c Release -o publish/api \
  -r win-x64 --self-contained true -p:PublishSingleFile=true
```

Publier le client :

```bash
dotnet publish EnergyPC.WinForms -c Release -o publish/desktop \
  -r win-x64 --self-contained true -p:PublishSingleFile=true
```

Démarrage automatique de l'API à l'ouverture de session (PowerShell admin) :

```powershell
schtasks /Create /TN EnergyPCApi `
  /TR "C:\Apps\EnergyPC\publish\api\EnergyPC.Api.exe" `
  /SC ONLOGON /RL HIGHEST /F
```

---

## Numérique Responsable

- Sobriété : intervalle de collecte et fréquence d'affichage paramétrables.
- Rotation des logs Serilog (7 jours de rétention).
- Purge périodique des relevés (`/api/maintenance/purge`) pour borner la taille de
  la base.
- Code commenté, dette technique maîtrisée.

---

## Structure du dépôt

```
EnergyPC/
├─ EnergyPC.sln
├─ global.json
├─ .github/workflows/ci.yml      # CI : build + tests (windows-latest)
├─ EnergyPC.Data/                # Entités, DbContext, migrations, seed
│  ├─ Entities/{SensorType,Reading}.cs
│  ├─ EnergyDbContext.cs
│  ├─ SeedData.cs
│  └─ Migrations/
├─ EnergyPC.Collector/           # IHardwareCollector + HardwareCollector
├─ EnergyPC.Api/                 # Program.cs, Controllers/, Services/, Dtos.cs
├─ EnergyPC.WinForms/            # Program.cs, Forms/, Services/, Models/, Controls/, Helpers/
└─ EnergyPC.Tests/               # Tests xUnit
```

---

## Licence

Distribué sous licence MIT — voir [`LICENSE`](LICENSE).
