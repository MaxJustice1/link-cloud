﻿using LantanaGroup.Link.Account.Application.Interfaces.Factories.User;
using LantanaGroup.Link.Account.Application.Models.User;
using LantanaGroup.Link.Shared.Application.Enums;

namespace LantanaGroup.Link.Account.Application.Factories.User
{
    public class UserSearchFilterRecordFactory : IUserSearchFilterRecordFactory
    {
        public UserSearchFilterRecord Create(string? searchText, string? filterFacilityBy, string? filterRoleBy, string? filterClaimBy, bool includeDeactivatedUsers, bool includeDeletedUsers, string? sortBy, SortOrder sortOrder, int pageSize, int pageNumber)
        {
            return new UserSearchFilterRecord
            {
                SearchText = searchText,
                FilterFacilityBy = filterFacilityBy,
                FilterRoleBy = filterRoleBy,
                FilterClaimBy = filterClaimBy,
                IncludeDeactivatedUsers = includeDeactivatedUsers,
                IncludeDeletedUsers = includeDeletedUsers,
                SortBy = sortBy,
                SortOrder = sortOrder,
                PageSize = pageSize,
                PageNumber = pageNumber
            };
        }
    }
}
