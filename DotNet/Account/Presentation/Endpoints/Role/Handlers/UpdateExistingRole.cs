﻿using LantanaGroup.Link.Account.Application.Commands.Role;
using LantanaGroup.Link.Account.Application.Commands.Role.UpdateRole;
using LantanaGroup.Link.Account.Application.Interfaces.Persistence;
using LantanaGroup.Link.Account.Application.Models.Role;
using LantanaGroup.Link.Account.Infrastructure.Logging;

namespace LantanaGroup.Link.Account.Presentation.Endpoints.Role.Handlers
{
    public static class UpdateExistingRole
    {
        public static async Task<IResult> Handle(HttpContext context, string id, LinkRoleModel model,
            ILogger logger, IRoleRepository roleRepository, IUpdateRole command, ICreateRole createCommand)
        {
            if (string.IsNullOrEmpty(id))
            {
                return Results.BadRequest("A role id is required");
            }

            if(id != model.Id)
            {
                return Results.BadRequest("The role id in the model does not match the id in the request");
            }                     

            var requestor = context.User;
            var existingRole = await roleRepository.GetRoleAsync(id, cancellationToken: context.RequestAborted);

            if (existingRole is null)
            {
                var createdRole = await createCommand.Execute(requestor, model, context.RequestAborted);

                if (createdRole is null)
                {
                    return Results.Problem("Failed to create role");
                }

                logger.LogRoleCreated(createdRole.Name, requestor.Claims.First(c => c.Type == "sub").Value ?? "Uknown", createdRole);

                //build resource uri
                var uriBuilder = new UriBuilder
                {
                    Scheme = context.Request.Scheme,
                    Host = context.Request.Host.Host,
                    Path = $"api/account/role/{createdRole.Id}"
                };

                if (context.Request.Host.Port.HasValue)
                {
                    uriBuilder.Port = context.Request.Host.Port.Value;
                }

                return Results.Created(uriBuilder.ToString(), createdRole);
            }

            //update existing role
            var updateResult = await command.Execute(requestor, model, context.RequestAborted);
            if (!updateResult)
            {
                return Results.Problem("Failed to update role");
            }

            logger.LogRoleUpdated(model.Name, requestor.Claims.First(c => c.Type == "sub").Value ?? "Uknown", model);

            return Results.NoContent();
        }
    }
}
