# API Explorer Configuration

The Karamchari platform integrates **Scalar** as its native interactive API explorer in local development mode, replacing traditional Swagger UI.

---

## 1. Access Information
- **URL**: [https://localhost:60462/scalar](https://localhost:60462/scalar) (in development mode, navigating to root [https://localhost:60462/](https://localhost:60462/) will redirect here).
- **OpenAPI Schema Path**: `/openapi/v1.json`

---

## 2. Platform Configuration

### Programmatic Setup (`Program.cs`)
1. **OpenAPI generation**: Enabled via `.NET 10` native OpenAPI support (`Microsoft.AspNetCore.OpenApi`).
2. **Security Integration**: An `IOpenApiDocumentTransformer` injects a Bearer JWT authentication requirement globally.
3. **Scalar Middleware**: Registered via `Scalar.AspNetCore` using:
   ```csharp
   app.MapScalarApiReference(options =>
   {
       options.WithTitle("Karamchari API Explorer")
              .WithTheme(ScalarTheme.Purple);

       options.Authentication = new ScalarAuthenticationOptions
       {
           PreferredSecuritySchemes = new List<string> { "Bearer" }
       };
   });
   ```

---

## 3. How to Authenticate

To execute API calls directly from the Scalar UI:
1. Generate a developer JWT token using the identity endpoints (`POST /api/identity/login`).
2. Click on the **Authorize** or **Authentication** section in the Scalar sidebar.
3. Under the **Bearer** input, paste the returned access token.
4. Execute any protected route; the UI automatically attaches the `Authorization: Bearer <token>` header to the request.
