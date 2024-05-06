﻿using LantanaGroup.Link.Account.Application.Models.Role;
using static LantanaGroup.Link.Account.Settings.AccountConstants;

namespace LantanaGroup.Link.Account.Infrastructure.Logging
{
    public static partial class RoleLogging
    {
        [LoggerMessage(
            AccountLoggingIds.RoleCreated,
            LogLevel.Information,
            "Role {roleName} was created by {requestor}.")]
        public static partial void LogRoleCreated(this ILogger logger, string roleName, string requestor, [LogProperties]LinkRoleModel role);

        [LoggerMessage(
            AccountLoggingIds.RoleCreationException,
            LogLevel.Error,
            "An exception occured while creating role {roleName}: {message}")]
        public static partial void LogRoleCreationException(this ILogger logger, string roleName, string message, [LogProperties] LinkRoleModel role);

        [LoggerMessage(
            AccountLoggingIds.RoleUpdated,
            LogLevel.Information,
            "Role {roleName} was updated by {requestor}.")]
        public static partial void LogRoleUpdated(this ILogger logger, string roleName, string requestor, [LogProperties] LinkRoleModel role);

        [LoggerMessage(
            AccountLoggingIds.RoleUpdateException,
            LogLevel.Error,
            "An exception occured while updating role {roleName}: {message}")]
        public static partial void LogRoleUpdateException(this ILogger logger, string roleName, string message, [LogProperties] LinkRoleModel role);

        [LoggerMessage(
            AccountLoggingIds.RoleDeleted,
            LogLevel.Information,
            "Role {roleName} was deleted by {requestor}.")]
        public static partial void LogRoleDeleted(this ILogger logger, string roleName, string requestor, [LogProperties] LinkRoleModel role);

        [LoggerMessage(
            AccountLoggingIds.RoleDeletionException,
            LogLevel.Error,
            "An exception occured while deleting role {roleName}: {message}")]
        public static partial void LogRoleDeletionException(this ILogger logger, string roleName, string message);

        [LoggerMessage(
            AccountLoggingIds.FindRole,
            LogLevel.Information,
            "A request to find role {roleName} by {requestor} was successful.")]
        public static partial void LogFindRole(this ILogger logger, string roleName, string requestor, [LogProperties] LinkRoleModel role);

        [LoggerMessage(
            AccountLoggingIds.FindRoleException,
            LogLevel.Error,
            "An exception occured while finding role {roleName}: {message}")]
        public static partial void LogFindRoleException(this ILogger logger, string roleName, string message);
    }
}
