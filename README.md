# AppHttpClient

A simple and extensible HTTP client wrapper for .NET applications.

## Features

- Unified `Result<T>` response model
- Supports:
  - JSON requests
  - `multipart/form-data` (file upload)
  - `application/x-www-form-urlencoded`
- Built-in error handling
- `HttpClientFactory` integration
- Generates `curl` commands for debugging

## Usage

### Registration

```csharp
services.AddHttpClient();
services.AddScoped<IAppHttpClient, AppHttpClient>();
