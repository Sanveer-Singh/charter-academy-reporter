# SendGrid Email Configuration Guide

This guide explains how to set up SendGrid for sending real emails in both development and production environments.

## Prerequisites

1. A SendGrid account (sign up at https://sendgrid.com)
2. A verified sender domain or email address in SendGrid
3. An API key from SendGrid

## Setting Up SendGrid

### Step 1: Create a SendGrid API Key

1. Log in to your SendGrid account
2. Navigate to **Settings** → **API Keys**
3. Click **Create API Key**
4. Give your key a name (e.g., "Charter Reporter Dev" or "Charter Reporter Production")
5. Select **Full Access** or **Restricted Access** with at least "Mail Send" permissions
6. Click **Create & View**
7. **Important**: Copy the API key immediately as it won't be shown again

### Step 2: Verify Your Sender

1. In SendGrid, go to **Settings** → **Sender Authentication**
2. Either:
   - **Domain Authentication** (recommended for production): Verify your entire domain
   - **Single Sender Verification** (good for development): Verify a single email address
3. Follow SendGrid's verification process

### Step 3: Configure the Application

#### For Development Environment

1. Set the SendGrid API key as an environment variable:

   **Windows (Command Prompt):**
   ```cmd
   set SENDGRID_API_KEY=your_api_key_here
   ```

   **Windows (PowerShell):**
   ```powershell
   $env:SENDGRID_API_KEY="your_api_key_here"
   ```

   **Linux/Mac:**
   ```bash
   export SENDGRID_API_KEY="your_api_key_here"
   ```

2. Update `appsettings.Development.json` with your sender email:
   ```json
   "Email": {
     "FromAddress": "your-verified-email@yourdomain.com",
     "FromName": "Charter Reporter"
   }
   ```

#### For Production Environment

1. Set the SendGrid API key as an environment variable on your server
2. Update `appsettings.json` with your production sender email

### Step 4: Test Email Sending

1. Start the application:
   ```bash
   cd src/Web
   dotnet run
   ```

2. The console will show which email service is being used:
   - ✅ `Email Service: Using SendGrid (API key configured)` - SendGrid is active
   - ⚠️ `WARNING: Email Service: Using DevNoopEmailSender` - No emails will be sent

3. Test the email functionality:
   - **Registration**: Create a new account to test the email verification
   - **Password Reset**: Use the "Forgot Password?" link on the login page

## How the Email System Works

### Service Priority

The application chooses the email service in this order:
1. **SendGrid** - If `SENDGRID_API_KEY` environment variable is set
2. **SMTP** - If SMTP settings are configured (not smtp.example.com)
3. **DevNoop** - Development only, logs emails but doesn't send them

### Email Types Sent

1. **Account Verification** - Sent when a user registers
2. **Password Reset** - Sent when a user requests a password reset
3. **Role Approval Notifications** - Sent when admin approves/rejects user roles

### Development Mode Features

In development mode (`ASPNETCORE_ENVIRONMENT=Development`):
- Email links are displayed in the browser for easy testing
- Detailed error messages are shown if email sending fails
- Console logs show email sending attempts and results

## Troubleshooting

### Common Issues

1. **"SendGrid API key is missing"**
   - Ensure the `SENDGRID_API_KEY` environment variable is set
   - Restart the application after setting the variable

2. **"SendGrid send failed with status 401"**
   - The API key is invalid or has insufficient permissions
   - Create a new API key with "Mail Send" permissions

3. **"SendGrid send failed with status 403"**
   - Your sender email is not verified
   - Verify your sender domain or email address in SendGrid

4. **No emails received**
   - Check SendGrid's Activity Feed for delivery status
   - Check spam/junk folders
   - Ensure the recipient email is correct

### Monitoring

Monitor email delivery in SendGrid:
1. Log in to SendGrid
2. Go to **Activity** → **Activity Feed**
3. View email status (delivered, opened, bounced, etc.)

## Security Best Practices

1. **Never commit API keys** to source control
2. Use different API keys for development and production
3. Restrict API key permissions to only what's needed
4. Rotate API keys periodically
5. Use domain authentication for production

## Additional Configuration (Optional)

### Data Residency

If you need to specify SendGrid data residency (EU or Global):
```bash
export SENDGRID_DATA_RESIDENCY=eu  # or "global"
```

### Custom Email Templates

To use SendGrid's dynamic templates:
1. Create templates in SendGrid
2. Modify the email sending code to use template IDs
3. Pass dynamic data to populate the templates

## Support

For SendGrid-specific issues:
- SendGrid Documentation: https://docs.sendgrid.com
- SendGrid Support: https://support.sendgrid.com

For application-specific issues:
- Contact: support@charter-reporter.com
