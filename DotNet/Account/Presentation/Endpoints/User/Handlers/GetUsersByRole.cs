﻿using LantanaGroup.Link.Account.Application.Queries.User;
using LantanaGroup.Link.Account.Infrastructure.Logging;

namespace LantanaGroup.Link.Account.Presentation.Endpoints.User.Handlers
{
    public static class GetUsersByRole
    {
        public static async Task<IResult> Handle(HttpContext context, string id, ILogger logger, IGetRoleUsers query)
        {
            if (string.IsNullOrEmpty(id))
            {
                return Results.BadRequest("A role is required");
            }

            var users = await query.Execute(id, context.RequestAborted);

            logger.LogFindUsers(context.User.Claims.First(c => c.Type == "sub").Value ?? "Uknown");

            return Results.Ok(users);
        }
    }
}
