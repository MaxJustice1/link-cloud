﻿using LantanaGroup.Link.Account.Application.Models.Role;

namespace LantanaGroup.Link.Account.Application.Queries.Role
{
    public interface IGetRole
    {
        Task<LinkRoleModel> Execute(string roleId, CancellationToken cancellationToken = default);
    }
}
