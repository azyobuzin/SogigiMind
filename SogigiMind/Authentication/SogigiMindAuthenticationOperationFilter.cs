using System;
using System.Collections.Generic;
using System.Reflection;
using Microsoft.AspNetCore.Authorization;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.OpenApi.Models;
using Swashbuckle.AspNetCore.SwaggerGen;

namespace SogigiMind.Authentication
{
    public class SogigiMindAuthenticationOperationFilter : IOperationFilter
    {
        private readonly string _securityDefinitionName;

        public SogigiMindAuthenticationOperationFilter(string securityDefinitionName)
        {
            this._securityDefinitionName = securityDefinitionName ?? throw new ArgumentNullException(nameof(securityDefinitionName));
        }

        public void Apply(OpenApiOperation operation, OperationFilterContext context)
        {
            var authorizeAttribute = context.MethodInfo.GetCustomAttribute<AuthorizeAttribute>()
                ?? context.MethodInfo.DeclaringType!.GetCustomAttribute<AuthorizeAttribute>();
            if (authorizeAttribute == null) return;

            var isEndUserPolicy = authorizeAttribute.Policy == EndUserAuthorizationPolicy.PolicyName;
            var description = isEndUserPolicy
                ? "Forbidden. Your access token is invalid or is not an end user token."
                : $"Forbidden. Your access token is invalid or is not granted role `{authorizeAttribute.Roles}` to.";

            var securityScheme = new OpenApiSecurityScheme()
            {
                Reference = new OpenApiReference()
                {
                    Type = ReferenceType.SecurityScheme,
                    Id = this._securityDefinitionName,
                }
            };
            operation.Security = new List<OpenApiSecurityRequirement>()
            {
                new OpenApiSecurityRequirement()
                {
                    { securityScheme, Array.Empty<string>() }
                }
            };

            operation.Responses.Add("401", new OpenApiResponse { Description = "Unauthorized" });
            operation.Responses.Add("403", new OpenApiResponse { Description = description });
        }
    }

    public static class SogigiMindAuthenticationOperationFilterExtensions
    {
        public static void AddSogigiMindAuthenticationOperationFilter(this SwaggerGenOptions options)
        {
            const string securityDefinitionName = "access_token";

            options.AddSecurityDefinition(securityDefinitionName, new OpenApiSecurityScheme()
            {
                Type = SecuritySchemeType.Http,
                Scheme = "bearer",
            });

            options.OperationFilter<SogigiMindAuthenticationOperationFilter>(securityDefinitionName);
        }
    }
}
