﻿using LantanaGroup.Link.Account.Domain.Entities;

namespace LantanaGroup.Link.Account.Application.Queries.User
{
    public interface IGetLinkUserEntity
    {
        Task<LinkUser> Execute(string id, CancellationToken cancellationToken = default);
    }
}
