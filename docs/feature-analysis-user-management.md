# Feature Analysis: Charter Admin User Management

## Feature Context
Enhance the existing registration approval system to provide comprehensive user management capabilities for Charter Administrators, including user creation, editing, password reset, and lockout management.

## Users/Roles Affected
- **Primary**: Charter Admin (full access to user management)
- **Secondary**: All system users (subjects of management)
- **Restricted**: RebosaAdmin, PpraAdmin (no access to user management)

## Current Behavior
- Basic approval/rejection of registration requests via ApprovalsController
- Users stored in SQLite database using ASP.NET Core Identity
- ApplicationUser entity with properties: FirstName, LastName, Organization, IdNumber, Cell, Address, RequestedRole
- Simple table view on Approvals page with Approve/Reject buttons
- No ability to edit user details or create users directly

## Desired Behavior
1. **Enhanced Approval Page**:
   - Inline editing of user details before approval
   - Quick actions for password reset and lockout management
   - User creation modal/form on the same page

2. **User Management Features**:
   - Create new users directly without registration process
   - Edit existing user details (name, email, organization, etc.)
   - Reset passwords with temporary password generation
   - Manage lockout status (lock/unlock accounts)
   - Assign/change roles

3. **Awesome UX**:
   - Modern modal dialogs for user editing
   - Inline editing with smooth animations
   - Real-time validation and feedback
   - Confirmation dialogs for critical actions
   - Toast notifications for success/error messages

## Conventions & Patterns to Apply
- **Architecture**: Controller → Service → Repository pattern
- **Security**: Policy-based authorization (RequireCharterAdmin), ValidateAntiForgeryToken
- **Data**: EF Core with SQLite, AsNoTracking for reads, async/await pattern
- **UI**: SB Admin 2 components, Bootstrap modals, responsive design
- **Validation**: FluentValidation for server-side, client-side validation
- **Result Pattern**: Use Result<T> for service operations

## Reuse Opportunities
- Existing ApplicationUser and Identity infrastructure
- Current AppDbContext and repository patterns
- SB Admin 2 theme components and styles
- Notification system (_Notifications.cshtml)
- Existing validation patterns

## Risks & Dependencies
- **Data Integrity**: Ensure no modifications to MariaDB databases
- **Security**: Password reset must generate secure temporary passwords
- **Audit**: All user modifications should be logged
- **Email**: Password reset notifications via existing email service
- **Validation**: Prevent duplicate emails and invalid role assignments

## User Stories & Acceptance Criteria

### Story 1: Edit User Details
**As a** Charter Admin  
**I want to** edit user details on the approval page  
**So that** I can correct information before approving accounts

**Acceptance Criteria**:
- Edit button opens modal with user details
- All user fields are editable except ID
- Changes are validated before saving
- Success/error notifications displayed

### Story 2: Create New User
**As a** Charter Admin  
**I want to** create new users directly  
**So that** I can add users without the registration process

**Acceptance Criteria**:
- "Add User" button on approvals page
- Modal form with all required fields
- Auto-generate temporary password
- Email notification to new user
- Role assignment during creation

### Story 3: Reset Password
**As a** Charter Admin  
**I want to** reset user passwords  
**So that** I can help users who've forgotten their passwords

**Acceptance Criteria**:
- Reset password button per user
- Generate secure temporary password
- Display temporary password to admin
- Option to email password to user
- Force password change on next login (optional)

### Story 4: Manage Lockout Status
**As a** Charter Admin  
**I want to** lock/unlock user accounts  
**So that** I can control access for security reasons

**Acceptance Criteria**:
- Visual indicator for locked accounts
- Lock/Unlock toggle button
- Confirmation dialog for status changes
- Audit log entry for lockout changes
- Immediate effect on user access

## Implementation Order
1. Database: No changes needed (using existing Identity tables)
2. Entities: Extend ApplicationUser if needed
3. Repository: Create IUserRepository for user queries
4. Service: Create UserManagementService
5. ViewModels: UserEditVm, UserCreateVm, UserListVm
6. Controller: Enhance ApprovalsController or create AdminController
7. Views: Enhanced Approvals/Index with modals and inline editing
8. Tests: Unit tests for service, integration tests for controller
