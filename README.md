# Shellty.Kanban - Project Manager

<div align="center">

![.NET](https://img.shields.io/badge/.NET_10-512BD4?style=flat-square&logo=dotnet&logoColor=white)
![EF Core](https://img.shields.io/badge/EF_Core_10-512BD4?style=flat-square&logo=dotnet&logoColor=white)
![Netlify](https://img.shields.io/badge/Netlify-00C7B7?style=flat-square&logo=netlify&logoColor=white)
![Render](https://img.shields.io/badge/Render-46E3B7?style=flat-square&logo=render&logoColor=white)
![Neon](https://img.shields.io/badge/Neon-00E599?style=flat-square&logo=neon&logoColor=black)
![JWT](https://img.shields.io/badge/JWT-black?style=flat-square&logo=jsonwebtokens&logoColor=white)
![xUnit](https://img.shields.io/badge/xUnit-60_tests_passed-success?style=flat-square)
![CI/CD](https://img.shields.io/badge/GitHub_Actions-2088FF?style=flat-square&logo=githubactions&logoColor=white)

A RESTful backend API for a team Kanban task management application.
Built with ASP.NET Core 10 Minimal API, deployed to Render and Netlify.

[👤 LinkedIn](https://www.linkedin.com/in/tomasz-skorupski-shellty)

</div>

---

## Screenshots

| Dashboard | Swagger UI |
|---|---|
| ![Dashboard](.github/screenshots/dashboard.png) | ![Swagger](.github/screenshots/swagger.png) |

---

## About

KanbanApp is a full-stack Kanban board application built as a portfolio project during the Engineering Academy with Nerds Family.

The backend exposes a complete REST API for managing teams, projects, boards, columns and cards - with JWT authentication, role-based board access, card search, notifications and file uploads.

**Core features:**
- Projects → Boards → Columns → Cards hierarchy
- JWT access tokens + rotating refresh tokens
- Role-based board access (Owner / Member)
- Card assignment with notifications
- Full-text card search
- Avatar upload, due dates, color labels

---

## Tech Stack

| | |
|---|---|
| **Framework** | ASP.NET Core 10 – Minimal API |
| **ORM** | Entity Framework Core 10 |
| **Auth** | ASP.NET Identity + JWT Bearer |
| **Database** | SQLite (local) → Neon PostgreSQL (production) |
| **Testing** | xUnit + EF InMemory + WebApplicationFactory |
| **CI/CD** | GitHub Actions → Render / Netlify |

---

## Architecture

The project follows a clean layered structure with a clear separation of concerns.

**Endpoints** handle HTTP routing only - no business logic.
**Services** contain all business logic and are injected via DI.
**Authorization handlers** enforce board-level access policies.
**Extensions** keep `Program.cs` clean and readable.
KanbanApp.Backend/
│
├── Endpoints/ HTTP route handlers (Auth, Board, Card, Column, Project, User, Notification)
├── Services/ Business logic (BoardService, CardService, UserService)
├── Models/ EF Core entities
├── DTOs/ Request / Response objects
├── Authorization/ Custom policy handlers (IsBoardOwner, IsBoardMember)
├── Data/ ApplicationDbContext
├── Extensions/ AddAuth(), AddDatabase(), AddAppServices(), AddSwagger()
└── Migrations/ EF Core migrations (SQL Server)


`Program.cs` is intentionally minimal:

```csharp
builder.Services.AddDatabase(builder.Configuration, builder.Environment);
builder.Services.AddAuth(builder.Configuration);
builder.Services.AddAppServices();
builder.Services.AddSwagger();
API Endpoints
Auth
Method	Endpoint	Auth	Notes
POST	/register	-	Rate limited – 5 req/min
POST	/login	-	Returns accessToken + refreshToken
POST	/api/auth/refresh	-	Rotates refresh token
POST	/api/auth/logout	JWT	Revokes refresh token
Projects · Boards · Columns · Cards
Method	Endpoint	Notes
GET POST	/api/projects	
PUT DELETE	/api/projects/{id}	
GET POST	/api/boards	
PUT DELETE	/api/boards/{id}	
GET POST	/api/boards/{id}/columns	
PUT DELETE	/api/boards/{id}/columns/{columnId}	
GET POST	/api/boards/{id}/cards	
PUT DELETE	/api/boards/{id}/cards/{cardId}	
PUT	/api/boards/{id}/cards/{cardId}/assign	Creates notification
GET	/api/boards/{id}/cards/search?q=	Search by title / description
GET POST	/api/boards/{id}/members	
Users · Notifications
Method	Endpoint	Notes
GET PUT	/api/users/me	
POST	/api/users/me/avatar	File upload
GET	/api/notifications	
PUT	/api/notifications/{id}/read	
PUT	/api/notifications/read-all	
All endpoints above require JWT Bearer token.

Authentication
The API uses a dual-token strategy - short-lived access tokens with rotating refresh tokens stored in the database.


POST /login
  → accessToken  (JWT, 15 min)
  → refreshToken (random 64 bytes, 7 days, stored in DB)

On 401 → POST /api/auth/refresh
  → old refresh token is revoked
  → new token pair is issued
  → original request is retried automatically (axios interceptor)
Security summary:

Access token expires in 15 minutes
Refresh token is single-use and rotated on every refresh
Auth endpoints are rate-limited to 5 req/min (HTTP 429)
Board access enforced by IsBoardOwner / IsBoardMember policies
CORS restricted to frontend origin only
Testing
60 integration tests - all passing.


KanbanWebAppFactory
  └── replaces DbContext with EF InMemory (unique DB per test run)
  └── disables rate limiter via RateLimiting:Disabled=true

TestBase helpers
  ├── CreateAuthenticatedClientAsync()
  ├── CreateBoardAsync()
  ├── CreateColumnAsync()
  ├── CreateCardAsync()
  └── CreateProjectAsync()
Suite	Tests	Coverage
CardTests	14	100%
ColumnTests	10	100%
ProjectTests	10	98.2%
AuthTests	8	96.8%
BoardTests	12	~95%
UserProfileTests	4	54.5%
Total	60	~95%
Bash

dotnet test --collect:"XPlat Code Coverage"
Getting Started
Bash

git clone https://github.com/Shellty-IT/KanbanApp_Project.git
cd KanbanApp_Project/KanbanApp.Backend

dotnet restore
dotnet ef database update
dotnet run
API: http://localhost:5067
Swagger: http://localhost:5067/swagger
appsettings.Development.json:

JSON

{
  "Jwt": {
    "Key": "your-secret-key-minimum-32-characters"
  },
  "ConnectionStrings": {
    "DefaultConnection": "Data Source=kanban.db"
  }
}
Deployment
Deployed to Render (Backend) and Netlify (Frontend) on every push to main via GitHub Actions.


push to main
  ├── dotnet restore + build
  ├── dotnet test              (all 60 tests must pass)
  ├── dotnet publish
  └── deploy
Resource	Name
Backend	https://smartquote-backend-fzh5.onrender.com
Frontend	https://shellty-kanban.netlify.app
Database	https://neon.com

Author
Shellty – Tomasz Skorupski

GitHub
LinkedIn

Portfolio project – Engineering Academy with Nerds Family