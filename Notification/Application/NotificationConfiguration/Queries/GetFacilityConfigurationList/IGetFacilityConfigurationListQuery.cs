﻿using LantanaGroup.Link.Notification.Application.Models;

namespace LantanaGroup.Link.Notification.Application.NotificationConfiguration.Queries
{
    public interface IGetFacilityConfigurationListQuery
    {
        Task<PagedNotificationConfigurationModel> Execute(string? searchText, string? filterFacilityBy, string? sortBy, int pageSize, int pageNumber);
    }
}
