# KanbanApp — CLAUDE.md

## What is KanbanApp

KanbanApp is a collaborative project management tool with Kanban boards. Users
can create projects, add boards with columns and cards, invite team members,
assign cards, set deadlines, drag-and-drop cards between columns, and receive
in-app notifications when cards are assigned to them.

## Repository & Hosting

- GitHub: https://github.com/Shellty-IT/KanbanApp (confirm URL)
- Local: `C:\Users\Tomek\Desktop\Projekty\NerdsFamily\KanbanApp_Project`
- Backend: Azure App Service — https://shellty-kanban-api.azurewebsites.net
- Frontend: Azure App Service — https://shellty-kanban.azurewebsites.net

## Stack

**Backend:** ASP.NET Core 10 Minimal API / C# / EF Core / ASP.NET Identity

**Database:** SQLite (local dev) → Azure SQL Serverless (production)

**Frontend:** React 18 + Vite / JavaScript / axios / react-router-dom /
@hello-pangea/dnd

**Auth:** JWT (access token 15 min) + Refresh token (7 days, rotate on use, DB-stored)

**CI/CD:** GitHub Actions → Azure App Service (single `deploy.yml` on push to `main`)

**Tests backend:** xUnit + EF InMemory + coverlet — 60 tests

**Tests frontend:** Vitest — 45 helper tests

## Architecture

```
KanbanApp_Project/
  KanbanApp.Backend/
    Controllers/          # Minimal API endpoints (Auth, Project, Board, Column, Card, User, Notification)
    DTOs/                 # Create/Update/Detail objects
    Models/               # Project, Board, Column, Card, BoardMember, ApplicationUser, RefreshToken, Notification
    Services/             # BoardService, CardService, UserService, ...
    Extensions/           # DatabaseExtensions, AuthExtensions, AppServicesExtensions, SwaggerExtensions
    Program.cs            # Bootstrapped via Extension methods
    Migrations/           # EF Core — always generate for SQL Server (--environment Production)
  KanbanApp.Frontend/
    src/
      context/            # TopbarContext.jsx
      views/              # Dashboard, ProjectView, BoardView, LoginPage, RegisterPage, ProfilePage
      components/         # Card, Column, ColumnForm, InviteModal, NotificationBell, ProtectedRoute
      hooks/              # useBoardData, useColumns, useCards, useCardSearch, useDragDrop, useBoardTopbar.jsx
      services/api.js     # axios + interceptor (auto-refresh on 401)
      App.jsx, main.jsx
  KanbanApp.Tests/        # xUnit, EF InMemory, coverlet
    TestBase.cs
    KanbanWebAppFactory.cs
    GlobalUsings.cs
    *Tests.cs
  .github/workflows/
    deploy.yml            # push to main → build + test + deploy to Azure
  Dockerfile
```

## API Endpoints

### Auth (public)

| Method | Path | Notes |
|--------|------|-------|
| POST | `/register` | Rate limit 5/min |
| POST | `/login` | Returns `accessToken` + `refreshToken`, rate limit 5/min |
| POST | `/api/auth/refresh` | Rotates refresh token |
| POST | `/api/auth/logout` | Revokes refresh token (JWT required) |

### Resources (JWT required)

| Method | Path | Notes |
|--------|------|-------|
| GET/POST/PUT/DELETE | `/api/projects` | |
| GET/POST/PUT/DELETE | `/api/boards` | |
| GET/POST/PUT/DELETE | `/api/boards/{id}/columns` | |
| GET/POST/PUT/DELETE | `/api/boards/{id}/cards` | |
| GET | `/api/boards/{id}/cards/search?q=` | Search by title/description |
| PUT | `/api/boards/{id}/cards/{id}/assign` | Creates notification |
| GET/POST | `/api/boards/{id}/members` | |
| GET/PUT/POST | `/api/users/me` | GET profile, PUT update, POST avatar |
| GET | `/api/notifications` | |
| PUT | `/api/notifications/{id}/read` | |
| PUT | `/api/notifications/read-all` | |

## Models

```csharp
ApplicationUser : IdentityUser { ProfilePictureUrl, Bio, BoardMembers }

RefreshToken { Id, Token, UserId, ExpiresAt, IsRevoked }

BoardMember { UserId, BoardId, Role (Member|Owner), JoinedAt }

Card { Id, Title, Description, Position, CreatedAt, ColumnId,
       AssignedToUserId, DueDate, Color }

Notification { Id, UserId, Message, IsRead, CreatedAt, CardId }
```

## Auth & Security

- **Access token:** 15 minutes (HS256)
- **Refresh token:** 7 days, one-time use (rotate on use), stored in DB
- **Auto-refresh:** `api.js` interceptor retries on 401 via `/api/auth/refresh`
- **Rate limiting:** 5 req/min on `/register` and `/login` → HTTP 429
- **Rate limiter disabled in tests:** `RateLimiting:Disabled=true`

## Frontend Structure

```
src/
  context/
    TopbarContext.jsx       # Global topbar state: title, actions, notifications
  views/
    LoginPage.jsx, RegisterPage.jsx, Dashboard.jsx
    ProjectView.jsx, BoardView.jsx, ProfilePage.jsx
  components/
    Card.jsx, Column.jsx, ColumnForm.jsx
    InviteModal.jsx, NotificationBell.jsx, ProtectedRoute.jsx
  hooks/
    useBoardData.js, useColumns.js, useCards.js
    useCardSearch.js, useDragDrop.js
    useBoardTopbar.jsx          ← MUST be .jsx (contains JSX — never rename to .js)
  services/
    api.js                  # axios, baseURL from VITE_API_URL, auto-refresh interceptor
```

### Routing

| Path | Component | Protected |
|------|-----------|-----------|
| `/` | LoginPage | No |
| `/login` | LoginPage | No |
| `/register` | RegisterPage | No |
| `/dashboard` | Dashboard | Yes |
| `/projects/:id` | ProjectView | Yes |
| `/board/:id` | BoardView | Yes |
| `/profile` | ProfilePage | Yes |

### API base URL

```js
const BASE_URL = import.meta.env.PROD
    ? 'https://shellty-kanban-api.azurewebsites.net'
    : 'http://localhost:5067';
```

### UX

- `TopbarContext` — global topbar (logo, title, left/right action buttons)
- Sidebar — fixed left, avatar, logout, navigation
- `NotificationBell` — topbar dropdown, unread badge
- Card search — input in BoardView topbar, Enter = search, result banner
- Member filter — select in BoardView topbar
- Drag-and-drop columns and cards — `@hello-pangea/dnd`
- 12-colour card palette (`COLORS` const)
- Deadline badge — green / red
- Password validation — live feedback
- Modals — custom `ModalOverlay` + `ModalBox`

## Configuration

### Backend (`appsettings.json` + Azure App Settings)

```json
{
  "ConnectionStrings": {
    "DefaultConnection": "..."    // SQLite locally, Azure SQL in production
  },
  "Jwt": {
    "Key": "6SjJOac2zFqvxTfE8FUsxziVjhfWRy7sxy4G4432Jo8=",
    "Issuer": "https://shellty-kanban-api.azurewebsites.net",
    "Audience": "https://shellty-kanban-api.azurewebsites.net"
  },
  "RateLimiting": {
    "Disabled": false             // set true in test environment
  }
}
```

Local dev: `appsettings.Development.json` (gitignored).

### Frontend

```
VITE_API_URL=http://localhost:5067
```

## Azure Infrastructure

| Resource | Name |
|----------|------|
| Resource Group | `my-apps-rg` |
| App Service Plan | `shellty-pulse-plan` (B1) |
| App Service Backend | `shellty-kanban-api` |
| App Service Frontend | `shellty-kanban` |
| Azure SQL Server | `shellty-sql-server` |
| Azure SQL DB | `shellty-kanban-db` (serverless) |

**Frontend startup command (App Service):**
```
pm2 serve /home/site/wwwroot --no-daemon --spa
```

**CORS (backend):**
```
https://shellty-kanban.azurewebsites.net
http://localhost:5173
```

## CI/CD

Push to `main` triggers `.github/workflows/deploy.yml`:
1. `dotnet restore` → `dotnet build` → `dotnet test`
2. `dotnet publish` (linux-x64, self-contained) → deploy to `shellty-kanban-api`
3. `npm ci` + `npm run build` in `KanbanApp.Frontend/` → deploy to `shellty-kanban`

**GitHub Secrets:**

| Secret | Purpose |
|--------|---------|
| `AZURE_CREDENTIALS` | Service principal JSON (`az ad sp create-for-rbac`) |
| `AZURE_WEBAPP_PUBLISH_PROFILE` | Backend publish profile (backup) |

## Tests

### Backend (xUnit)

- `KanbanWebAppFactory` — `WebApplicationFactory` with clean InMemory DB (Guid), rate limiter disabled
- `TestBase` — helpers: `CreateAuthenticatedClientAsync`, `CreateBoardAsync`, `CreateColumnAsync`, `CreateCardAsync`, `CreateProjectAsync`
- Login in tests uses `/login`, deserialises `accessToken` from response

**Coverage (60 tests, all passing):**

| Suite | Coverage |
|-------|---------|
| ProjectEndpoints | 98.2% |
| CardEndpoints | 100% |
| ColumnEndpoints | 100% |
| AuthEndpoints | 96.8% |
| UserEndpoints | 54.5% (no avatar upload test) |

### Frontend (Vitest) — 45 helper tests

## EF Core Migrations

**Always generate against SQL Server, never SQLite:**
```
dotnet ef migrations add <Name> --project KanbanApp.Backend -- --environment Production
```

Never modify existing migrations.

**Applied migrations:**
- `20260407232306_InitialCreate` — main schema (SQL Server)
- `20260407201959_AddRefreshTokens`
- `20260408_AddNotifications`

## Known Pitfalls (do not repeat)

- SQLite migrations do not work on Azure SQL — always generate under SQL Server
- `useBoardTopbar` must be `.jsx` — contains JSX, renaming to `.js` breaks Vite
- Static Web Apps replaced by App Service — do not revert to SWA
- Deploy via `AZURE_CREDENTIALS` (service principal) — not publish profile
- Frontend requires startup command: `pm2 serve /home/site/wwwroot --no-daemon --spa`
- Use `src/views/` not `src/pages/` for page-level components

## Project Status

**Done (in main):**
- Auth: register, login, JWT + refresh token rotation, logout, rate limiting
- Projects, boards, columns, cards CRUD
- Card assign with notification
- Board members (invite, list)
- User profile + avatar upload
- Notifications (list, mark read, mark all read)
- Card search (title/description)
- Drag-and-drop (columns + cards)
- Deadline badge, 12-colour palette
- CI/CD: GitHub Actions → Azure
- 60 xUnit tests, 45 Vitest tests

## Pending

<!-- Fill in manually -->

**Recommendations (not yet decided):**
- Add avatar upload test → UserEndpoints coverage from 54% to 90%+
- Add Serilog + Application Insights
- Smoke test after deploy in CI
- Consider SignalR for real-time notifications
- Verify CORS works correctly in production
- Full flow test: register → login → create board (production)

## Git & Commit Rules

- Agent does **not** commit, push, create PRs, or merge without an explicit instruction.
- After finishing work the agent always asks: "Commit and push?"
- Commit messages: conventional commits, short, plain English.
  Examples: `feat: add card colour picker`, `fix: refresh token rotation on 401`
- PR comments: plain English, dash-prefixed, lowercase.
  Example: `- fix null check on card assignment`
- No `Co-Authored-By`, no AI mentions, no emoji in commits or branch names.
- Branch names: short, lowercase, English, hyphens.

## Coding Rules

- No dead code, no comments in code.
- Use `src/views/` (not `src/pages/`) for page-level React components.
- Always deliver frontend and backend changes together — never omit the frontend.
- `useBoardTopbar` file extension must be `.jsx`, not `.js`.
- New tables require a new EF Core migration. Never modify existing migrations.
- Migrations always generated against SQL Server (`--environment Production`).
- No `Console.Write` — use `ILogger<T>` (backend) or remove logging entirely.
- No placeholder logos — user will add later.
