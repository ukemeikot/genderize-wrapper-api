# Gender Classify API

A .NET 9 REST API that classifies names by gender using the [Genderize.io](https://genderize.io) API, processes the upstream response, and returns a structured result with confidence scoring, validation, error handling, Docker support, and production deployment on AWS.

---

## Live API

Base URL:

`https://api.hng.credianlab.xyz`

Useful links:

- API endpoint: `https://api.hng.credianlab.xyz/api/classify?name=James`
- Swagger UI: `https://api.hng.credianlab.xyz/index.html`

---

## Tech Stack

- .NET 9
- ASP.NET Core Web API
- Genderize.io
- Swashbuckle / Swagger
- xUnit
- Moq
- FluentAssertions
- Docker
- Caddy
- AWS EC2
- GitHub Actions

---

## What The API Does

The API exposes one endpoint:

`GET /api/classify?name={name}`

It:

1. Validates the `name` query parameter
2. Calls the Genderize API
3. Extracts the upstream fields
4. Renames `count` to `sample_size`
5. Computes `is_confident`
6. Generates `processed_at` dynamically in UTC ISO 8601 format
7. Returns a structured success or error response

---

## Success Response

```json
{
  "status": "success",
  "data": {
    "name": "James",
    "gender": "male",
    "probability": 0.99,
    "sample_size": 1234,
    "is_confident": true,
    "processed_at": "2026-04-10T14:00:00Z"
  }
}
```

---

## Processing Rules

The API applies the following processing rules to the raw Genderize response:

- `gender` is returned as received from Genderize
- `probability` is returned as received from Genderize
- `count` is renamed to `sample_size`
- `processed_at` is generated on every request using UTC time
- `is_confident` is computed with this rule:

```text
is_confident = probability >= 0.7 AND sample_size >= 100
```

Both conditions must be true. If either fails, `is_confident` is `false`.

---

## Validation Rules

The API validates the incoming query parameter before calling Genderize.

### `400 Bad Request`

Returned when:

- `name` is missing
- `name` is present but empty
- `name` is whitespace only

Example:

```json
{
  "status": "error",
  "message": "Name parameter is required"
}
```

### `422 Unprocessable Entity`

Returned when the query shape is invalid.

Because HTTP query strings arrive as strings, "name is not a string" is handled practically by rejecting malformed multi-value patterns such as:

- duplicate `name` values
- array-like query keys such as `name[]`

Example:

```json
{
  "status": "error",
  "message": "Name must be a valid string"
}
```

---

## Genderize Edge Cases

If Genderize returns either of the following:

- `gender: null`
- `count: 0`

the API returns:

```json
{
  "status": "error",
  "message": "No prediction available for the provided name"
}
```

Current implementation returns this as `404 Not Found`.

---

## Error Handling

All error responses follow the same structure:

```json
{
  "status": "error",
  "message": "<error message>"
}
```

### Error Status Codes

- `400 Bad Request` - missing or empty `name`
- `404 Not Found` - no prediction available
- `422 Unprocessable Entity` - invalid query shape
- `500 Internal Server Error` - unexpected server error
- `502 Bad Gateway` - Genderize API unavailable, timed out, or returned an unusable upstream response

### Upstream Failure Response

```json
{
  "status": "error",
  "message": "Unable to reach the gender prediction service"
}
```

### Generic Server Failure Response

```json
{
  "status": "error",
  "message": "An unexpected error occurred"
}
```

---

## CORS

The API explicitly sets:

```text
Access-Control-Allow-Origin: *
```

This is enabled so external graders and browser clients can reach the API successfully.

---

## Swagger Documentation

Swagger is enabled and served from the application root:

- Local: `http://localhost:5280/index.html`
- Live: `https://api.hng.credianlab.xyz/index.html`

XML documentation comments are enabled in the project so the endpoint and response metadata are described inside Swagger.

---

## Local Development

### Prerequisites

- .NET 9 SDK
- Git
- Docker Desktop or Docker Engine (optional)

### Run Locally With .NET

From the project root:

```bash
dotnet restore
dotnet run --project src/GenderClassifyApi
```

The local server starts from the launch profile on:

`http://localhost:5280`

Swagger:

`http://localhost:5280/index.html`

### Quick Local Tests

```bash
curl "http://localhost:5280/api/classify?name=James"
curl "http://localhost:5280/api/classify"
curl "http://localhost:5280/api/classify?name="
curl "http://localhost:5280/api/classify?name=Xqzptlw"
```

---

## Environment Configuration

The application uses:

- `appsettings.json`
- `appsettings.Development.json`
- `appsettings.Production.json`
- environment variables
- optional `.env` files loaded at startup

The Genderize base URL is intentionally not hardcoded in committed appsettings anymore.

### Environment Files Supported

The app can load:

- `.env`
- `.env.development`
- `.env.staging`
- `.env.production`

Only example files are committed:

- `.env.example`
- `.env.staging.example`
- `.env.production.example`

Real environment files are ignored by git.

### Example `.env.production`

```bash
ASPNETCORE_ENVIRONMENT=Production
ASPNETCORE_URLS=http://+:8080
Genderize__BaseUrl=https://api.genderize.io/
Genderize__TimeoutSeconds=3
Genderize__ApiKey=
```

### Config Values

- `Genderize__BaseUrl` - upstream Genderize API base URL
- `Genderize__TimeoutSeconds` - HTTP timeout for upstream requests
- `Genderize__ApiKey` - optional Genderize API key
- `ASPNETCORE_URLS` - Kestrel binding
- `ASPNETCORE_ENVIRONMENT` - app environment

At startup, options validation ensures:

- `Genderize:BaseUrl` is a valid absolute URI
- `Genderize:TimeoutSeconds` is greater than zero

---

## Docker

The project includes:

- `Dockerfile`
- `.dockerignore`
- `compose.yaml`

### Build And Run With Docker

```bash
docker compose --env-file .env.production up --build
```

### Production Compose Binding

The production `compose.yaml` binds the app to:

```text
127.0.0.1:8080
```

This is intentional because the public traffic is handled by Caddy, not by exposing the application container directly to the internet.

---

## AWS Deployment

The app is deployed on AWS EC2 and served through Caddy.

### Production Architecture

```text
Client
  -> HTTPS request
  -> Caddy reverse proxy
  -> ASP.NET Core container on 127.0.0.1:8080
  -> Genderize.io
```

### EC2 Notes

- OS: Ubuntu 24.04
- App path: `/home/ubuntu/apps/gender-classify-api`
- Environment file on server: `.env.production`

### Caddy Reverse Proxy

Caddy terminates HTTPS and proxies requests to the local container.

Example Caddyfile:

```caddy
api.hng.credianlab.xyz {
    encode gzip zstd
    reverse_proxy 127.0.0.1:8080
}
```

### Public Production URLs

- `https://api.hng.credianlab.xyz/index.html`
- `https://api.hng.credianlab.xyz/api/classify?name=James`

---

## GitHub Actions Auto-Deploy

The repository includes an automated deployment workflow:

`/.github/workflows/deploy.yml`

### What The Workflow Does

On every push to `main`, it:

1. Checks out the repository
2. Sets up .NET 9
3. Restores dependencies
4. Builds the solution
5. Runs the test suite
6. Connects to EC2 over SSH
7. Pulls the latest code on the server
8. Rebuilds and restarts the Dockerized application
9. Performs a health check against `http://127.0.0.1:8080/api/classify?name=James`

The workflow also supports manual triggering with `workflow_dispatch`.

### Required GitHub Secrets

- `EC2_HOST`
- `EC2_USER`
- `EC2_SSH_PRIVATE_KEY`
- `EC2_APP_PATH` (optional)

Recommended values:

- `EC2_HOST` = your EC2 public IP or DNS name
- `EC2_USER` = `ubuntu`
- `EC2_APP_PATH` = `/home/ubuntu/apps/gender-classify-api`

### Notes

- The workflow is written to redeploy directly on EC2.
- If GitHub-hosted Actions are unavailable because of account billing restrictions, deployment can still be done manually on the server with:

```bash
cd ~/apps/gender-classify-api
git pull origin main
docker compose --env-file .env.production up -d --build
```

---

## Testing

The project includes automated tests for:

- validation rules
- controller success and error paths
- confidence threshold boundaries
- Genderize zero-sample edge case
- upstream HTTP failure handling
- upstream timeout handling
- global middleware `500` response shaping

### Run Tests

```bash
dotnet test
```

Current automated coverage includes 19 passing tests.

---

## Example Requests

### Successful Classification

```bash
curl "https://api.hng.credianlab.xyz/api/classify?name=James"
```

### Missing Name

```bash
curl "https://api.hng.credianlab.xyz/api/classify"
```

### Empty Name

```bash
curl "https://api.hng.credianlab.xyz/api/classify?name="
```

### No Prediction

```bash
curl "https://api.hng.credianlab.xyz/api/classify?name=Xqzptlw"
```

---

## Project Structure

```text
gender-classify-api/
|-- .github/
|   `-- workflows/
|       `-- deploy.yml
|-- src/
|   `-- GenderClassifyApi/
|       |-- Controllers/
|       |   `-- ClassifyController.cs
|       |-- Middleware/
|       |   `-- GlobalExceptionMiddleware.cs
|       |-- Models/
|       |   |-- ClassifyResponse.cs
|       |   |-- ErrorResponse.cs
|       |   `-- GenderizeApiResponse.cs
|       |-- Properties/
|       |   `-- launchSettings.json
|       |-- Services/
|       |   |-- EnvironmentFileLoader.cs
|       |   |-- GenderizeOptions.cs
|       |   |-- GenderizeService.cs
|       |   |-- GenderizeUnavailableException.cs
|       |   `-- IGenderizeService.cs
|       |-- Validators/
|       |   `-- NameParameterValidator.cs
|       |-- Program.cs
|       |-- GenderClassifyApi.csproj
|       |-- GenderClassifyApi.http
|       |-- appsettings.json
|       |-- appsettings.Development.json
|       `-- appsettings.Production.json
|-- tests/
|   `-- GenderClassifyApi.Tests/
|       |-- Controllers/
|       |   `-- ClassifyControllerTests.cs
|       |-- Middleware/
|       |   `-- GlobalExceptionMiddlewareTests.cs
|       |-- Services/
|       |   `-- GenderizeServiceTests.cs
|       `-- Validators/
|           `-- NameParameterValidatorTests.cs
|-- .dockerignore
|-- .env.example
|-- .env.production.example
|-- .env.staging.example
|-- .gitignore
|-- Dockerfile
|-- GenderClassifyApi.sln
|-- compose.yaml
`-- README.md
```

---

## Submission Details

Suggested submission values:

- API base URL: `https://api.hng.credianlab.xyz`
- Swagger URL: `https://api.hng.credianlab.xyz/index.html`
- Stack: `.NET 9, ASP.NET Core, Docker, AWS EC2, Caddy`

Before submitting, verify:

- the domain is reachable externally
- the classify endpoint returns the expected structure
- missing and empty name cases return the correct error shape
- the GitHub repo is public
- the README is present and clear

---

## License

MIT
