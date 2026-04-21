# SimpleApiPlaywright

[![NuGet](https://img.shields.io/nuget/v/SimpleApiPlaywright.svg)](https://www.nuget.org/packages/SimpleApiPlaywright/)[![npm](https://img.shields.io/npm/v/simple-api-playwright.svg)](https://www.npmjs.com/package/simple-api-playwright)
[![GitHub](https://img.shields.io/badge/github-repo-black.svg)](https://github.com/notNullThen/simple-api-playwright-nuget-dotnet)

Do API requests like this:

```csharp
var response = await Api.CreateUserAsync(user).RequestAsync();
```

or API waits like this:

```csharp
var userResponseTask = Api.Users.GetUser().WaitAsync();
await Task.WhenAll(
    Page.LoginButton.ClickAsync(),
    userResponseTask
);
userResponse...
```

A lightweight wrapper for **Playwright .NET** that unifies API requests and UI network assertions. Designed for high-performance automation suites requiring parallel execution and clean architecture.

## 🚀 Key Features

- **Unified Interface**: Use the same endpoint definitions for standalone API tests (`RequestAsync`) and UI network synchronization (`WaitAsync`).
- **Parallel Safe**: Built-in `AsyncLocal<ApiContext>` management prevents state leakage across concurrent tests.
- **Smart Assertions**: Automatic status code validation with descriptive error messages including method and route details.
- **Tracing Integration**: Automatically groups requests in Steps (Playwright Trace Groups) for better debugging.
- **Global Configuration**: Set base URLs and timeouts once at the suite level.

## 📖 Usage Modes

The library is flexible and adapts to your testing context:

1.  **API Only**: For standalone API testing. Requires only `IAPIRequestContext`.
2.  **UI Integrated**: For waiting on network calls triggered by UI actions. Requires `IPage`.

## 📦 Installation

```bash
dotnet add package SimpleApiPlaywright
```

## 🛠️ Quick Start

### 1. Define your Endpoint
```csharp
public class AuthApi : ApiEndpointBase
{
    public ApiAction<LoginResponse> PostLogin(LoginPayload? payload = null) =>
        Action<LoginResponse>(new() { Url = "login", Method = ApiHttpMethod.POST, Body = payload } );
}
```

### 2. Global & Test Setup
Before executing requests, you must initialize the global configuration and set the current test context (usually in a test hook):

```csharp
// Once per suite:
ApiClient.SetInitialConfig(apiWaitTimeout: 5000, expectedStatusCodes: [200, 201], baseUrl: "https://api.example.com");

// Once per test (mandatory):
ApiClient.SetContext(new ApiContext(requestContext, page)); 
```

### 3. Standalone API Call
*Requires `IAPIRequestContext` context*

```csharp
var response = await Api.Auth.PostLogin(credentials).RequestAsync();
Assert.Equal(200, response.Response.Status);
```

### 4. UI Synchronization (XHR/Fetch)
*Requires `IPage` context*

```csharp
// Define the task to wait for the network response
var loginTask = Api.Auth.PostLogin(credentials).WaitAsync();

// Trigger the UI action
await Page.GetByRole(AriaRole.Button, new() { Name = "Login" }).ClickAsync();

// Await the network response
var response = await loginTask;
```

## 🏗️ Architecture

SimpleApiPlaywright is built with **SDET efficiency** in mind:
- **`ApiClient`**: Core engine handling the request/wait logic.
- **`ApiContext`**: Thread-safe container for `IAPIRequestContext` and `IPage`.
- **`TokenStorage`**: Centralized, parallel-safe authentication management.

---

*Part of a larger portfolio demonstrating advanced automation patterns. See the [Implementation Demo](https://github.com/notNullThen/owasp-playwright-api-csharp-docker).*
