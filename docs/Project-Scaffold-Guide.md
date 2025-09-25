## Charter Reporter App – Scaffold & Build Guide

### Custom Inputs That Guided Decisions
- **Authoritative guide**: `README.md` dictates architecture (ASP.NET Core MVC, .NET 8, Clean layers, server-side-first, RBAC, security posture).
- **Cursor rules**: `/.cursor/.cursorrules`, `/.cursor/rules/MASTER-CURSORRULES.mdc`, `/.cursor/rules/QUICK-START.mdc` enforce coding, security, and workflow practices.
- **Stack commitments**:
  - **.NET 8**: Target `net8.0` per `README.md` and `/.cursor/.cursorrules`.
  - **Clean layering**: Controllers → Services → Repositories → DbContexts; Views consume ViewModels/DTOs only.
  - **Server-side-first**: No client aggregation; live data only (no static demo data in views/scripts).
- **Future UI**: SB Admin 2 theme planned; no frontend tooling required at scaffold stage.

---

## 1) Prerequisites
- **.NET SDK 8.x** installed. Verify:
```bash
dotnet --info
```
- Terminal: Git Bash or PowerShell. Commands below are written for Git Bash; PowerShell variants are noted where relevant.

---

## 2) Solution and Folder Skeleton
Reasoning: Aligns with `README.md` layered architecture and separation of concerns; enables independent testing and clear references.

Commands:
```bash
mkdir -p src/Application src/Domain src/Infrastructure src/Shared src/Web tests
dotnet new sln -n Charter.Reporter
dotnet new gitignore
```

Resulting structure:
```
repo-root/
├── src/
│   ├── Application/
│   ├── Domain/
│   ├── Infrastructure/
│   ├── Shared/
│   └── Web/
├── tests/
├── Charter.Reporter.sln
└── .gitignore
```

---

## 3) Project Scaffolding
Reasoning: Minimal viable projects to compile early; keep Web empty for now, consistent with server-side-first policy.

Commands:
```bash
# Web (ASP.NET Core Empty)
dotnet new web -n Charter.Reporter.Web -o src/Web

# Class Libraries for layers
dotnet new classlib -n Charter.Reporter.Application   -o src/Application
dotnet new classlib -n Charter.Reporter.Domain        -o src/Domain
dotnet new classlib -n Charter.Reporter.Infrastructure -o src/Infrastructure
dotnet new classlib -n Charter.Reporter.Shared        -o src/Shared

# Tests
dotnet new xunit -n Charter.Reporter.Tests -o tests/Tests

# EditorConfig baseline (optional but recommended)
dotnet new editorconfig
```

Add projects to solution:
```bash
dotnet sln Charter.Reporter.sln add \
  src/Web/Charter.Reporter.Web.csproj \
  src/Application/Charter.Reporter.Application.csproj \
  src/Domain/Charter.Reporter.Domain.csproj \
  src/Infrastructure/Charter.Reporter.Infrastructure.csproj \
  src/Shared/Charter.Reporter.Shared.csproj \
  tests/Tests/Charter.Reporter.Tests.csproj
```

---

## 4) Target Framework Alignment
Reasoning: `README.md` pins ASP.NET Core to v8; we align all projects to `net8.0` for consistency and compatibility.

Edit each `*.csproj` and set:
```xml
<PropertyGroup>
  <TargetFramework>net8.0</TargetFramework>
  <Nullable>enable</Nullable>
  <ImplicitUsings>enable</ImplicitUsings>
  <!-- Tests also include <IsPackable>false</IsPackable> -->
  <!-- Web uses Sdk="Microsoft.NET.Sdk.Web" -->
  <!-- Libraries use Sdk="Microsoft.NET.Sdk" -->
  <!-- Tests use Sdk="Microsoft.NET.Sdk" with xUnit packages -->
  
</PropertyGroup>
```

Projects updated:
- `src/Web/Charter.Reporter.Web.csproj`
- `src/Application/Charter.Reporter.Application.csproj`
- `src/Domain/Charter.Reporter.Domain.csproj`
- `src/Infrastructure/Charter.Reporter.Infrastructure.csproj`
- `src/Shared/Charter.Reporter.Shared.csproj`
- `tests/Tests/Charter.Reporter.Tests.csproj`

---

## 5) Enforce Layered References
Reasoning: Enforces Clean Architecture boundaries; prevents leaking dependencies upward.

Commands:
```bash
# Web depends on Application, Shared
dotnet add src/Web/Charter.Reporter.Web.csproj reference \
  src/Application/Charter.Reporter.Application.csproj \
  src/Shared/Charter.Reporter.Shared.csproj

# Application depends on Domain, Shared
dotnet add src/Application/Charter.Reporter.Application.csproj reference \
  src/Domain/Charter.Reporter.Domain.csproj \
  src/Shared/Charter.Reporter.Shared.csproj

# Infrastructure depends on Domain, Shared
dotnet add src/Infrastructure/Charter.Reporter.Infrastructure.csproj reference \
  src/Domain/Charter.Reporter.Domain.csproj \
  src/Shared/Charter.Reporter.Shared.csproj

# Tests can reference all
dotnet add tests/Tests/Charter.Reporter.Tests.csproj reference \
  src/Application/Charter.Reporter.Application.csproj \
  src/Domain/Charter.Reporter.Domain.csproj \
  src/Infrastructure/Charter.Reporter.Infrastructure.csproj \
  src/Shared/Charter.Reporter.Shared.csproj
```

Visual reference:
```
Web → Application, Shared
Application → Domain, Shared
Infrastructure → Domain, Shared
Tests → Application, Domain, Infrastructure, Shared
```

---

## 6) Organize Cursor Rules
Reasoning: Keep rules discoverable and consistent with `/.cursor/rules/README.md` structure; enables deterministic patterns.

Commands:
```bash
mkdir -p .cursor/rules/Base
cp .cursor/.cursorrules .cursor/rules/Base/.cursorrules
# Verify presence of MASTER-CURSORRULES.mdc, QUICK-START.mdc, routers, and subfolders
ls -la .cursor/rules
```

Notes:
- Primary rules remain at `/.cursor/.cursorrules` (active). A copy exists under `/.cursor/rules/Base/.cursorrules` for structure and reference.
- Additional routers are kept alongside per rules README: `cpd-router.mdc`, `export-safety-router.mdc`, `data-source-router.mdc`.

---

## 7) Build
Reasoning: Early compilation confirms references and SDK alignment.

Command:
```bash
dotnet build
```

Expected result: `Build succeeded. 0 Warning(s) 0 Error(s)`.

---

## 8) Run & Verify
Reasoning: Validate the web host boots and serves an HTTP response. Keep it simple; no controller/views yet.

Start the Web project on a fixed port:
```bash
dotnet run --project src/Web/Charter.Reporter.Web.csproj --urls http://localhost:5080
```

Verify with curl (Git Bash):
```bash
curl -s -o /dev/null -w "%{http_code}" http://localhost:5080
# Expected: 200
```

PowerShell alternative probe:
```powershell
try {
  (Invoke-WebRequest -Uri http://localhost:5080 -UseBasicParsing -TimeoutSec 10).StatusCode
} catch {
  Write-Host $_.Exception.Message
}
```

Note: If using PowerShell from Git Bash, mind quoting/escaping differences.

---

## 9) Reasoning Summary (Why These Choices)
- **.NET 8 target**: `README.md` mandates ASP.NET Core v8; aligns runtime, packages, and hosting guidance.
- **Layered projects**: Enforces separation and testability; prevents architectural erosion.
- **Empty Web template**: Minimal surface area; no premature dependencies; adhere to server-side-first policy.
- **Cursor rules organization**: Mirrors rules README; ensures consistent, deterministic code generation and workflows.
- **Fixed port for run**: Simplifies verification and scripting across environments.

---

## 10) Troubleshooting
- **SDK mismatch / cannot restore**: Ensure `dotnet --info` shows .NET 8 SDK. Install from `https://dotnet.microsoft.com`.
- **Port already in use**: Change `--urls` port or free the port:
  - Windows: `netstat -ano | findstr :5080` → `taskkill /PID <pid> /F`
- **PowerShell quoting errors**: Use pure PowerShell or pure Git Bash; avoid cross-shell quoting.
- **HTTPS dev cert**: If HTTPS is needed, trust the dev cert: `dotnet dev-certs https --trust`.

---

## 11) One-Command Repro (from clean repo)
```bash
mkdir -p src/Application src/Domain src/Infrastructure src/Shared src/Web tests && \
dotnet new sln -n Charter.Reporter && \
dotnet new web -n Charter.Reporter.Web -o src/Web && \
dotnet new classlib -n Charter.Reporter.Application   -o src/Application && \
dotnet new classlib -n Charter.Reporter.Domain        -o src/Domain && \
dotnet new classlib -n Charter.Reporter.Infrastructure -o src/Infrastructure && \
dotnet new classlib -n Charter.Reporter.Shared        -o src/Shared && \
dotnet new xunit -n Charter.Reporter.Tests -o tests/Tests && \
dotnet sln Charter.Reporter.sln add \
  src/Web/Charter.Reporter.Web.csproj \
  src/Application/Charter.Reporter.Application.csproj \
  src/Domain/Charter.Reporter.Domain.csproj \
  src/Infrastructure/Charter.Reporter.Infrastructure.csproj \
  src/Shared/Charter.Reporter.Shared.csproj \
  tests/Tests/Charter.Reporter.Tests.csproj && \
dotnet add src/Web/Charter.Reporter.Web.csproj reference \
  src/Application/Charter.Reporter.Application.csproj \
  src/Shared/Charter.Reporter.Shared.csproj && \
dotnet add src/Application/Charter.Reporter.Application.csproj reference \
  src/Domain/Charter.Reporter.Domain.csproj \
  src/Shared/Charter.Reporter.Shared.csproj && \
dotnet add src/Infrastructure/Charter.Reporter.Infrastructure.csproj reference \
  src/Domain/Charter.Reporter.Domain.csproj \
  src/Shared/Charter.Reporter.Shared.csproj && \
dotnet add tests/Tests/Charter.Reporter.Tests.csproj reference \
  src/Application/Charter.Reporter.Application.csproj \
  src/Domain/Charter.Reporter.Domain.csproj \
  src/Infrastructure/Charter.Reporter.Infrastructure.csproj \
  src/Shared/Charter.Reporter.Shared.csproj && \
echo "Set <TargetFramework>net8.0</TargetFramework> in all csprojs" && \
dotnet build
```

---

## 12) Next Steps (Optional, Recommended)
- **Solution folders**: Group projects (Web, Application, Domain, Infrastructure, Shared, Tests) within the solution for IDE clarity.
- **Directory.Build.props**: Centralize `TargetFramework`, `Nullable`, and common analyzers.
- **Initial middleware**: Add correlation ID, global exception handling, and minimal landing endpoint returning 200 + correlation ID.
- **CI**: Add a simple CI to build and run tests on push.


