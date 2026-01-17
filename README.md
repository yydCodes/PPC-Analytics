# Payroll Intelligence Assistant

A comprehensive web application for payroll analysis and intelligence gathering, with Docker support.

## Quick Start with Docker

### Prerequisites
- Docker Desktop installed and running
- (Optional) Docker Compose

### Option 1: Using Docker Compose (Recommended)

```bash
# Start the application
docker-compose up -d

# View logs
docker-compose logs -f

# Stop the application
docker-compose down
```

The application will be available at: **http://localhost:5000**

### Option 2: Using Docker CLI

```bash
# Build the image
docker build -t payroll-intelligence .

# Run the container
docker run -d \
  --name payroll-intelligence-app \
  -p 5000:80 \
  -e PayrollApi__ClientId="your-client-id" \
  -e PayrollApi__ClientSecret="your-client-secret" \
  -e PayrollApi__BaseUrl="https://your-api-domain.com" \
  payroll-intelligence

# View logs
docker logs -f payroll-intelligence-app

# Stop the container
docker stop payroll-intelligence-app
docker rm payroll-intelligence-app
```

### Environment Variables

Configure the application using environment variables:

| Variable | Description | Default |
|----------|-------------|---------|
| `PayrollApi__ClientId` | OAuth2 Client ID | (empty) |
| `PayrollApi__ClientSecret` | OAuth2 Client Secret | (empty) |
| `PayrollApi__BaseUrl` | Payroll API base URL | https://your-api-domain.com |
| `PayrollApi__TimeoutSeconds` | API timeout in seconds | 30 |

### Using a .env File

Create a `.env` file in the repository root directory:

```env
PAYROLL_API_CLIENT_ID=your-client-id or empty
PAYROLL_API_CLIENT_SECRET=your-client-secret or empty
PAYROLL_API_BASE_URL=https://your-api-domain.com
PAYROLL_API_TIMEOUT_SECONDS=30
```

Then run:
```bash
docker-compose up -d
```

---

## Features

### 1. Payroll Summary
- Compares previous and current payroll periods
- Identifies whether total payroll increased or decreased
- Calculates percentage change
- Identifies top 3 contributors to the change
- Provides business-friendly explanations

### 2. Detailed Changes
- Identifies meaningful differences between payroll datasets
- Focuses on changes that matter to payroll admins and finance
- Groups similar changes
- Explains why each change matters

### 3. Risk & Review
- Analyzes current payroll in context of historical patterns
- Identifies unusual, risky, or inconsistent changes
- Flags items for review (does not assume errors)
- Provides recommended actions

## Usage

### Running the Web Application

From the repository root directory, run:

```bash
dotnet run --project PayrollIntelligence.Web
```

Or navigate to the web project directory:

```bash
cd PayrollIntelligence.Web
dotnet run
```

The web application will start and be available at `http://localhost:5000` (or the port specified in `launchSettings.json`).

### Interactive Web Interface

The web application provides a complete user interface with interactive modal dialogs for each analysis feature.

#### **User Experience Flow**

1. **Visit Homepage**: Clean interface with three analysis feature cards
2. **Click Any Feature**: Automatically opens a modal dialog for period selection
3. **Configure Analysis**: Select periods and parameters (limited to January/February 2026)
4. **Run Analysis**: Submit form to see detailed results with visual feedback

#### **Modal Dialogs for Each Feature**

1. **Payroll Summary Modal**
   - Previous Period: Year (2026) + Month (January/February)
   - Current Period: Year (2026) + Month (January/February)
   - Pre-selects January → February comparison

2. **Detailed Changes Modal**
   - Same period selection as Comparison
   - Shows key differences between selected periods

3. **Risk & Review Modal**
   - Current Period: Year (2026) + Month (January/February)
   - Historical Months: Choose 1 or 2 months for comparison
   - Analyzes current period against historical patterns

#### **Key Features**

- **Automatic Modal Display**: Modals appear when clicking feature buttons
- **Smart Defaults**: Sensible pre-selected values for immediate analysis
- **Form Validation**: Required field validation prevents errors
- **Visual Results**: Analysis results show selected periods clearly
- **Change Settings**: "Change Periods/Settings" buttons allow re-configuration
- **API Fallback**: Automatically uses local JSON files when API unavailable

#### **API Configuration Settings**

Access the Settings page (`/Home/Settings`) to configure API credentials:

- **Client ID**: Your OAuth2 client identifier
- **Client Secret**: Your OAuth2 client secret (stored securely)
- **Authentication Test**: Validates credentials with the API
- **Status Indicator**: Shows authentication status with visual feedback

**API Endpoints Used:**
- `POST {baseUrl}/connect/token` - OAuth2 authentication (returns JWT access token)
- `GET {baseUrl}/Payrolls/GetPayroll/:year/:month` - Payroll data retrieval

**Token Response Format:**
```json
{
  "access_token": "eyJhbGciOiJSUzI1NiIs...",
  "token_type": "Bearer",
  "expires_in": 3600
}
```

The application automatically handles JWT token parsing, expiration tracking, and Bearer token authentication for subsequent API calls.

#### **Configuration Options**

Configure the API settings in `appsettings.json`:

```json
{
  "PayrollApi": {
    "ClientId": "your-client-id",
    "ClientSecret": "your-client-secret",
    "BaseUrl": "https://your-api-domain.com",
    "TimeoutSeconds": 30
  }
}
```

**Required Settings:**
- `BaseUrl`: The base URL of your payroll API (e.g., "https://your-api-domain.com")

**Optional Settings:**
- `ClientId`: Your OAuth2 client identifier
- `ClientSecret`: Your OAuth2 client secret
- `TimeoutSeconds`: HTTP client timeout in seconds (default: 30)

**Alternative Configuration:**
You can also use environment variables:
- `PAYROLL_API_CLIENT_ID`
- `PAYROLL_API_CLIENT_SECRET`
- `PAYROLL_API_BASE_URL`
- `PAYROLL_API_TIMEOUT_SECONDS`

**Note:** The `BaseUrl` setting is now fully configurable and no longer hardcoded in the application code.

## Project Structure

```
PPC-Analytics/
├── PayrollIntelligence.Core/       # Core business logic and services
│   ├── Services/
│   │   ├── PayrollComparisonService.cs
│   │   ├── PayrollDifferencesService.cs
│   │   └── PayrollAnomalyService.cs
│   ├── PayrollModels.cs
│   ├── PayrollApiService.cs
│   ├── PayrollAnalysisService.cs
│   └── ApiConfiguration.cs
├── PayrollIntelligence.Web/        # ASP.NET Core Web Application
│   ├── Controllers/
│   ├── Views/
│   └── wwwroot/
├── Dockerfile
├── docker-compose.yml
└── README.md
```

## Dependencies

- .NET 8.0
- System.Text.Json (included)

## Local Development (Without Docker)

### Prerequisites
- .NET 8.0 SDK installed

### Building

```bash
dotnet build
```

### Running the Web Application

```bash
cd PayrollIntelligence.Web
dotnet run --urls=http://localhost:5000
```

Or from the solution root:

```bash
dotnet run --project PayrollIntelligence.Web --urls=http://localhost:5000
```

The application will be available at: **http://localhost:5000**

## Analysis Output

When you use the web interface to analyze payroll data, you'll see comprehensive analysis including:

**Payroll Summary:**
- Summary of changes between periods
- Percentage change calculations
- Top contributors to the change

**Detailed Changes:**
- Key differences that matter to payroll admins
- Business impact explanations
- Affected areas (benefits, deductions, compensation)

**Risk & Review:**
- Anomalies with severity levels (low/medium/high)
- Reasons for flagging items
- Recommended actions for review

## Analysis Rules

All analyses follow these principles:
- **Business-friendly language**: Non-technical explanations
- **Conservative approach**: Avoid false alarms
- **Data-driven**: Do not guess missing data
- **Actionable insights**: Focus on meaningful changes

## Output Formats

Each analysis provides structured output suitable for management reporting and decision-making.

---

## Docker Troubleshooting

### Check Container Status
```bash
docker ps -a
docker logs payroll-intelligence-app
```

### Rebuild After Code Changes
```bash
docker-compose down
docker-compose build --no-cache
docker-compose up -d
```

### Access Container Shell
```bash
docker exec -it payroll-intelligence-app /bin/bash
```

### Check Health Status
```bash
docker inspect --format='{{.State.Health.Status}}' payroll-intelligence-app
```

### Common Issues

| Issue | Solution |
|-------|----------|
| Port 5000 already in use | Change port in docker-compose.yml: `"5001:80"` |
| Container exits immediately | Check logs: `docker logs payroll-intelligence-app` |
| API connection fails | Verify environment variables are set correctly |
| Health check failing | Ensure the application started successfully |

### Resource Limits (Optional)

Add to docker-compose.yml under the service:
```yaml
deploy:
  resources:
    limits:
      cpus: '1.0'
      memory: 512M
    reservations:
      cpus: '0.25'
      memory: 256M
```