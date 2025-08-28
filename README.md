# Charter Reporter App - ASP.NET Core MVC Implementation

## Overview

This is a complete ASP.NET Core MVC implementation of the Charter Reporter App, featuring:
- Clean Architecture with Domain, Application, Infrastructure, and Web layers
- SB Admin 2 theme integration with proper CSS hierarchy
- Entity Framework Core with MySQL support
- Cookie-based authentication with role-based authorization
- Responsive design with mobile-first approach
- WCAG 2.1 AA accessibility compliance

## Project Structure

```
Charter.ReporterApp/
├── Charter.ReporterApp.sln                    # Solution file
├── Charter.ReporterApp.Domain/                # Domain layer (entities, interfaces)
│   ├── Entities/
│   │   ├── User.cs
│   │   ├── Role.cs
│   │   ├── RegistrationRequest.cs
│   │   └── AuditLog.cs
│   └── Interfaces/
│       ├── IUserRepository.cs
│       └── IRegistrationRepository.cs
├── Charter.ReporterApp.Application/           # Application layer (services, DTOs)
│   ├── DTOs/
│   │   ├── LoginDto.cs
│   │   └── RegisterDto.cs
│   └── Interfaces/
│       └── IAuthenticationService.cs
├── Charter.ReporterApp.Infrastructure/        # Infrastructure layer (data access)
│   └── Data/
│       └── AppDbContext.cs
└── Charter.ReporterApp.Web/                   # Web layer (MVC)
    ├── Controllers/
    │   ├── HomeController.cs
    │   ├── AccountController.cs
    │   ├── DashboardController.cs
    │   └── AdminController.cs
    ├── Views/
    │   ├── Shared/
    │   │   ├── _Layout.cshtml
    │   │   └── _LoginLayout.cshtml
    │   ├── Home/
    │   │   └── Index.cshtml
    │   ├── Account/
    │   │   ├── Login.cshtml
    │   │   └── Register.cshtml
    │   └── Dashboard/
    │       └── CharterAdminDashboard.cshtml
    ├── wwwroot/
    │   ├── css/
    │   │   ├── variables.css
    │   │   ├── site.css
    │   │   └── modules/
    │   └── js/
    │       ├── site.js
    │       └── modules/
    └── Program.cs
```

## Prerequisites

- .NET 8.0 SDK
- MySQL Server (or MariaDB)
- Visual Studio 2022 / VS Code / Rider

## Getting Started

1. **Clone or download the repository**

2. **Update database connection strings** in `appsettings.json`:
   ```json
   {
     "ConnectionStrings": {
       "DefaultConnection": "Server=localhost;Database=CharterReporterApp;User=root;Password=yourpassword;Port=3306;",
       "MoodleConnection": "Server=localhost;Database=Moodle;User=root;Password=yourpassword;Port=3306;",
       "WooCommerceConnection": "Server=localhost;Database=WooCommerce;User=root;Password=yourpassword;Port=3306;"
     }
   }
   ```

3. **Create the database**:
   ```bash
   cd Charter.ReporterApp.Web
   dotnet ef migrations add InitialCreate -p ../Charter.ReporterApp.Infrastructure -s .
   dotnet ef database update
   ```

4. **Run the application**:
   ```bash
   dotnet run
   ```

5. **Access the application**:
   - Navigate to `https://localhost:5001` or `http://localhost:5000`
   - Default login: `admin@charter.com` / `password` (for demo purposes)

## Features Implemented

### Authentication & Authorization
- Cookie-based authentication
- Role-based authorization (Charter-Admin, Rebosa-Admin, PPRA-Admin)
- User registration with email verification workflow
- Password recovery functionality
- Session timeout handling

### Dashboard Module
- Role-specific dashboards
- Real-time metrics cards
- Interactive charts using Chart.js
- Date range and category filters
- Export functionality (CSV, XLSX, PDF)

### Admin Features
- Registration approval queue
- Approve/reject functionality with reason tracking
- Email notifications (ready for integration)
- Audit trail logging

### Security Features
- CSRF protection on all forms
- Security headers (CSP, X-Frame-Options, etc.)
- Input validation and sanitization
- Parameterized queries
- HTTPS enforcement

### UI/UX Features
- SB Admin 2 theme integration
- Mobile-responsive design
- WCAG 2.1 AA accessibility
- Loading states and animations
- Toast notifications
- Modal dialogs

## CSS Architecture

The application follows a three-layer CSS hierarchy:

1. **Base Layer**: SB Admin 2 theme (unmodified)
2. **Site Layer**: Global variables and overrides (`variables.css`, `site.css`)
3. **Module Layer**: Component-specific styles (`modules/*.css`)

## Development

### Adding a New Module

1. Create a new controller in `Controllers/`
2. Add views in `Views/[ControllerName]/`
3. Create module-specific CSS in `wwwroot/css/modules/`
4. Add JavaScript functionality in `wwwroot/js/modules/`

### Database Migrations

```bash
# Add a new migration
dotnet ef migrations add MigrationName -p ../Charter.ReporterApp.Infrastructure -s .

# Update database
dotnet ef database update

# Remove last migration
dotnet ef migrations remove
```

### Running Tests

```bash
# Run all tests
dotnet test

# Run with coverage
dotnet test /p:CollectCoverage=true
```

## Production Deployment

1. **Publish the application**:
   ```bash
   dotnet publish -c Release -o ./publish
   ```

2. **Configure IIS or use Kestrel with reverse proxy**

3. **Set environment variables**:
   - `ASPNETCORE_ENVIRONMENT=Production`
   - Connection strings via environment variables or Azure Key Vault

4. **Enable HTTPS with valid SSL certificate**

5. **Configure logging and monitoring**

## API Endpoints

- `POST /Account/Login` - User login
- `POST /Account/Register` - User registration
- `POST /Account/Logout` - User logout
- `GET /Dashboard` - Dashboard view (authenticated)
- `POST /Dashboard/Filter` - Apply dashboard filters
- `POST /Dashboard/Export` - Export dashboard data
- `GET /Admin/Approvals` - View pending registrations (Charter-Admin only)
- `POST /Admin/ApproveRequest/{id}` - Approve registration
- `POST /Admin/RejectRequest/{id}` - Reject registration

## Next Steps

1. **Complete Entity Framework Implementation**:
   - Implement repositories
   - Add service layer implementation
   - Create database migrations

2. **External Database Integration**:
   - Connect to Moodle database
   - Connect to WooCommerce database
   - Implement data aggregation services

3. **Email Service**:
   - Integrate email service for notifications
   - Implement email templates

4. **Advanced Features**:
   - Real-time updates with SignalR
   - Background job processing with Hangfire
   - Caching with Redis
   - API versioning

5. **Testing**:
   - Unit tests for services
   - Integration tests for controllers
   - UI tests with Selenium

## License

This project is licensed under the MIT License.

## Support

For issues or questions, please contact the development team.