﻿using Confluent.Kafka;
using Confluent.Kafka.Extensions.Diagnostics;
using Hl7.Fhir.Model;
using Hl7.Fhir.Serialization;
using LantanaGroup.Link.Normalization.Application.Commands;
using LantanaGroup.Link.Normalization.Application.Commands.Config;
using LantanaGroup.Link.Normalization.Application.Interfaces;
using LantanaGroup.Link.Normalization.Application.Models;
using LantanaGroup.Link.Normalization.Application.Models.Exceptions;
using LantanaGroup.Link.Normalization.Application.Models.Messages;
using LantanaGroup.Link.Normalization.Application.Settings;
using LantanaGroup.Link.Normalization.Domain.Entities;
using LantanaGroup.Link.Normalization.Domain.JsonObjects;
using LantanaGroup.Link.Shared.Application.Error.Interfaces;
using LantanaGroup.Link.Shared.Application.Interfaces;
using LantanaGroup.Link.Shared.Application.Models;
using LantanaGroup.Link.Shared.Application.Models.Kafka;
using LantanaGroup.Link.Shared.Application.Models.Telemetry;
using LantanaGroup.Link.Shared.Application.Utilities;
using MediatR;
using Microsoft.Extensions.Options;
using System.Text;
using System.Text.Json;

namespace LantanaGroup.Link.Normalization.Listeners;

public class ResourceAcquiredListener : BackgroundService
{
    private readonly ILogger<ResourceAcquiredListener> _logger;
    private readonly IMediator _mediator;
    private readonly IKafkaConsumerFactory<string, ResourceAcquiredMessage> _consumerFactory;
    private readonly IKafkaProducerFactory<string, ResourceNormalizedMessage> _producerFactory;
    private readonly IDeadLetterExceptionHandler<string, string> _consumeExceptionHandler;
    private readonly IDeadLetterExceptionHandler<string, ResourceAcquiredMessage> _deadLetterExceptionHandler;
    private readonly ITransientExceptionHandler<string, ResourceAcquiredMessage> _transientExceptionHandler;
    private bool _cancelled = false;
    private readonly INormalizationServiceMetrics _metrics;

    public ResourceAcquiredListener(
        ILogger<ResourceAcquiredListener> logger,
        IOptions<ServiceInformation> serviceInformation,
        IMediator mediator,
        IKafkaConsumerFactory<string, ResourceAcquiredMessage> consumerFactory,
        IKafkaProducerFactory<string, ResourceNormalizedMessage> producerFactory,
        IDeadLetterExceptionHandler<string, string> consumeExceptionHandler,
        IDeadLetterExceptionHandler<string, ResourceAcquiredMessage> deadLetterExceptionHandler,
        ITransientExceptionHandler<string, ResourceAcquiredMessage> transientExceptionHandler,
        INormalizationServiceMetrics metrics)
    {
        this._logger = logger ?? throw new ArgumentNullException(nameof(logger));
        _mediator = mediator ?? throw new ArgumentNullException(nameof(mediator));
        _consumerFactory = consumerFactory ?? throw new ArgumentNullException(nameof(consumerFactory));
        _producerFactory = producerFactory ?? throw new ArgumentNullException(nameof(producerFactory));
        _consumeExceptionHandler = consumeExceptionHandler ?? throw new ArgumentNullException(nameof(consumeExceptionHandler));
        _consumeExceptionHandler.ServiceName = serviceInformation.Value.Name;
        _consumeExceptionHandler.Topic = $"{nameof(KafkaTopic.ResourceAcquired)}-Error";
        _deadLetterExceptionHandler = deadLetterExceptionHandler ?? throw new ArgumentNullException(nameof(deadLetterExceptionHandler));
        _deadLetterExceptionHandler.ServiceName = serviceInformation.Value.Name;
        _deadLetterExceptionHandler.Topic = $"{nameof(KafkaTopic.ResourceAcquired)}-Error";
        _transientExceptionHandler = transientExceptionHandler;
        _transientExceptionHandler.ServiceName = serviceInformation.Value.Name;
        _transientExceptionHandler.Topic = KafkaTopic.ResourceAcquiredRetry.GetStringValue();
        _metrics = metrics ?? throw new ArgumentNullException(nameof(metrics));
    }

    ~ResourceAcquiredListener()
    {
     
    }

    public async System.Threading.Tasks.Task StartAsync(CancellationToken cancellationToken)
    {
        await base.StartAsync(cancellationToken);
    }

    protected override async System.Threading.Tasks.Task ExecuteAsync(CancellationToken cancellationToken)
    {
        await System.Threading.Tasks.Task.Run(() => StartConsumerLoop(cancellationToken), cancellationToken);
    }

    private async System.Threading.Tasks.Task StartConsumerLoop(CancellationToken cancellationToken)
    {
        using var kafkaConsumer = _consumerFactory.CreateConsumer(new ConsumerConfig
        {
            GroupId = NormalizationConstants.ServiceName,
            EnableAutoCommit = false
        });
        using var kafkaProducer = _producerFactory.CreateProducer(new ProducerConfig(), useOpenTelemetry: true);
        kafkaConsumer.Subscribe(new string[] { KafkaTopic.ResourceAcquired.ToString() });

        while (!cancellationToken.IsCancellationRequested && !_cancelled)
        {
            ConsumeResult<string, ResourceAcquiredMessage>? message;
            try
            {                
                await kafkaConsumer.ConsumeWithInstrumentation(async (result, CancellationToken) =>
                {
                    message = result;

                    //if (message == null)
                    //    continue;

                    (string facilityId, string correlationId) messageMetaData = (string.Empty, string.Empty);
                    try
                    {
                        messageMetaData = ExtractFacilityIdAndCorrelationIdFromMessage(message.Message);
                    }
                    catch (Exception ex)
                    {
                        _deadLetterExceptionHandler.HandleException(message, ex, AuditEventType.Create, message.Message.Key);
                        kafkaConsumer.Commit(message);
                        return;
                    }

                    NormalizationConfig? config = null;
                    try
                    {
                        config = await _mediator.Send(new GetConfigurationEntityQuery
                        {
                            FacilityId = messageMetaData.facilityId
                        });
                    }
                    catch (Exception ex)
                    {
                        var errorMessage = $"An error was encountered retrieving facility configuration for {messageMetaData.facilityId}";

                        _logger.LogError(errorMessage, ex);
                        _transientExceptionHandler.HandleException(message, ex, AuditEventType.Create, messageMetaData.facilityId);
                        kafkaConsumer.Commit(message);
                        return;
                    }

                    var opSeq = config.OperationSequence.OrderBy(x => x.Key).ToList();

                    Base resource = null;
                    try
                    {
                        resource = DeserializeResource(message.Message.Value.Resource);
                    }
                    catch (Exception ex)
                    {
                        _deadLetterExceptionHandler.HandleException(message, ex, AuditEventType.Create, messageMetaData.facilityId);
                        kafkaConsumer.Commit(message);
                        return;
                    }

                    var operationCommandResult = new OperationCommandResult
                    {
                        Resource = resource,
                        PropertyChanges = new List<PropertyChangeModel>()
                    };

                    //fix resource ids
                    operationCommandResult = await _mediator.Send(new FixResourceIDCommand
                    {
                        Resource = resource,
                        PropertyChanges = operationCommandResult.PropertyChanges
                    });

                    try
                    {
                        foreach (var op in opSeq)
                        {
                            ConceptMapOperation conceptMapOperation = null;
                            if (op.Value is ConceptMapOperation)
                            {
                                conceptMapOperation = JsonSerializer.Deserialize<ConceptMapOperation>(JsonSerializer.SerializeToElement(op.Value));
                                JsonElement conceptMapJsonEle = (JsonElement)conceptMapOperation.FhirConceptMap;
                                conceptMapOperation.FhirConceptMap = JsonSerializer.Deserialize<ConceptMap>(
                                    conceptMapJsonEle,
                                    new JsonSerializerOptions().ForFhir(
                                        ModelInfo.ModelInspector,
                                        new FhirJsonPocoDeserializerSettings { Validator = null }));
                            }

                            operationCommandResult = op.Value switch
                            {
                                ConceptMapOperation => await _mediator.Send(new ApplyConceptMapCommand
                                {
                                    Resource = resource,
                                    Operation = conceptMapOperation,
                                    PropertyChanges = operationCommandResult.PropertyChanges
                                }),
                                ConditionalTransformationOperation => await _mediator.Send(new ConditionalTransformationCommand
                                {
                                    Resource = resource,
                                    Operation = (ConditionalTransformationOperation)op.Value,
                                    PropertyChanges = operationCommandResult.PropertyChanges
                                }),
                                CopyElementOperation => await _mediator.Send(new CopyElementCommand
                                {
                                    Resource = resource,
                                    Operation = (CopyElementOperation)op.Value,
                                    PropertyChanges = operationCommandResult.PropertyChanges
                                }),
                                CopyLocationIdentifierToTypeOperation => await _mediator.Send(new CopyLocationIdentifierToTypeCommand
                                {
                                    Resource = resource,
                                    PropertyChanges = operationCommandResult.PropertyChanges
                                }),
                                PeriodDateFixerOperation => await _mediator.Send(new PeriodDateFixerCommand
                                {
                                    Resource = resource,
                                    PropertyChanges = operationCommandResult.PropertyChanges
                                }),
                                _ => await _mediator.Send(new UnknownOperationCommand
                                {
                                    Resource = resource,
                                    PropertyChanges = operationCommandResult.PropertyChanges
                                }),
                            };

                            _metrics.IncrementResourceNormalizedCounter(new List<KeyValuePair<string, object?>>() {
                                new KeyValuePair<string, object?>(DiagnosticNames.FacilityId, messageMetaData.facilityId),
                                new KeyValuePair<string, object?>(DiagnosticNames.CorrelationId, messageMetaData.correlationId),
                                new KeyValuePair<string, object?>(DiagnosticNames.PatientId, message.Message.Value.PatientId),
                                new KeyValuePair<string, object?>(DiagnosticNames.Resource, operationCommandResult.Resource.TypeName),
                                new KeyValuePair<string, object?>(DiagnosticNames.QueryType, message.Message.Value.QueryType),
                                new KeyValuePair<string, object?>(DiagnosticNames.NormalizationOperation, op.Value.GetType().Name)
                            });
                        }
                    }
                    catch(NoEntityFoundException ex)
                    {
                        var errorMessage = $"An error was encountered processing Operation Commands for {messageMetaData.facilityId}";

                        _logger.LogError(ex, "An error was encountered processing Operation Commands for {facilityId}", messageMetaData.facilityId);
                        _transientExceptionHandler.HandleException(message, ex, AuditEventType.Create, messageMetaData.facilityId);
                        kafkaConsumer.Commit(message);
                    }
                    catch (Exception ex)
                    {
                        var errorMessage = $"An error was encountered processing Operation Commands for {messageMetaData.facilityId}";

                        _logger.LogError(errorMessage, ex);
                        _transientExceptionHandler.HandleException(message, ex, AuditEventType.Create, messageMetaData.facilityId);
                        kafkaConsumer.Commit(message);
                        return;
                    }

                    try
                    {
                        await _mediator.Send(new TriggerAuditEventCommand
                        {
                            CorrelationId = messageMetaData.correlationId,
                            FacilityId = messageMetaData.facilityId,
                            resourceAcquiredMessage = message.Value,
                            PropertyChanges = operationCommandResult.PropertyChanges
                        });

                        var serializedResource = JsonSerializer.SerializeToElement(operationCommandResult.Resource, new JsonSerializerOptions().ForFhir(ModelInfo.ModelInspector));

                        var headers = new Headers
                    {
                        new Header(NormalizationConstants.HeaderNames.CorrelationId, Encoding.UTF8.GetBytes(messageMetaData.correlationId))
                    };
                        var resourceNormalizedMessage = new ResourceNormalizedMessage
                        {
                            PatientId = message.Value.PatientId ?? "",
                            Resource = serializedResource,

                            QueryType = message.Value.QueryType,
                            ScheduledReports = message.Message.Value.ScheduledReports
                        };
                        Message<string, ResourceNormalizedMessage> produceMessage = new Message<string, ResourceNormalizedMessage>
                        {
                            Key = messageMetaData.facilityId,
                            Headers = headers,
                            Value = resourceNormalizedMessage
                        };
                        await kafkaProducer.ProduceAsync(KafkaTopic.ResourceNormalized.ToString(), produceMessage);

                    }
                    catch (Exception ex)
                    {
                        var errorMessage = $"An error was encountered building/producing kafka message for {messageMetaData.facilityId}";

                        _logger.LogError(errorMessage, ex);
                        _transientExceptionHandler.HandleException(message, ex, AuditEventType.Create, messageMetaData.facilityId);
                        kafkaConsumer.Commit(message);
                        return;
                    }

                    kafkaConsumer.Commit(message);

                }, cancellationToken);
            }
            catch (OperationCanceledException)
            {
                continue;
            }
            catch (ConsumeException ex)
            {
                if (ex.Error.Code == ErrorCode.UnknownTopicOrPart)
                {
                    new OperationCanceledException(ex.Error.Reason, ex);
                }

                string facilityId = Encoding.UTF8.GetString(ex.ConsumerRecord?.Message?.Key ?? []);
                
                ConsumeResult<string, string> result = new ConsumeResult<string, string>
                {
                    Message = new Message<string, string>
                    {
                        Headers = ex.ConsumerRecord?.Message?.Headers,
                        Key = facilityId,
                        Value = Encoding.UTF8.GetString(ex.ConsumerRecord?.Message?.Value ?? Array.Empty<byte>())
                    }
                };
                _consumeExceptionHandler.HandleException(result, ex, AuditEventType.Create, facilityId);
                TopicPartitionOffset? offset = ex.ConsumerRecord?.TopicPartitionOffset;
                if (offset == null)
                {
                    kafkaConsumer.Commit();
                }
                else
                {
                    kafkaConsumer.Commit( new List<TopicPartitionOffset> {
                        offset
                    });
                }
                continue;
            }            
        }
    }

    public void Cancel()
    {
        this._cancelled = true;
    }

    public async System.Threading.Tasks.Task StopAsync(CancellationToken cancellationToken)
    {
        
    }

    private (string facilityId, string correlationId) ExtractFacilityIdAndCorrelationIdFromMessage(Message<string, ResourceAcquiredMessage> message)
    {
        var facilityId = message.Key;
        var cIBytes = message.Headers.FirstOrDefault(x => x.Key == NormalizationConstants.HeaderNames.CorrelationId)?.GetValueBytes();

        if (cIBytes == null || cIBytes.Length == 0)
            return (facilityId, string.Empty);


        var correlationId = Encoding.UTF8.GetString(cIBytes);

        return (facilityId, correlationId);
    }

    private Base DeserializeStringToResource(string json)
    {
        return JsonSerializer.Deserialize<Resource>(json, new JsonSerializerOptions().ForFhir(ModelInfo.ModelInspector, new FhirJsonPocoDeserializerSettings { Validator = null }));
    }

    private Base DeserializeResource(object resource)
    {

        switch (resource)
        {
            case JsonElement:
                return DeserializeStringToResource(resource.ToString());
            case string:
                return DeserializeStringToResource((string)resource);
            default:
                throw new DeserializationUnsupportedTypeException();
        }

        /*return resource switch
        {
            JsonElement => DeserializeStringToResource(JsonObject.Create((JsonElement)resource).ToJsonString()),
            string => DeserializeStringToResource((string)resource),
            _ => throw new DeserializationUnsupportedTypeException()
        };
*/
    }

}
