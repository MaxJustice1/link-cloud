﻿using LantanaGroup.Link.Shared.Application.Interfaces.Services;
using Link.Authorization.Infrastructure;
using Microsoft.Extensions.Logging;
using System.Security.Cryptography;

namespace LantanaGroup.Link.Shared.Application.Services.SecretManager
{
    internal class LocalSecretManager : ISecretManager
    {
        private readonly ILogger<LocalSecretManager> _logger;

        public LocalSecretManager(ILogger<LocalSecretManager> logger)
        {
            _logger = logger ?? throw new ArgumentNullException(nameof(logger));

            _logger.LogInformation("Local Secret Manager initialized");
        }


        public Task<string> GetSecretAsync(string secretName, CancellationToken cancellationToken)
        {
            return Task.FromResult(GetSecret(secretName));
        }

        public Task<string> GetSecretAsync(string secretName, string version, CancellationToken cancellationToken)
        {
            throw new NotImplementedException();
        }

        public Task<bool> SetSecretAsync(string secretName, string secretValue, CancellationToken cancellationToken)
        {
            throw new NotImplementedException();
        }

        private static string GetSecret(string secretName)
        {
            return secretName switch
            {
                LinkAuthorizationConstants.LinkBearerService.LinkBearerKeyName => GenerateRandomKey(256),
                _ => throw new NotImplementedException()
            };
        }

        private static string GenerateRandomKey(int size)
        {
            using var rng = RandomNumberGenerator.Create();

            var randomNumber = new byte[size];
            rng.GetBytes(randomNumber);
            return Convert.ToBase64String(randomNumber);
        }
    }
}
