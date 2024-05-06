﻿using LantanaGroup.Link.Account.Application.Interfaces.Factories.User;
using LantanaGroup.Link.Account.Application.Interfaces.Persistence;
using LantanaGroup.Link.Account.Application.Models.User;
using LantanaGroup.Link.Account.Infrastructure;
using LantanaGroup.Link.Account.Infrastructure.Logging;
using LantanaGroup.Link.Shared.Application.Extensions.Telemetry;
using LantanaGroup.Link.Shared.Application.Models.Telemetry;
using OpenTelemetry.Trace;
using System.Diagnostics;

namespace LantanaGroup.Link.Account.Application.Queries.User
{
    public class GetUserByEmail : IGetUserByEmail
    {
        private readonly ILogger<GetUserByEmail> _logger;
        private readonly IUserRepository _userRepository;
        private readonly ILinkUserModelFactory _linkUserModelFactory;

        public GetUserByEmail(ILogger<GetUserByEmail> logger, IUserRepository userRepository, ILinkUserModelFactory linkUserModelFactory)
        {
            _logger = logger ?? throw new ArgumentNullException(nameof(logger));
            _userRepository = userRepository ?? throw new ArgumentNullException(nameof(userRepository));
            _linkUserModelFactory = linkUserModelFactory ?? throw new ArgumentNullException(nameof(linkUserModelFactory));
        }

        public async Task<LinkUserModel> Execute(string email, CancellationToken cancellationToken = default)
        {
            List<KeyValuePair<string, object?>> tagList = [new KeyValuePair<string, object?>(DiagnosticNames.Email, email)];
            using Activity? activity = ServiceActivitySource.Instance.StartActivityWithTags("GetUserByEmail:Execute", tagList);

            try
            {
                if(string.IsNullOrEmpty(email))
                {
                    throw new ArgumentException("An email address is required");
                }

                var user = await _userRepository.GetUserByEmailAsync(email, cancellationToken: cancellationToken);

                LinkUserModel userModel = user is null ? throw new ApplicationException($"User with an email address of {email} was not found") 
                    : _linkUserModelFactory.Create(user);

                return userModel;
            }
            catch (Exception ex)
            {
                Activity.Current?.SetStatus(ActivityStatusCode.Error);
                Activity.Current?.RecordException(ex);
                _logger.LogFindUserException(email, ex.Message);
                throw;
            }
        }
    }
}
