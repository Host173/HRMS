# Configuration Guide

## Connection String Setup

The application uses a connection string from configuration. The configuration is loaded in the following priority order:

1. **User Secrets** (highest priority - for development)
2. **Environment Variables**
3. **appsettings.Development.json** (for development environment)
4. **appsettings.json** (fallback)

### Option 1: Using User Secrets (Recommended for Development)

User secrets are stored outside the project directory and are not committed to source control.

**Initialize user secrets:**
```bash
dotnet user-secrets init
```

**Set the connection string:**
```bash
dotnet user-secrets set "ConnectionStrings:DefaultConnection" "Server=YOUR_SERVER\\SQLEXPRESS;Database=HRMS;Trusted_Connection=True;TrustServerCertificate=True;"
```

**View current secrets:**
```bash
dotnet user-secrets list
```

### Option 2: Using appsettings.Development.json

The `appsettings.Development.json` file contains a connection string for local development. Update the server name to match your SQL Server instance:

```json
{
  "ConnectionStrings": {
    "DefaultConnection": "Server=YOUR_SERVER\\SQLEXPRESS;Database=HRMS;Trusted_Connection=True;TrustServerCertificate=True;"
  }
}
```

**Note:** If `appsettings.Development.json` is in `.gitignore`, make sure to create it locally.

### Option 3: Using Environment Variables

Set the environment variable:
- **Windows PowerShell:**
  ```powershell
  $env:ConnectionStrings__DefaultConnection = "Server=YOUR_SERVER\SQLEXPRESS;Database=HRMS;Trusted_Connection=True;TrustServerCertificate=True;"
  ```

- **Windows CMD:**
  ```cmd
  set ConnectionStrings__DefaultConnection=Server=YOUR_SERVER\SQLEXPRESS;Database=HRMS;Trusted_Connection=True;TrustServerCertificate=True;
  ```

- **Linux/Mac:**
  ```bash
  export ConnectionStrings__DefaultConnection="Server=YOUR_SERVER\\SQLEXPRESS;Database=HRMS;Trusted_Connection=True;TrustServerCertificate=True;"
  ```

## Current Configuration

- **appsettings.json**: Contains empty connection string (should be set via user secrets or environment variables)
- **appsettings.Development.json**: Contains connection string for local development (Server: HESTIA\SQLEXPRESS)

## Security Best Practices

1. **Never commit sensitive connection strings** to source control
2. **Use User Secrets** for local development
3. **Use Environment Variables or Azure Key Vault** for production
4. **Keep appsettings.json** with empty or placeholder values
5. **Use appsettings.Development.json** only for local development (ensure it's in .gitignore if it contains sensitive data)

## Troubleshooting

If you get an error: `Connection string 'DefaultConnection' not found`

1. Check that you've set the connection string using one of the methods above
2. Verify the connection string format is correct
3. Ensure SQL Server is running and accessible
4. Check that the database name matches your actual database

