﻿using LantanaGroup.Link.Audit.Application.Models;
using Microsoft.AspNetCore.Authentication.JwtBearer;
using System.IdentityModel.Tokens.Jwt;

namespace LantanaGroup.Link.Audit.Infrastructure.Extensions
{
    public static class ServiceCollectionExtensions
    {      

        public static IServiceCollection AddCorsService(this IServiceCollection services, IWebHostEnvironment env)
        {
            //TODO: Use env variable to control strictness of CORS policy
            services.AddCors(options =>
            {
                options.AddPolicy("CorsPolicy",
                    builder => builder
                        .AllowAnyMethod()
                        .AllowCredentials()
                        .SetIsOriginAllowed((host) => true) //lock this down, allows all atm
                        .AllowAnyHeader());
            });

            return services;
        }

        public static IServiceCollection AddAuthenticationService(this IServiceCollection services, IdentityProviderConfig idpConfig, IWebHostEnvironment env)
        {
            JwtSecurityTokenHandler.DefaultInboundClaimTypeMap.Clear();

            services.AddAuthentication(JwtBearerDefaults.AuthenticationScheme)
            .AddJwtBearer(options =>
            {
                options.Authority = idpConfig.Issuer; //gets the IDP metadata about endpoints and keys
                options.Audience = idpConfig.Audience;
                if (env.IsDevelopment())
                {
                    options.RequireHttpsMetadata = false;
                }
                options.TokenValidationParameters = new()
                {
                    NameClaimType = idpConfig.NameClaimType,
                    RoleClaimType = idpConfig.RoleClaimType,
                    ValidTypes = idpConfig.ValidTypes //avoid jwt confusion attacks (ie: circumvent token signature checking)
                };
            });

            return services;
        }

        public static IServiceCollection AddAuthorizationService(this IServiceCollection services)
        {
            //services.AddAuthorization(authorizationOptions =>
            //{
            //    authorizationOptions.AddPolicy("UserCanViewAuditLogs", AuthorizationPolicies.CanViewAuditLogs());
            //    authorizationOptions.AddPolicy("CanCreateAuditLogs", AuthorizationPolicies.CanCreateAuditLogs());

            //    authorizationOptions.AddPolicy("ClientApplicationCanRead", policyBuilder =>
            //    {
            //        policyBuilder.RequireScope("botwdemogatewayapi.read");
            //    });

            //    authorizationOptions.AddPolicy("ClientApplicationCanCreate", policyBuilder =>
            //    {
            //        policyBuilder.RequireScope("botwdemogatewayapi.write");
            //    });

            //    authorizationOptions.AddPolicy("ClientApplicationCanDelete", policyBuilder =>
            //    {
            //        policyBuilder.RequireScope("botwdemogatewayapi.delete");
            //    });

            //});

            return services;
        }
    }
}
