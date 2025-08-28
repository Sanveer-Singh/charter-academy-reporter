# Charter Reporter App

A secure, role-based reporting application built with ASP.NET Core 8.0 and SB Admin 2 theme, following clean architecture principles.

## 🚀 Features

### Authentication & Security
- ✅ Secure user registration with email verification
- ✅ Admin approval workflow for new users
- ✅ Role-based access control (Charter-Admin, Rebosa-Admin, PPRA-Admin)
- ✅ Comprehensive audit logging
- ✅ Rate limiting and security validation
- ✅ OWASP compliant security measures

### User Interface
- ✅ SB Admin 2 theme with proper CSS hierarchy
- ✅ Responsive design with mobile-first approach
- ✅ WCAG 2.1 AA accessibility compliance
- ✅ Progressive enhancement

### Data Integration
- ✅ Multi-database support (App, Moodle, WooCommerce)
- ✅ Repository pattern implementation
- ✅ Entity Framework Core with MySQL

### Reporting & Analytics
- 🔄 Role-based dashboards
- 🔄 Data visualization with charts
- 🔄 CSV/XLSX export functionality
- 🔄 Conversion funnel analysis

## 🏗️ Architecture

The application follows Clean Architecture principles with clear separation of concerns:

```
├── Charter.ReporterApp.Domain/      # Core business logic
│   ├── Entities/                    # Domain entities
│   ├── ValueObjects/               # Value objects
│   └── Interfaces/                 # Repository interfaces
│
├── Charter.ReporterApp.Application/ # Business services
│   ├── Services/                   # Application services
│   ├── DTOs/                       # Data transfer objects
│   └── Interfaces/                 # Service interfaces
│
├── Charter.ReporterApp.Infrastructure/ # External integrations
│   ├── Data/                       # Database contexts
│   ├── Repositories/               # Repository implementations
│   └── Services/                   # Infrastructure services
│
└── Charter.ReporterApp.Web/        # Presentation layer
    ├── Controllers/                # MVC controllers
    ├── Views/                      # Razor views
    ├── Areas/                      # Role-specific areas
    └── wwwroot/                    # Static assets
```

## 🎨 CSS Hierarchy

The application implements a strict CSS hierarchy to ensure maintainability:

1. **Base Layer** (`sb-admin-2.css`) - Never modify
2. **Site Layer** (`variables.css`, `site.css`) - Global overrides
3. **Module Layer** (`modules/*.css`) - Component-specific styles

### CSS Variables
All theming is done through CSS custom properties defined in `variables.css`:

```css
:root {
    --charter-primary: #1e3a8a;
    --charter-success: #10b981;
    --charter-danger: #ef4444;
    /* ... */
}
```

## 🔐 Security Features

### Authentication Flow
1. User submits registration form
2. Email verification sent
3. User clicks verification link
4. Request appears in admin approval queue
5. Charter Admin approves/rejects
6. User receives notification email
7. Approved users can log in

### Security Measures
- Input validation and sanitization
- Rate limiting on sensitive endpoints
- Comprehensive audit logging
- Secure session management
- CSRF protection
- XSS prevention
- Content Security Policy headers

## 🗄️ Database Schema

### Main Application Database
- `Users` - Application users (extends IdentityUser)
- `Roles` - Application roles (extends IdentityRole)
- `RegistrationRequests` - Pending user registrations
- `AuditLogs` - Comprehensive audit trail

### External Databases
- **Moodle** - Read-only access to course and enrollment data
- **WooCommerce** - Read-only access to sales and customer data

## 🚀 Getting Started

### Prerequisites
- .NET 8.0 SDK
- MySQL Server 8.0+
- Node.js (for SB Admin 2 assets)

### Installation

1. **Clone the repository**
   ```bash
   git clone <repository-url>
   cd Charter.ReporterApp
   ```

2. **Configure databases**
   Update `appsettings.json` with your database connections:
   ```json
   {
     "ConnectionStrings": {
       "DefaultConnection": "Server=localhost;Database=CharterReporterApp;Uid=root;Pwd=your_password;",
       "MoodleConnection": "Server=localhost;Database=moodle;Uid=moodle_user;Pwd=moodle_password;",
       "WooCommerceConnection": "Server=localhost;Database=wordpress;Uid=wp_user;Pwd=wp_password;"
     }
   }
   ```

3. **Install SB Admin 2 assets**
   ```bash
   # Download SB Admin 2 and place in wwwroot/vendor/sb-admin-2/
   # Or use CDN links in the layout files
   ```

4. **Run database migrations**
   ```bash
   cd Charter.ReporterApp.Web
   dotnet ef database update
   ```

5. **Configure email settings**
   Update `EmailSettings` in `appsettings.json`:
   ```json
   {
     "EmailSettings": {
       "SmtpServer": "smtp.gmail.com",
       "SmtpPort": 587,
       "FromEmail": "noreply@charter.co.za",
       "Username": "your_email@gmail.com",
       "Password": "your_app_password"
     }
   }
   ```

6. **Run the application**
   ```bash
   dotnet run
   ```

### Default Admin Account
- **Email**: `admin@charter.co.za`
- **Password**: `Charter@2024!`
- **Role**: Charter-Admin

⚠️ **Important**: Change the default admin password immediately after first login!

## 🔧 Configuration

### Environment Variables
For production deployment, use environment variables instead of `appsettings.json`:

```bash
export ConnectionStrings__DefaultConnection="Server=prod-server;..."
export EmailSettings__Password="secure-app-password"
export Security__DataProtectionKeysPath="/secure/path/keys"
```

### Feature Flags
Configure features in `appsettings.json`:

```json
{
  "Features": {
    "EnableEmailVerification": true,
    "EnableAuditLogging": true,
    "EnableRateLimiting": true,
    "EnableTwoFactorAuth": false,
    "MaintenanceMode": false
  }
}
```

## 📊 Role-Based Access

### Charter-Admin
- Full system access
- User approval/rejection
- User management
- All reports and dashboards
- Audit log access

### Rebosa-Admin
- Organization-specific dashboards
- Limited reporting capabilities
- Own profile management

### PPRA-Admin
- PPRA-specific dashboards
- CPD course reporting
- Regulatory compliance reports

## 🧪 Testing

### Running Tests
```bash
# Unit tests
dotnet test Charter.ReporterApp.Tests/

# Integration tests
dotnet test Charter.ReporterApp.IntegrationTests/
```

### Security Testing
- SQL injection prevention
- XSS protection
- CSRF validation
- Rate limiting verification
- Authentication bypass attempts

## 📝 Development Guidelines

### CSS Methodology
1. Never modify `sb-admin-2.css` directly
2. Use CSS variables for theming
3. Create module-specific CSS files
4. Follow mobile-first responsive design
5. Ensure WCAG 2.1 AA compliance

### Code Standards
- Follow SOLID principles
- Use dependency injection
- Implement comprehensive logging
- Write unit tests for business logic
- Document public APIs

### Security Checklist
- [ ] All inputs validated server-side
- [ ] Parameterized queries used exclusively
- [ ] Output encoding prevents XSS
- [ ] CSRF tokens on all forms
- [ ] Secure session management
- [ ] Role-based access control enforced
- [ ] Audit logging implemented
- [ ] Security headers configured

## 🚀 Deployment

### Production Checklist
- [ ] Update default admin password
- [ ] Configure secure connection strings
- [ ] Set up SSL certificates
- [ ] Configure email SMTP settings
- [ ] Set up database backups
- [ ] Configure application insights/logging
- [ ] Set maintenance page
- [ ] Test all security features
- [ ] Perform load testing

### Docker Deployment
```dockerfile
# Dockerfile example for production deployment
FROM mcr.microsoft.com/dotnet/aspnet:8.0 AS base
WORKDIR /app
EXPOSE 80
EXPOSE 443

FROM mcr.microsoft.com/dotnet/sdk:8.0 AS build
WORKDIR /src
COPY . .
RUN dotnet restore
RUN dotnet build -c Release -o /app/build

FROM build AS publish
RUN dotnet publish -c Release -o /app/publish

FROM base AS final
WORKDIR /app
COPY --from=publish /app/publish .
ENTRYPOINT ["dotnet", "Charter.ReporterApp.Web.dll"]
```

## 📖 API Documentation

### Authentication Endpoints
- `POST /Account/Login` - User authentication
- `POST /Account/Register` - User registration
- `GET /Account/ConfirmEmail` - Email verification
- `POST /Account/Logout` - User logout

### Admin Endpoints
- `GET /Admin/Approvals` - Approval queue
- `POST /Admin/ApproveRequest/{id}` - Approve registration
- `POST /Admin/RejectRequest/{id}` - Reject registration
- `GET /Admin/GetPendingCount` - Get pending count

## 🤝 Contributing

1. Fork the repository
2. Create a feature branch
3. Follow coding standards
4. Write tests for new features
5. Update documentation
6. Submit a pull request

## 📄 License

This project is proprietary to Charter Institute. All rights reserved.

## 📞 Support

For technical support or questions:
- **Email**: support@charter.co.za
- **Documentation**: [Internal Wiki](https://wiki.charter.co.za)
- **Issues**: Use the project issue tracker

---

**Charter Institute Reporter App** - Secure, scalable, and user-friendly reporting solution.