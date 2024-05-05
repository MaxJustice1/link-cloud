﻿using LantanaGroup.Link.Account.Application.Models.User;

namespace LantanaGroup.Link.Account.Application.Queries.User.GetFacilityUsers
{
    public interface IGetFacilityUsers
    {
        Task<List<GroupedUserModel>> Execute(string facilityId, CancellationToken cancellationToken = default);
    }
}
