﻿using LantanaGroup.Link.DataAcquisition.Application.Models;
using LantanaGroup.Link.DataAcquisition.Domain.Entities;
using LantanaGroup.Link.DataAcquisition.Domain.Interfaces;
using LantanaGroup.Link.DataAcquisition.Domain.Models.QueryConfig;
using LantanaGroup.Link.DataAcquisition.Application.Models.Kafka;
using Hl7.Fhir.Model;
using LantanaGroup.Link.DataAcquisition.Application.Factories.QueryFactories;
using LantanaGroup.Link.DataAcquisition.Application.Models.Factory.ParameterQuery;
using LantanaGroup.Link.DataAcquisition.Application.Models.Factory.ReferenceQuery;
using LantanaGroup.Link.DataAcquisition.Application.Models.Factory;
using Confluent.Kafka;
using LantanaGroup.Link.DataAcquisition.Domain.Settings;
using LantanaGroup.Link.Shared.Application.Models;
using System.Text;
using LantanaGroup.Link.DataAcquisition.Application.Services.FhirApi;
using LantanaGroup.Link.Shared.Application.Interfaces;
using Task = System.Threading.Tasks.Task;

namespace LantanaGroup.Link.DataAcquisition.Application.Services;

public interface IQueryListProcessor
{
    Task Process(IOrderedEnumerable<KeyValuePair<string, IQueryConfig>> queryList,
        GetPatientDataRequest request,
        FhirQueryConfiguration fhirQueryConfiguration,
        ScheduledReport scheduledReport,
        QueryPlan queryPlan,
        List<string> referenceTypes,
        string queryPlanType, 
        CancellationToken cancellationToken = default);
}

public class QueryListProcessor : IQueryListProcessor
{
    private readonly ILogger<QueryListProcessor> _logger;
    private readonly IFhirApiService _fhirRepo;
    private readonly IKafkaProducerFactory<string, ResourceAcquired> _kafkaProducerFactory;
    private readonly IReferenceResourceService _referenceResourceService;
    private readonly ProducerConfig _producerConfig;

    public QueryListProcessor(
        ILogger<QueryListProcessor> logger, 
        IFhirApiService fhirRepo, 
        IKafkaProducerFactory<string, ResourceAcquired> kafkaProducerFactory, 
        IReferenceResourceService referenceResourceService)
    {
        _logger = logger ?? throw new ArgumentNullException(nameof(logger));
        _fhirRepo = fhirRepo ?? throw new ArgumentNullException(nameof(fhirRepo));
        _kafkaProducerFactory = kafkaProducerFactory ?? throw new ArgumentNullException(nameof(kafkaProducerFactory));
        _referenceResourceService = referenceResourceService ?? throw new ArgumentNullException(nameof(referenceResourceService));

        _producerConfig = new ProducerConfig();
        _producerConfig.CompressionType = CompressionType.Zstd;
    }

    public async Task Process(
        IOrderedEnumerable<KeyValuePair<string, IQueryConfig>> queryList, 
        GetPatientDataRequest request, 
        FhirQueryConfiguration fhirQueryConfiguration, 
        ScheduledReport scheduledReport, 
        QueryPlan queryPlan, 
        List<string> referenceTypes, 
        string queryPlanType, 
        CancellationToken cancellationToken = default
        )
    {
        List<ResourceReference> referenceResources = new List<ResourceReference>();
        foreach (var query in queryList)
        {
            var queryConfig = query.Value;
            QueryFactoryResult builtQuery = queryConfig switch
            {
                ParameterQueryConfig => ParameterQueryFactory.Build((ParameterQueryConfig)queryConfig, request,
                    scheduledReport, queryPlan.LookBack),
                ReferenceQueryConfig => ReferenceQueryFactory.Build((ReferenceQueryConfig)queryConfig, referenceResources),
                _ => throw new Exception("Unable to identify type for query operation."),
            };

            _logger.LogInformation("Processing Query for:");

            if (builtQuery.GetType() == typeof(SingularParameterQueryFactoryResult))
            {
                var queryInfo = (ParameterQueryConfig)queryConfig;
                _logger.LogInformation("Resource: {1}", queryInfo.ResourceType);

                var bundle = await _fhirRepo.GetSingularBundledResultsAsync(
                    fhirQueryConfiguration.FhirServerBaseUrl,
                    request.ConsumeResult.Message.Value.PatientId,
                    request.CorrelationId,
                    request.FacilityId,
                    queryPlanType,
                    (SingularParameterQueryFactoryResult)builtQuery,
                    (ParameterQueryConfig)queryConfig,
                    scheduledReport,
                    fhirQueryConfiguration.Authentication);

                referenceResources.AddRange(ReferenceResourceBundleExtractor.Extract(bundle, referenceTypes));

                await GenerateMessagesFromBundle(bundle, request.FacilityId, request.ConsumeResult.Message.Value.PatientId, queryPlanType, request.CorrelationId, new List<ScheduledReport> { scheduledReport }, CancellationToken.None);
            }

            if (builtQuery.GetType() == typeof(PagedParameterQueryFactoryResult))
            {
                var queryInfo = (ParameterQueryConfig)queryConfig;
                _logger.LogInformation("Resource: {1}", queryInfo.ResourceType);

                var bundle = await _fhirRepo.GetPagedBundledResultsAsync(
                    fhirQueryConfiguration.FhirServerBaseUrl,
                    request.ConsumeResult.Message.Value.PatientId,
                    request.CorrelationId,
                    request.FacilityId,
                    queryPlanType,
                    (PagedParameterQueryFactoryResult)builtQuery,
                    (ParameterQueryConfig)queryConfig,
                    scheduledReport,
                    fhirQueryConfiguration.Authentication);

                referenceResources.AddRange(ReferenceResourceBundleExtractor.Extract(bundle, referenceTypes));

                await GenerateMessagesFromBundle(bundle, request.FacilityId, request.ConsumeResult.Message.Value.PatientId, queryPlanType, request.CorrelationId, new List<ScheduledReport> { scheduledReport }, CancellationToken.None);
            }

            if (builtQuery.GetType() == typeof(ReferenceQueryFactoryResult))
            {
                var referenceQueryFactoryResult = (ReferenceQueryFactoryResult)builtQuery;

                var queryInfo = (ReferenceQueryConfig)queryConfig;
                _logger.LogInformation("Resource: {1}", queryInfo.ResourceType);

                await _referenceResourceService.Execute(
                    referenceQueryFactoryResult,
                    request,
                    fhirQueryConfiguration,
                    queryInfo,
                    queryPlanType);
            }

        }
    }

    private async Task GenerateMessagesFromBundle(
        Bundle bundle,
        string facilityId,
        string patientId,
        string queryType,
        string correlationId,
        List<ScheduledReport> scheduledReports,
        CancellationToken cancellationToken)
    {
        bundle.Entry.ForEach(e =>
        {
            if (e.Resource is Resource resource)
            {
                _kafkaProducerFactory.CreateProducer(_producerConfig).Produce(
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
                            PatientId = RemovePatientId(e.Resource) ? string.Empty : patientId,
                            QueryType = queryType
                        }
                    });
            }
        });
    }

    private bool RemovePatientId(Resource resource)
    {
        return resource switch
        {
            Device => true,
            Medication => true,
            Location => true,
            Specimen => true,
            _ => false,
        };
    }
}
