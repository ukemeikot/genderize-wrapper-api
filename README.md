# Gender Classify API

A .NET 8 REST API that classifies names by gender using the [Genderize.io](https://genderize.io) API, with data enrichment, confidence scoring, and input validation.

---

## Tech Stack

- **.NET 8** — Web API framework
- **Genderize.io** — External gender prediction API
- **Swashbuckle** — Swagger/OpenAPI documentation
- **xUnit** — Unit testing
- **Moq** — Mocking library
- **FluentAssertions** — Test assertions

---

## Local Setup

### Prerequisites

- [.NET 8 SDK](https://dotnet.microsoft.com/en-us/download/dotnet/8.0)
- Git

### Steps

```bash
# Clone the repository
git clone https://github.com/YOUR_USERNAME/gender-classify-api.git
cd gender-classify-api

# Restore dependencies
dotnet restore

# Run the project
dotnet run --project src/GenderClassifyApi
```

The API will start on:
http://localhost:5280

Swagger UI will be available at:
http://localhost:5280/swagger

---

## API Documentation

### `GET /api/classify`

Classifies a name by gender using the Genderize.io API.

#### Query Parameters

| Parameter | Type   | Required | Description        |
|-----------|--------|----------|--------------------|
| `name`    | string | Yes      | The name to classify |

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

**400 Bad Request** — Missing or empty name parameter
```json
{
  "status": "error",
  "message": "Name parameter is required"
}
```

**422 Unprocessable Entity** — Invalid name parameter type
```json
{
  "status": "error",
  "message": "Name must be a valid string"
}
```

**404 / No Prediction Available**
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

**502 Bad Gateway** — Genderize.io API is unreachable
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
is_confident = probability >= 0.7 AND sample_size >= 100

Both conditions must be true. If either fails, `is_confident` is `false`.

---

## Running Tests

```bash
dotnet test
```

---

## Project Structure

```text
gender-classify-api/
├── src/
│   └── GenderClassifyApi/
│       ├── Controllers/        # API endpoint
│       ├── Services/           # Genderize.io HTTP client
│       ├── Models/             # Request/response models
│       ├── Middleware/         # Global exception handler
│       └── Validators/         # Input validation logic
└── tests/
    └── GenderClassifyApi.Tests/
        ├── Controllers/
        ├── Services/
        └── Validators/
```

---

## Live API

Base URL:
http://YOUR_EC2_IP

Example:
http://YOUR_EC2_IP/api/classify?name=James

> This will be updated after deployment.

---

## License

MIT
