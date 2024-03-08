﻿using LantanaGroup.Link.Census.Settings;
using MediatR;
using Microsoft.Extensions.Options;

namespace LantanaGroup.Link.Census.Commands.TenantCheck;

public class CheckIfTenantExistsQuery : IRequest<bool>
{
    public string TenantId { get; set; }
}

public class CheckIfTenantExistsQueryHandler : IRequestHandler<CheckIfTenantExistsQuery, bool>
{
    private readonly HttpClient _httpClient;
    private readonly TenantConfig _tenantConfig;

    public CheckIfTenantExistsQueryHandler(HttpClient httpClient, IOptions<TenantConfig> tenantConfig)
    {
        _httpClient = httpClient;
        _tenantConfig = tenantConfig.Value;
    }

    public async Task<bool> Handle(CheckIfTenantExistsQuery request, CancellationToken cancellationToken)
    {
        if (!_tenantConfig.CheckIfTenantExists)
            return true;

        var url = $"{_tenantConfig.TenantServiceBaseEndpoint.TrimEnd('/')}/{_tenantConfig.GetTenantRelativeEndpoint.TrimEnd('/')}/{request.TenantId}";
        var response = await _httpClient.GetAsync(url, cancellationToken);
        return response.IsSuccessStatusCode;
    }
}
