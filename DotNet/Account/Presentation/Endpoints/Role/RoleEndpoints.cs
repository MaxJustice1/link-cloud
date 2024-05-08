﻿using LantanaGroup.Link.Account.Application.Interfaces.Presentation;
using LantanaGroup.Link.Account.Application.Models.Role;
using LantanaGroup.Link.Account.Infrastructure.Logging;
using LantanaGroup.Link.Account.Presentation.Endpoints.Role.Handlers;
using LantanaGroup.Link.Shared.Application.Filters;
using LantanaGroup.Link.Shared.Application.Models.Responses;
using Microsoft.OpenApi.Models;

namespace LantanaGroup.Link.Account.Presentation.Endpoints.Role
{
    public class RoleEndpoints : IApi
    {
        private readonly ILogger<RoleEndpoints> _logger;

        public RoleEndpoints(ILogger<RoleEndpoints> logger)
        {
            _logger = logger ?? throw new ArgumentNullException(nameof(logger));
        }

        public void RegisterEndpoints(WebApplication app)
        {
            var roleEndpoints = app.MapGroup("/api/account/")
                .WithOpenApi(x => new OpenApiOperation(x)
                {
                    Tags = new List<OpenApiTag> { new() { Name = "Role" } }
                });

            #region Queries

            roleEndpoints.MapGet("/role/{id}", GetRoleById.Handle)
                .Produces(StatusCodes.Status200OK)
                .Produces(StatusCodes.Status400BadRequest)
                .Produces(StatusCodes.Status401Unauthorized)
                .Produces(StatusCodes.Status403Forbidden)
                .Produces(StatusCodes.Status404NotFound)
                .ProducesProblem(StatusCodes.Status500InternalServerError)
                .WithOpenApi(x => new OpenApiOperation(x)
                {
                    Summary = "Get role by id",
                    Description = "Retrieves information about a role with the id provided"
                });
            
            roleEndpoints.MapGet("/role/name/{name}", GetRoleByName.Handle)
                .Produces(StatusCodes.Status200OK)
                .Produces(StatusCodes.Status400BadRequest)
                .Produces(StatusCodes.Status401Unauthorized)
                .Produces(StatusCodes.Status403Forbidden)
                .Produces(StatusCodes.Status404NotFound)
                .ProducesProblem(StatusCodes.Status500InternalServerError)
                .WithOpenApi(x => new OpenApiOperation(x)
                {
                    Summary = "Get role by name",
                    Description = "Retrieves information about a role with the name provided"
                });
            
            roleEndpoints.MapGet("/role", GetRoleList.Handle)
                .Produces(StatusCodes.Status200OK)
                .Produces(StatusCodes.Status400BadRequest)
                .Produces(StatusCodes.Status401Unauthorized)
                .Produces(StatusCodes.Status403Forbidden)
                .Produces(StatusCodes.Status404NotFound)
                .ProducesProblem(StatusCodes.Status500InternalServerError)
                .WithOpenApi(x => new OpenApiOperation(x)
                {
                    Summary = "Get all roles",
                    Description = "Retrieves all roles"
                });


            #endregion

            #region Commands

            roleEndpoints.MapPost("/role", CreateNewRole.Handle)
                .AddEndpointFilter<ValidationFilter<LinkRoleModel>>()
                .Produces(StatusCodes.Status201Created)
                .Produces<ValidationFailureResponse>(StatusCodes.Status400BadRequest)
                .Produces(StatusCodes.Status401Unauthorized)
                .Produces(StatusCodes.Status403Forbidden)
                .ProducesProblem(StatusCodes.Status500InternalServerError)
                .WithOpenApi(x => new OpenApiOperation(x)
                {
                    Summary = "Create user",
                    Description = "Creates a new user"
                });

            roleEndpoints.MapPut("/role/{id}", UpdateExistingRole.Handle)
                .AddEndpointFilter<ValidationFilter<LinkRoleModel>>()
                .Produces(StatusCodes.Status200OK)
                .Produces<ValidationFailureResponse>(StatusCodes.Status400BadRequest)
                .Produces(StatusCodes.Status401Unauthorized)
                .Produces(StatusCodes.Status403Forbidden)
                .Produces(StatusCodes.Status404NotFound)
                .ProducesProblem(StatusCodes.Status500InternalServerError)
                .WithOpenApi(x => new OpenApiOperation(x)
                {
                    Summary = "Update role",
                    Description = "Updates an existing role"
                });

            roleEndpoints.MapDelete("/role/{id}", DeleteExistingRole.Handle)
                .Produces(StatusCodes.Status204NoContent)
                .Produces(StatusCodes.Status400BadRequest)
                .Produces(StatusCodes.Status401Unauthorized)
                .Produces(StatusCodes.Status403Forbidden)
                .Produces(StatusCodes.Status404NotFound)
                .ProducesProblem(StatusCodes.Status500InternalServerError)
                .WithOpenApi(x => new OpenApiOperation(x)
                {
                    Summary = "Delete role",
                    Description = "Deletes an existing role"
                });
         
            roleEndpoints.MapPut("/role/{id}/claims", UpdateRoleClaims.Handle)               
                .Produces(StatusCodes.Status200OK)
                .Produces<ValidationFailureResponse>(StatusCodes.Status400BadRequest)
                .Produces(StatusCodes.Status401Unauthorized)
                .Produces(StatusCodes.Status403Forbidden)
                .Produces(StatusCodes.Status404NotFound)
                .ProducesProblem(StatusCodes.Status500InternalServerError)
                .WithOpenApi(x => new OpenApiOperation(x)
                {
                    Summary = "Update role claims",
                    Description = "Updates the claims for an existing role"
                });

            #endregion

            _logger.LogApiRegistration(nameof(RoleEndpoints));
        }
    }
}
