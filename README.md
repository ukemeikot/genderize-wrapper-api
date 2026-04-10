# Gender Classify API

A .NET 9 REST API that classifies names by gender using the [Genderize.io](https://genderize.io) API, with response shaping, confidence scoring, input validation, Docker support, and environment-based deployment configuration.

---

## Tech Stack

- **.NET 9** - Web API framework
- **Genderize.io** - External gender prediction API
- **Swashbuckle** - Swagger/OpenAPI documentation
- **xUnit** - Unit testing
- **Moq** - Mocking library
- **FluentAssertions** - Expressive test assertions
- **Docker** - Containerized local and server deployment

---

## Features

- `GET /api/classify?name=<name>` endpoint
- Structured success and error responses
- Validation for missing, empty, duplicate, and array-like `name` query values
- `sample_size` mapping from Genderize `count`
- `is_confident` calculation using the assessment rule
- `processed_at` generated dynamically in UTC ISO 8601 format
- Global exception middleware for `500` and `502` responses
- CORS configured with `Access-Control-Allow-Origin: *`
- Swagger UI available at `baseurl/index.html`
- Docker Compose setup for environment-based deployment

---

## Local Setup

### Prerequisites

- [.NET 9 SDK](https://dotnet.microsoft.com/en-us/download/dotnet/9.0)
- Docker Desktop or Docker Engine (optional, for container runs)
- Git

### Run with .NET

```bash
# Clone the repository
git clone https://github.com/YOUR_USERNAME/gender-classify-api.git
cd gender-classify-api

# Restore dependencies
dotnet restore

# Create a local development env file
cp .env.example .env.development

# Run the project
dotnet run --project src/GenderClassifyApi
```

The API will start on:
`http://localhost:5280`

Swagger UI will be available at:
`http://localhost:5280/index.html`

### Run with Docker

```bash
docker compose --env-file .env.production up --build
```

The containerized API will be available at:
`http://localhost:8080/index.html`

---

## Environment Configuration

The project supports configuration through `appsettings.*.json` and environment variables. The Genderize base URL is intentionally not committed in the appsettings files anymore.

The app auto-loads:

- `.env`
- `.env.development`
- `.env.staging`
- `.env.production`

Environment files are ignored by git. Example templates are committed:

- `.env.example`
- `.env.staging.example`
- `.env.production.example`

### Example `.env.development`

```bash
ASPNETCORE_ENVIRONMENT=Development
ASPNETCORE_URLS=http://+:8080
Genderize__BaseUrl=https://api.genderize.io/
Genderize__TimeoutSeconds=3
Genderize__ApiKey=
```

### Example `.env.staging`

```bash
ASPNETCORE_ENVIRONMENT=Staging
ASPNETCORE_URLS=http://+:8080
Genderize__BaseUrl=https://api.genderize.io/
Genderize__TimeoutSeconds=3
Genderize__ApiKey=
```

### Example `.env.production`

```bash
ASPNETCORE_ENVIRONMENT=Production
ASPNETCORE_URLS=http://+:8080
Genderize__BaseUrl=https://api.genderize.io/
Genderize__TimeoutSeconds=3
Genderize__ApiKey=
```

Notes:

- `Genderize__ApiKey` is optional
- `appsettings.Development.json` and `appsettings.Production.json` now keep only safe non-secret defaults
- `.env.staging` and `.env.production` should stay uncommitted
- for Docker deployment, pass the appropriate file with `docker compose --env-file <file> up --build`

---

## API Documentation

### `GET /api/classify`

Classifies a name by gender using the Genderize.io API.

#### Query Parameters

| Parameter | Type | Required | Description |
|-----------|------|----------|-------------|
| `name` | string | Yes | The name to classify |

#### Success Response `200 OK`

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

#### Error Responses

**400 Bad Request** - Missing or empty name parameter

```json
{
  "status": "error",
  "message": "Name parameter is required"
}
```

**422 Unprocessable Entity** - Invalid name parameter type

```json
{
  "status": "error",
  "message": "Name must be a valid string"
}
```

**404 Not Found** - No prediction available

```json
{
  "status": "error",
  "message": "No prediction available for the provided name"
}
```

**500 Internal Server Error**

```json
{
  "status": "error",
  "message": "An unexpected error occurred"
}
```

**502 Bad Gateway** - Genderize.io API is unreachable

```json
{
  "status": "error",
  "message": "Unable to reach the gender prediction service"
}
```

#### Example Requests

```bash
# Happy path
curl "http://localhost:5280/api/classify?name=James"

# Missing name
curl "http://localhost:5280/api/classify"

# Empty name
curl "http://localhost:5280/api/classify?name="

# Unknown name
curl "http://localhost:5280/api/classify?name=Xqzptlw"
```

---

## Confidence Scoring

`is_confident` is calculated as:

```text
is_confident = probability >= 0.7 AND sample_size >= 100
```

Both conditions must be true. If either condition fails, `is_confident` is `false`.

---

## Running Tests

```bash
dotnet test
```

---

## Project Structure

```text
gender-classify-api/
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
|       |-- Services/
|       |   |-- EnvironmentFileLoader.cs
|       |   |-- GenderizeOptions.cs
|       |   |-- GenderizeService.cs
|       |   |-- GenderizeUnavailableException.cs
|       |   `-- IGenderizeService.cs
|       |-- Validators/
|       |   `-- NameParameterValidator.cs
|       |-- Program.cs
|       |-- appsettings.json
|       |-- appsettings.Development.json
|       `-- appsettings.Production.json
|-- tests/
|   `-- GenderClassifyApi.Tests/
|       |-- Controllers/
|       |   `-- ClassifyControllerTests.cs
|       |-- Services/
|       |   `-- GenderizeServiceTests.cs
|       `-- Validators/
|           `-- NameParameterValidatorTests.cs
|-- .dockerignore
|-- .env.example
|-- .env.production.example
|-- .env.staging.example
|-- compose.yaml
|-- Dockerfile
`-- README.md
```

---

## Deployment Notes

For AWS EC2 deployment with your own domain:

- deploy the Dockerized app to an EC2 instance
- create a real `.env.production` on the server
- run `docker compose --env-file .env.production up -d --build`
- point your domain `A` record to the instance Elastic IP
- place the app behind Nginx or Caddy for `80/443` handling and TLS

Once deployed, replace this placeholder:

Base URL:
`https://api.yourdomain.com`

Swagger:
`https://api.yourdomain.com/index.html`

Example:
`https://api.yourdomain.com/api/classify?name=James`

---

## License

MIT
