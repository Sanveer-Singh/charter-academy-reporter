# Feature Planning: Charter Admin User Management

## Test-First Setup

### Unit Tests (UserManagementService)
1. **CreateUser_WithValidData_ReturnsSuccess**
   - Input: Valid UserCreateVm
   - Expected: User created, temporary password generated
   - Edge: Duplicate email handling

2. **UpdateUser_WithValidData_ReturnsSuccess**
   - Input: Valid UserEditVm with existing user ID
   - Expected: User updated, audit logged
   - Edge: Non-existent user, concurrent updates

3. **ResetPassword_GeneratesSecurePassword**
   - Input: Valid user ID
   - Expected: 12+ char password with complexity
   - Edge: Invalid user ID

4. **ToggleLockout_ChangesStatus**
   - Input: User ID and lockout status
   - Expected: Status changed, audit logged
   - Edge: Already locked/unlocked state

### Integration Tests (ApprovalsController)
1. **OnlyCharterAdmin_CanAccessUserManagement**
   - Test policy enforcement
   - Verify 403 for other roles

2. **ApproveWithEdit_UpdatesAndApproves**
   - Input: Modified user data with approval
   - Expected: User updated and approved atomically

3. **CreateUser_SendsEmail**
   - Verify email service integration
   - Check temporary password delivery

## Atomic Task Breakdown

### Task 1: Create User Repository Interface & Implementation
**Objective**: Provide data access layer for user queries
**Constraints**: 
- Use IQueryable for efficient filtering
- AsNoTracking for read operations
- Include role relationships

**Data Contracts**:
```csharp
public interface IUserRepository
{
    Task<ApplicationUser?> GetByIdAsync(string id);
    Task<ApplicationUser?> GetByEmailAsync(string email);
    IQueryable<ApplicationUser> GetAllUsers();
    Task<bool> EmailExistsAsync(string email);
}
```

### Task 2: Create UserManagementService
**Objective**: Business logic for user operations
**Constraints**:
- Result<T> pattern for all operations
- Audit all modifications
- Validate business rules

**Operations**:
- CreateUserAsync(UserCreateVm)
- UpdateUserAsync(UserEditVm)
- ResetPasswordAsync(string userId)
- SetLockoutAsync(string userId, bool locked)
- GetUserDetailsAsync(string userId)

### Task 3: Create ViewModels
**Objective**: DTOs for user management
**Constraints**:
- FluentValidation rules
- No entity exposure

**ViewModels**:
```csharp
- UserCreateVm (all fields for new user)
- UserEditVm (editable fields + ID)
- UserDetailsVm (display model)
- PasswordResetResultVm (temp password + instructions)
```

### Task 4: Enhance ApprovalsController
**Objective**: Add user management actions
**Constraints**:
- [Authorize(Policy = RequireCharterAdmin)]
- [ValidateAntiForgeryToken] on all POST
- Async/await pattern

**New Actions**:
- GetUserDetails(string id) - AJAX endpoint
- EditUser(UserEditVm model) - AJAX endpoint
- CreateUser(UserCreateVm model) - AJAX endpoint
- ResetPassword(string id) - AJAX endpoint
- ToggleLockout(string id, bool locked) - AJAX endpoint

### Task 5: Create User Management UI Components
**Objective**: Modern, responsive UI for user management
**Constraints**:
- Bootstrap 4 modals
- SB Admin 2 theme
- WCAG 2.1 AA compliance
- No inline styles/scripts

**Components**:
1. **Edit User Modal**
   - Form with all editable fields
   - Client-side validation
   - Loading states
   - Error handling

2. **Create User Modal**
   - Similar to edit but for new users
   - Password generation display
   - Role selection

3. **Password Reset Modal**
   - Confirmation dialog
   - Display generated password
   - Copy to clipboard button

4. **Enhanced User Table**
   - Action buttons per row
   - Status indicators
   - Inline quick actions

### Task 6: Client-Side JavaScript Module
**Objective**: Handle AJAX operations and UX
**Constraints**:
- Use fetch API
- Anti-forgery token handling
- Proper error handling

**Functions**:
- openEditModal(userId)
- saveUser()
- createUser()
- resetPassword(userId)
- toggleLockout(userId)
- showToast(type, message)

### Task 7: Add Audit Logging
**Objective**: Track all user modifications
**Constraints**:
- Use existing AuditLog entity
- Include before/after values
- Timestamp and user context

### Task 8: Email Templates
**Objective**: Professional email notifications
**Constraints**:
- HTML with fallback text
- Include temporary passwords securely
- Clear instructions

## Full Implementation Plan

### Phase Order:
1. **Database Layer** (Task 1)
   - Create IUserRepository interface
   - Implement UserRepository with EF Core
   - Register in DI container

2. **Service Layer** (Task 2)
   - Create IUserManagementService interface
   - Implement UserManagementService
   - Add audit logging
   - Register in DI

3. **ViewModels** (Task 3)
   - Create all ViewModels
   - Add FluentValidation rules
   - Create AutoMapper profiles

4. **Controller** (Task 4)
   - Enhance ApprovalsController
   - Add AJAX endpoints
   - Add error handling

5. **Views** (Task 5-6)
   - Create modal partials
   - Enhance Approvals/Index.cshtml
   - Add JavaScript module
   - Style with CSS

6. **Email** (Task 8)
   - Create email templates
   - Integrate with service

7. **Tests** (Task 7)
   - Unit tests for service
   - Integration tests for controller
   - UI tests (manual)

## Risk Management

### Risks & Mitigations:
1. **Risk**: Accidental MariaDB modification
   - **Mitigation**: Only use AppDbContext, never MariaDbContext
   - **Validation**: Review all data access code

2. **Risk**: Weak temporary passwords
   - **Mitigation**: Use cryptographically secure generation
   - **Validation**: Minimum 12 chars with complexity

3. **Risk**: Unauthorized access
   - **Mitigation**: Policy-based authorization on all endpoints
   - **Validation**: Integration tests for each role

4. **Risk**: Data loss during updates
   - **Mitigation**: Audit before/after values
   - **Rollback**: Restore from audit log

## Out of Scope
- Bulk user operations
- CSV import/export
- Two-factor authentication changes
- Custom role creation
- User profile photos
- Activity tracking beyond audit logs

## Dependencies
- Existing Identity infrastructure
- Email service configuration
- SB Admin 2 theme
- Bootstrap 4
- jQuery (for Bootstrap modals)

## Performance Requirements
- User list: < 500ms for 1000 users
- Single user operations: < 200ms
- Email sending: async/fire-and-forget
- Pagination: 20 users per page
- Cancellation tokens on all async operations

## Security Requirements
- All operations require CharterAdmin role
- Anti-forgery tokens on all state changes
- No PII in logs except audit
- Secure password generation
- Rate limiting on password resets
- XSS prevention in all user inputs

## Accessibility Requirements
- ARIA labels on all interactive elements
- Keyboard navigation for modals
- Screen reader announcements
- Focus management in modals
- Color contrast WCAG AA
- Error messages associated with fields

## State Update
Phase: Planning ✓
Status: Completed
Next: Implementation
Feature: Charter Admin User Management
