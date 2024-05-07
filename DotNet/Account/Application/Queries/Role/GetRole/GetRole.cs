﻿using LantanaGroup.Link.Account.Application.Interfaces.Factories.Role;
using LantanaGroup.Link.Account.Application.Interfaces.Persistence;
using LantanaGroup.Link.Account.Application.Models.Role;
using LantanaGroup.Link.Account.Infrastructure;
using LantanaGroup.Link.Account.Infrastructure.Logging;
using OpenTelemetry.Trace;
using System.Diagnostics;

namespace LantanaGroup.Link.Account.Application.Queries.Role
{
    public class GetRole : IGetRole
    {
        private readonly ILogger<GetRole> _logger;
        private readonly IRoleRepository _roleRepository;
        private readonly ILinkRoleModelFactory _linkRoleModelFactory;

        public GetRole(ILogger<GetRole> logger, IRoleRepository roleRepository, ILinkRoleModelFactory linkRoleModelFactory)
        {
            _logger = logger ?? throw new ArgumentNullException(nameof(logger));
            _roleRepository = roleRepository ?? throw new ArgumentNullException(nameof(roleRepository));
            _linkRoleModelFactory = linkRoleModelFactory ?? throw new ArgumentNullException(nameof(linkRoleModelFactory));
        }

        public async Task<LinkRoleModel> Execute(string roleId, CancellationToken cancellationToken = default)
        {
            using Activity? activity = ServiceActivitySource.Instance.StartActivity("GetRole:Execute");

            try
            {
                var role = await _roleRepository.GetRoleAsync(roleId, cancellationToken: cancellationToken) ?? 
                    throw new ApplicationException($"Role with id {roleId} not found");

                return _linkRoleModelFactory.Create(role);
            }
            catch (Exception ex)
            {
                Activity.Current?.SetStatus(ActivityStatusCode.Error);
                Activity.Current?.RecordException(ex);
                _logger.LogFindRoleException(roleId, ex.Message);
                throw;
            }
        }
    }
}
