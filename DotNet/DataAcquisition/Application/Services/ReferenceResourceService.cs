﻿using Confluent.Kafka;
using Hl7.Fhir.Model;
using Hl7.Fhir.Serialization;
using LantanaGroup.Link.DataAcquisition.Application.Models;
using LantanaGroup.Link.DataAcquisition.Application.Models.Factory.ReferenceQuery;
using LantanaGroup.Link.DataAcquisition.Application.Models.Kafka;
using LantanaGroup.Link.DataAcquisition.Application.Repositories;
using LantanaGroup.Link.DataAcquisition.Application.Serializers;
using LantanaGroup.Link.DataAcquisition.Application.Services.FhirApi;
using LantanaGroup.Link.DataAcquisition.Application.Utilities;
using LantanaGroup.Link.DataAcquisition.Domain.Entities;
using LantanaGroup.Link.DataAcquisition.Domain.Models.QueryConfig;
using LantanaGroup.Link.DataAcquisition.Domain.Settings;
using LantanaGroup.Link.Shared.Application.Models;
using System.Text;
using System.Text.Json;
using Task = System.Threading.Tasks.Task;

namespace LantanaGroup.Link.DataAcquisition.Application.Services;

public interface IReferenceResourceService
{
    Task<List<Resource>> Execute_NoKafka(
        ReferenceQueryFactoryResult referenceQueryFactoryResult,
        GetPatientDataRequest request,
        FhirQueryConfiguration fhirQueryConfiguration,
        ReferenceQueryConfig referenceQueryConfig,
        string queryPlanType,
        CancellationToken cancellationToken = default);

    Task Execute(
        ReferenceQueryFactoryResult referenceQueryFactoryResult, 
        GetPatientDataRequest request,
        FhirQueryConfiguration fhirQueryConfiguration,
        ReferenceQueryConfig referenceQueryConfig,
        string queryPlanType,
        CancellationToken cancellationToken = default);
}

public class ReferenceResourceService : IReferenceResourceService
{
    private readonly ILogger<ReferenceResourceService> _logger;
    private readonly IReferenceResourcesManager _referenceResourcesManager;
    private readonly IQueriedFhirResourceManager _queriedFhirResourceManager;
    private readonly IFhirApiService _fhirRepo;
    private readonly IProducer<string, ResourceAcquired> _kafkaProducer;

    public ReferenceResourceService(
        ILogger<ReferenceResourceService> logger,
        IReferenceResourcesManager referenceResourcesManager,
        IFhirApiService fhirRepo,
        IProducer<string, ResourceAcquired> kafkaProducer,
        IQueriedFhirResourceManager queriedFhirResourceManager)
    {
        _logger = logger ?? throw new ArgumentNullException(nameof(logger));
        _referenceResourcesManager = referenceResourcesManager ?? throw new ArgumentNullException(nameof(referenceResourcesManager));
        _fhirRepo = fhirRepo ?? throw new ArgumentNullException(nameof(fhirRepo));
        _kafkaProducer = kafkaProducer ?? throw new ArgumentNullException(nameof(kafkaProducer));
        _queriedFhirResourceManager = queriedFhirResourceManager ?? throw new ArgumentNullException(nameof(queriedFhirResourceManager));
    }

    public async Task<List<Resource>> Execute_NoKafka(ReferenceQueryFactoryResult referenceQueryFactoryResult, GetPatientDataRequest request, FhirQueryConfiguration fhirQueryConfiguration, ReferenceQueryConfig referenceQueryConfig, string queryPlanType, CancellationToken cancellationToken = default)
    {
        var resources = new List<Resource>();
        if (referenceQueryFactoryResult.ReferenceIds?.Count == 0)
        {
            return resources;
        }

        var validReferenceResources =
            referenceQueryFactoryResult
            ?.ReferenceIds
            ?.Where(x => x.TypeName == referenceQueryConfig.ResourceType || x.Reference.StartsWith(referenceQueryConfig.ResourceType, StringComparison.InvariantCultureIgnoreCase))
            .ToList();

        var existingReferenceResources =
            await _referenceResourcesManager.GetReferenceResourcesForListOfIds(
                validReferenceResources.Select(x => x.Reference.SplitReference()).ToList(),
                request.FacilityId);

        resources.AddRange(existingReferenceResources.Select(x => FhirResourceDeserializer.DeserializeFhirResource(x)));

        List<ResourceReference> missingReferences = validReferenceResources
            .Where(x => !existingReferenceResources.Any(y => y.ResourceId == x.Reference.SplitReference())).ToList();

        foreach(var x in missingReferences)
        {
            var fullMissingResources = await _fhirRepo.GetReferenceResource(
                fhirQueryConfiguration.FhirServerBaseUrl,
                referenceQueryFactoryResult.ResourceType,
                request.ConsumeResult.Message.Value.PatientId,
                request.FacilityId,
                request.CorrelationId,
                queryPlanType,
                x,
                referenceQueryConfig,
                fhirQueryConfiguration.Authentication);

            resources.AddRange(fullMissingResources);
        }

        return resources;
    }

    public async Task Execute(
        ReferenceQueryFactoryResult referenceQueryFactoryResult, 
        GetPatientDataRequest request,
        FhirQueryConfiguration fhirQueryConfiguration,
        ReferenceQueryConfig referenceQueryConfig,
        string queryPlanType,
        CancellationToken cancellationToken = default)
    {
        if (referenceQueryFactoryResult.ReferenceIds?.Count == 0)
        {
            return;
        }

        var validReferenceResources = 
            referenceQueryFactoryResult
            ?.ReferenceIds
            ?.Where(x => x.TypeName == referenceQueryConfig.ResourceType || x.Reference.StartsWith(referenceQueryConfig.ResourceType, StringComparison.InvariantCultureIgnoreCase))
            .ToList();

        var existingReferenceResources =
            await _referenceResourcesManager.GetReferenceResourcesForListOfIds(
                validReferenceResources.Select(x => x.Reference.SplitReference()).ToList(),
                request.FacilityId);

        foreach(var existingReference in existingReferenceResources)
        {
            await _queriedFhirResourceManager.AddAsync(new QueriedFhirResourceRecord
            {
                CorrelationId = request.CorrelationId,
                FacilityId = request.FacilityId,
                IsSuccessful = true,
                PatientId = request.ConsumeResult.Message.Value.PatientId.SplitReference(),
                QueryType = queryPlanType,
                ResourceId = existingReference.ResourceId,
                ResourceType = referenceQueryFactoryResult.ResourceType,
                CreateDate = DateTime.UtcNow,
                ModifyDate = DateTime.UtcNow
            });

            await GenerateMessage(
            FhirResourceDeserializer.DeserializeFhirResource(existingReference),
            request.FacilityId,
            request.ConsumeResult.Message.Value.PatientId,
            queryPlanType,
            request.CorrelationId,
            request.ConsumeResult.Message.Value.ScheduledReports);
        }
            

        List<ResourceReference> missingReferences = validReferenceResources
            .Where(x => !existingReferenceResources.Any(y => y.ResourceId.Equals(x.Reference.SplitReference(), StringComparison.InvariantCultureIgnoreCase))).ToList();

        foreach(var x in missingReferences)
        {
            var fullMissingResources = await _fhirRepo.GetReferenceResource(
            fhirQueryConfiguration.FhirServerBaseUrl,
            referenceQueryFactoryResult.ResourceType,
            request.ConsumeResult.Message.Value.PatientId.SplitReference(),
            request.FacilityId,
            request.CorrelationId,
            queryPlanType,
            x,
            referenceQueryConfig,
            fhirQueryConfiguration.Authentication);

            foreach (var resource in fullMissingResources)
            {
                if (resource.TypeName == nameof(OperationOutcome))
                {
                    var opOutcome = (OperationOutcome)resource;
                    _logger.LogWarning("Operation Outcome encountered:\n {opOutcome}", opOutcome.Text);
                    continue;
                }

                var jsonOptions = new JsonSerializerOptions().ForFhir(ModelInfo.ModelInspector);
                var currentDateTime = DateTime.UtcNow;
                var fhirDateTime = new FhirDateTime(
                    currentDateTime.Year,
                    currentDateTime.Month,
                    currentDateTime.Day,
                    currentDateTime.Hour,
                    currentDateTime.Minute,
                    currentDateTime.Second,
                    TimeSpan.Zero);

                var refResource = new ReferenceResources
                {
                    FacilityId = request.FacilityId,
                    ResourceId = resource.Id,
                    ReferenceResource = System.Text.Json.JsonSerializer.Serialize(resource, jsonOptions),
                    ResourceType = referenceQueryFactoryResult.ResourceType,
                    CreateDate = currentDateTime,
                    ModifyDate = currentDateTime,
                };

                await GenerateMessage(
                resource,
                request.FacilityId,
                request.ConsumeResult.Message.Value.PatientId,
                queryPlanType,
                request.CorrelationId,
                request.ConsumeResult.Message.Value.ScheduledReports);

                await _referenceResourcesManager.AddAsync(refResource);
            }
        }
    }

    private async Task GenerateMessage(
            Resource resource,
            string facilityId,
            string patientId,
            string queryType,
            string correlationId,
            List<ScheduledReport> scheduledReports)
    {
        var producerConfig = new ProducerConfig();
        producerConfig.CompressionType = CompressionType.Zstd;

        await _kafkaProducer.ProduceAsync(
            KafkaTopic.ResourceAcquired.ToString(),
            new Message<string, ResourceAcquired>
            {
                Key = facilityId,
                Headers = new Headers
                {
                    new Header(DataAcquisitionConstants.HeaderNames.CorrelationId, Encoding.UTF8.GetBytes(correlationId))
                },
                Value = new ResourceAcquired
                {
                    Resource = resource,
                    ScheduledReports = scheduledReports,
                    PatientId = string.Empty,
                    QueryType = queryType
                }
            });

    }

    
}
