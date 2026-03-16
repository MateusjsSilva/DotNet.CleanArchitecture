using Microsoft.AspNetCore.Authorization;
using Microsoft.OpenApi;

namespace CleanArchitecture.WebAPI.Extensions;

public static class OpenApiExtensions
{
    /// <summary>
    /// Registers OpenAPI with a JWT Bearer security scheme.
    /// Scalar will show the "Bearer" auth panel so you can enter a token and test
    /// protected endpoints directly from the UI.
    /// Obtain a token via POST /api/v1/auth/login first.
    /// </summary>
    public static IServiceCollection AddOpenApiWithJwtSecurity(this IServiceCollection services)
    {
        services.AddOpenApi(options =>
        {
            options.AddDocumentTransformer((document, context, ct) =>
            {
                // 1. Register the Bearer scheme in document components
                document.Components ??= new OpenApiComponents();
                document.Components.SecuritySchemes ??= new Dictionary<string, IOpenApiSecurityScheme>();
                document.Components.SecuritySchemes["Bearer"] = new OpenApiSecurityScheme
                {
                    Type = SecuritySchemeType.Http,
                    Scheme = "bearer",
                    BearerFormat = "JWT",
                    Description = "JWT token. Obtain one via POST /api/v1/auth/login, then paste it here (without the 'Bearer ' prefix)."
                };

                // 2. Add a global security requirement — Scalar sends the token on every request.
                //    Anonymous endpoints are cleared of this requirement in the operation transformer.
                document.Security ??= [];
                document.Security.Add(new OpenApiSecurityRequirement
                {
                    [new OpenApiSecuritySchemeReference("Bearer", document)] = []
                });

                return Task.CompletedTask;
            });

            // 3. Remove the security requirement from public (non-[Authorize]) operations
            options.AddOperationTransformer((operation, context, ct) =>
            {
                var metadata = context.Description.ActionDescriptor.EndpointMetadata;

                if (!metadata.OfType<IAuthorizeData>().Any() ||
                     metadata.OfType<IAllowAnonymous>().Any())
                {
                    operation.Security?.Clear();
                }

                return Task.CompletedTask;
            });
        });

        return services;
    }
}
