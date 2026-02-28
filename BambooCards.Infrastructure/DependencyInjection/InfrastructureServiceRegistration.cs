using BambooCards.Application.Common;
using BambooCards.Application.InterfaceServiceClients;
using BambooCards.Infrastructure.ServiceClients;
using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.RateLimiting;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Options;
using Microsoft.IdentityModel.Tokens;
using System;
using System.Security.Claims;
using System.Text.Json;

namespace BambooCards.Infrastructure.DependencyInjection
{
    public static class InfrastructureServiceRegistration
    {
        public static IServiceCollection AddInfrastructureServices(this IServiceCollection services, IConfiguration configuration)
        {
            services.Configure<FrankfuterService>(configuration.GetSection("FrankfurterService"));

            var keycloakOptions = configuration.GetSection("Keycloak").Get<KeycloakSettings>();
            var redis = configuration.GetConnectionString("redis");
            var keycloakUri =configuration["services:keycloak:http:0"];
            // 1. Register the Typed Client
            services.AddHttpClient<IFrankfuterServiceClients, FrankfurterServiceClients>((serviceProvider, client) =>
            {
                // Get the bound options directly here
                var settings = serviceProvider.GetRequiredService<IOptions<FrankfuterService>>().Value;

                if (string.IsNullOrEmpty(settings.BaseUri))
                    throw new Exception("Frankfuter BaseUri is null. Check appsettings.json section name.");

                client.BaseAddress = new Uri(settings.BaseUri);
            })
            // 2. Add the Standard Resilience Pipeline
            // This includes: Retry (Exponential Backoff) + Circuit Breaker + Timeout + Rate Limiter
            .AddStandardResilienceHandler(options =>
            {
                // Configure Exponential Backoff
                options.Retry.MaxRetryAttempts = 3;
                options.Retry.BackoffType = Polly.DelayBackoffType.Exponential;
                options.Retry.UseJitter = true; // Prevents "Thundering Herd" effect

                // Configure Circuit Breaker
                options.CircuitBreaker.FailureRatio = 0.5; // Break if 50% of requests fail
                options.CircuitBreaker.SamplingDuration = TimeSpan.FromSeconds(30);
                options.CircuitBreaker.BreakDuration = TimeSpan.FromSeconds(15);
            });
            services.AddStackExchangeRedisCache(options =>
            {
                options.Configuration = redis;
                options.InstanceName = "SampleInstance_"; // Optional prefix for keys
            });
            // 1. Prevent .NET from mapping "sub" to "http://schemas.xmlsoap.org/ws/2005/05/identity/claims/nameidentifier"
            System.IdentityModel.Tokens.Jwt.JwtSecurityTokenHandler.DefaultInboundClaimTypeMap.Clear();

            services.AddAuthentication(JwtBearerDefaults.AuthenticationScheme)
                .AddJwtBearer(options =>
                {
                    options.Authority = keycloakUri + keycloakOptions!.Authority;
                    options.Audience = keycloakOptions.Audience;
                    options.RequireHttpsMetadata = false; // Set to true in production

                    options.TokenValidationParameters = new TokenValidationParameters
                    {
                        ValidateIssuer = false,
                        ValidIssuer = keycloakUri + keycloakOptions.Authority,
                        ValidateAudience = true,
                        ValidAudience = keycloakOptions.Audience,
                        // Ensure .NET looks for the "role" claim we will create manually below
                        // This tells .NET to look for roles in the "role" claim
                        RoleClaimType = "role",
                        NameClaimType = "preferred_username"
                    };

                    options.Events = new JwtBearerEvents
                    {
                        OnTokenValidated = context =>
                        {
                            var claimsIdentity = (ClaimsIdentity)context.Principal!.Identity!;

                            // Flatten realm_access.roles
                            if (context.Principal.HasClaim(c => c.Type == "realm_access"))
                            {
                                var realmAccessClaim = context.Principal.FindFirst("realm_access");
                                using var jsonDoc = JsonDocument.Parse(realmAccessClaim!.Value);
                                if (jsonDoc.RootElement.TryGetProperty("roles", out var roles))
                                {
                                    foreach (var role in roles.EnumerateArray())
                                    {
                                        claimsIdentity.AddClaim(new Claim("role", role.GetString()!));
                                    }
                                }
                            }
                            return Task.CompletedTask;
                        }
                    };
                });

            services.AddAuthorization(options =>
            {
                // Define a policy to make it easier to debug
                options.AddPolicy("BambooUserPolicy", policy => policy.RequireRole("bamboo_user"));
            });
            services.AddRateLimiter(options =>
            {
                options.RejectionStatusCode = StatusCodes.Status429TooManyRequests;

                // Add the policy
                options.AddFixedWindowLimiter(policyName: "fixed", opt =>
                {
                    opt.PermitLimit = 5;
                    opt.Window = TimeSpan.FromMinutes(10);
                    opt.QueueLimit = 0;
                });

                // SET AS DEFAULT (Temporary test)
            });
            return services;
        }
    }
}
