﻿using LantanaGroup.Link.DataAcquisition.Application.Interfaces;
using LantanaGroup.Link.DataAcquisition.Application.Models.Exceptions;
using LantanaGroup.Link.DataAcquisition.Domain;
using LantanaGroup.Link.DataAcquisition.Domain.Entities;
using LantanaGroup.Link.DataAcquisition.Domain.Models;
using LantanaGroup.Link.Shared.Application.Repositories.Implementations;
using Microsoft.EntityFrameworkCore;
using MongoDB.Driver;

namespace LantanaGroup.Link.DataAcquisition.Application.Repositories;

public class FhirQueryListConfigurationRepository : BaseSqlConfigurationRepo<FhirListConfiguration>, IFhirQueryListConfigurationRepository
{
    private readonly ILogger<FhirQueryListConfigurationRepository> _logger;
    private readonly DataAcquisitionDbContext _dbContext;

    public FhirQueryListConfigurationRepository(ILogger<FhirQueryListConfigurationRepository> logger, DataAcquisitionDbContext dbContext) : base(logger, dbContext)
    {
        _logger = logger ?? throw new ArgumentNullException(nameof(logger));
        _dbContext = dbContext ?? throw new ArgumentNullException(nameof(dbContext));
    }

    public async Task<AuthenticationConfiguration> GetAuthenticationConfigurationByFacilityId(string facilityId, CancellationToken cancellationToken = default)
    {
        var queryResult = await _dbContext.FhirListConfigurations.FirstOrDefaultAsync(x => x.FacilityId == facilityId, cancellationToken);

        if (queryResult == null)
        {
            throw new NotFoundException($"No configuration found for facilityId: {facilityId}. Unable to retrieve Authentication settings.");
        }

        if (queryResult.Authentication == null)
        {
            throw new NotFoundException($"No Authentication found on configuration for facilityId: {facilityId}. Unable to retrieve Authentication settings.");
        }

        return queryResult.Authentication;
    }

    public async Task SaveAuthenticationConfiguration(string facilityId, AuthenticationConfiguration config, CancellationToken cancellationToken = default)
    {
        var queryResult = await _dbContext.FhirListConfigurations.FirstOrDefaultAsync(x => x.FacilityId == facilityId, cancellationToken);

        if (queryResult == null)
        {
            throw new NotFoundException($"No configuration found for facilityId: {facilityId}. Unable to save Authentication settings.");
        }

        queryResult.Authentication = config;
        _dbContext.FhirListConfigurations.Update(queryResult);
        await _dbContext.SaveChangesAsync(cancellationToken);
    }

    public async Task DeleteAuthenticationConfiguration(string facilityId, CancellationToken cancellationToken = default)
    {
        var entity = await _dbContext.FhirListConfigurations.FirstOrDefaultAsync(x => x.FacilityId == facilityId, cancellationToken);

        if (entity == null)
            throw new NotFoundException();

        _dbContext.FhirListConfigurations.Remove(entity);
        await _dbContext.SaveChangesAsync(cancellationToken);
    }

    public async Task<FhirListConfiguration> GetByFacilityIdAsync(string facilityId, CancellationToken cancellation = default)
    {
        var queryResult = await _dbContext.FhirListConfigurations.FirstOrDefaultAsync(x => x.FacilityId == facilityId, cancellation);
        return queryResult;
    }

    public override FhirListConfiguration Get(string facilityId)
    {
        return _dbContext.FhirListConfigurations.FirstOrDefault(x => x.FacilityId == facilityId);
    }

    public override async Task<FhirListConfiguration> GetAsync(string facilityId, CancellationToken cancellationToken = default)
    {
        return await _dbContext.FhirListConfigurations.FirstOrDefaultAsync(x => x.FacilityId == facilityId);
    }

    public void Dispose()
    {
        throw new NotImplementedException();
    }
}
