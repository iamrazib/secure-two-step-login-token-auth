# EmployeeMvcAuth
# Secure Two-Step Login with Challenge ID and Session Token Authentication

A simple ASP.NET Core MVC web application demonstrating a secure two-step employee login process using **Challenge ID**, **Session Token**, and database-level token validation.

This project shows how to separate User ID verification and Password verification into different stages, then protect future employee data requests using a short-lived and revocable session token.

---

## Project Overview

Traditional login systems often collect both User ID and Password on the same page. This project demonstrates a more controlled authentication flow:

```text
Step 1: User enters User ID
Step 2: System verifies User ID and generates Challenge ID
Step 3: User enters Password on a separate page
Step 4: System verifies password using Challenge ID
Step 5: System generates Session Token
Step 6: Every protected request is validated using Session Token
```

The main goal is to ensure that protected employee data is not accessed directly through only User ID or Password. Instead, every sensitive request must contain a valid, active, non-expired, and non-revoked `SessionToken`.

---

## Key Security Idea

This project uses two important identifiers:

### 1. Challenge ID

`ChallengeId` is a temporary ID generated after the User ID is verified.

It is used only during the login process.

```text
User ID valid → Generate ChallengeId → Show Password Page
```

The `ChallengeId` helps connect the password verification step with the previously verified user.

It should be:

```text
Short-lived
Single-use
Server-side controlled
Expired after a few minutes
Invalid after password verification
```

---

### 2. Session Token

`SessionToken` is generated only after successful password verification.

```text
Password correct → Generate SessionToken → Load Dashboard
```

After login, protected pages such as Employee Salary and Leave Details are accessed only after validating the `SessionToken`.

```sql
WHERE SessionToken = @SessionToken
AND ExpiresAt > SYSUTCDATETIME()
AND RevokedAt IS NULL
```

If the token is invalid, expired, or revoked, the user is redirected to the login page.

---

## Why User ID and Password Are Taken on Separate Pages

This project separates User ID and Password to make the login process more controlled.

Instead of sending both credentials in one step, the system first verifies whether the user exists. If the User ID is valid, it creates a temporary `ChallengeId`.

Only then does the user enter the password.

```text
User ID Page
    ↓
Challenge ID Generated
    ↓
Password Page
    ↓
Session Token Generated
```

This allows the application to apply additional checks between the two stages, such as:

```text
Account status check
Login attempt control
Temporary challenge expiry
Device/IP validation
MFA integration possibility
```

---

## Important Note About Security

This project demonstrates a stronger authentication flow, but it does not mean that User ID and Password theft becomes harmless.

If an attacker knows the correct User ID and Password, they may still try to log in and generate a new session token.

Therefore, this design should be combined with:

```text
HTTPS
Password hashing
Secure HttpOnly cookies
Short session lifetime
Login attempt limits
Account lockout
Device monitoring
Multi-factor authentication
```

The main purpose of this project is to show that protected data should not be returned only because a User ID or Password is known. Protected data should be returned only when a valid `SessionToken` is present and verified.

---

## Features

```text
Two-step login flow
User ID verification page
Password verification page
Challenge ID generation
Password hashing
Session Token generation
10-minute token expiry
RevokedAt-based logout handling
Protected Dashboard page
Protected Employee Salary page
Protected Employee Leave Details page
Database-level session validation
Message display when no leave records exist
```

---

## Technology Stack

```text
ASP.NET Core MVC
C#
SQL Server
ADO.NET
Razor Views
Stored Procedures
Session-based authentication concept
```

---

## Authentication Flow

```text
[Login Page: User ID]
        ↓
Check User ID in Database
        ↓
Generate ChallengeId
        ↓
[Password Page]
        ↓
Verify Password using ChallengeId
        ↓
Generate SessionToken
        ↓
[Dashboard]
        ↓
Access Protected Pages using SessionToken
```

---

## Protected Page Flow

For every protected page, the system follows this process:

```csharp
if (!SessionTokenExists)
{
    RedirectToLogin();
}

if (!DatabaseConfirmsTokenIsValid)
{
    RedirectToLogin();
}

LoadProtectedData();
```

Example protected pages:

```text
Employee Dashboard
Employee Salary
Employee Leave Details
```

---

## Database Concept

The project uses three core database areas:

### Employees

Stores employee profile and password hash.

```text
EmployeeKey
UserId
PasswordHash
FullName
Email
IsActive
```

### LoginChallenges

Stores temporary login challenge information.

```text
ChallengeId
EmployeeKey
CreatedAt
ExpiresAt
IsUsed
```

### EmployeeSessions

Stores active login session information.

```text
SessionToken
EmployeeKey
CreatedAt
ExpiresAt
LastSeenAt
RevokedAt
```

---

## Session Expiry

Session validity is limited to 10 minutes.

```sql
ExpiresAt = DATEADD(MINUTE, 10, SYSUTCDATETIME())
```

This means the session token becomes invalid after 10 minutes.

When the user logs out, the token is revoked:

```sql
UPDATE EmployeeSessions
SET RevokedAt = SYSUTCDATETIME()
WHERE SessionToken = @SessionToken;
```

After revocation, the token can no longer be used.

---

## Example Use Case

Suppose an employee logs in with:

```text
User ID: E1001
Password: ********
```

The system first generates:

```text
ChallengeId = temporary login identifier
```

After password success, the system generates:

```text
SessionToken = active session identifier
```

Now, when the user clicks salary details, the database checks:

```text
Is SessionToken valid?
Is SessionToken expired?
Is SessionToken revoked?
Is Employee active?
```

Only then salary information is returned.

---

## Pages Included

```text
/Auth/Login
/Auth/Password
/Dashboard/Index
/EmployeeSalary/Index
/EmployeeLeaveDetails/Index
/EmployeeUser/Create
```

---

## Project Structure

```text
EmployeeMvcAuth
│
├── Controllers
│   ├── AuthController.cs
│   ├── DashboardController.cs
│   ├── EmployeeSalaryController.cs
│   ├── EmployeeLeaveDetailsController.cs
│   └── EmployeeUserController.cs
│
├── Data
│   └── AuthRepository.cs
│
├── Models
│   ├── LoginUserIdViewModel.cs
│   ├── PasswordViewModel.cs
│   ├── DashboardViewModel.cs
│   ├── EmployeeSalaryViewModel.cs
│   ├── EmployeeLeaveDetailsViewModel.cs
│   └── CreateEmployeeViewModel.cs
│
├── Views
│   ├── Auth
│   ├── Dashboard
│   ├── EmployeeSalary
│   ├── EmployeeLeaveDetails
│   └── EmployeeUser
│
├── appsettings.json
└── Program.cs
```

---

## How to Run

Clone the repository:

```bash
git clone https://github.com/your-username/secure-two-step-login-token-auth.git
```

Navigate to the project folder:

```bash
cd secure-two-step-login-token-auth
```

Update the database connection string in `appsettings.json`:

```json
{
  "ConnectionStrings": {
    "DefaultConnection": "Server=localhost;Database=EmployeeMvcAuth;Trusted_Connection=True;TrustServerCertificate=True;"
  }
}
```

Run the SQL scripts to create the required database tables and stored procedures.

Then run the project:

```bash
dotnet run
```

Open the application in browser:

```text
https://localhost:xxxx
```

---

## Sample Login

If demo data is inserted, use:

```text
User ID: E1001
Password: Test@123
```

---

## Suggested Repository Name

```text
secure-two-step-login-token-auth
```

Alternative names:

```text
two-step-login-session-token-auth
challenge-id-session-token-auth
employee-auth-challenge-session-token
aspnetcore-session-token-auth
```

---

## Screenshots

You can add screenshots later:

```text
Login Page
Password Page
Dashboard
Employee Salary Page
Employee Leave Details Page
```

Example:

```md
![Login Page](screenshots/login-page.png)
![Dashboard](screenshots/dashboard.png)
```

---

## Security Limitations

This project is designed for learning and demonstration purposes.

For production use, additional security controls are strongly recommended:

```text
Use ASP.NET Core Identity where appropriate
Use HTTPS only
Use secure password hashing
Use HttpOnly and Secure cookies
Implement MFA
Add login attempt limits
Add account lockout
Add audit logs
Use proper authorization roles
Avoid storing sensitive tokens in unsafe locations
```

---

## Main Learning Outcome

This project demonstrates that authentication should not end after password verification.

A secure system should continuously validate whether the current request belongs to a valid session.

```text
Challenge ID protects the login journey.
Session Token protects the authenticated journey.
```

The stronger question is not only:

```text
Is the password correct?
```

The better question is:

```text
Is this request coming from a valid, active, non-expired, non-revoked session?
```

---

## License

This project is open for learning, improvement, and demonstration purposes.

You may use, modify, and extend it for educational or portfolio work.
