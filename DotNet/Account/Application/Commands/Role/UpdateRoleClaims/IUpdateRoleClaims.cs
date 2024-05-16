﻿using System.Security.Claims;

namespace LantanaGroup.Link.Account.Application.Commands.Role
{
    public interface IUpdateRoleClaims
    {
        Task<bool> Execute(ClaimsPrincipal? requestor, string roleId, List<string> claims, CancellationToken cancellationToken = default);
    }
}
