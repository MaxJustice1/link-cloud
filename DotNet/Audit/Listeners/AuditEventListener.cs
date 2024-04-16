﻿using Confluent.Kafka;
using Confluent.Kafka.Extensions.Diagnostics;
using LantanaGroup.Link.Audit.Application.Commands;
using LantanaGroup.Link.Audit.Application.Interfaces;
using LantanaGroup.Link.Audit.Application.Models;
using LantanaGroup.Link.Audit.Infrastructure.Logging;
using LantanaGroup.Link.Shared.Application.Models;
using LantanaGroup.Link.Shared.Application.Models.Configs;
using Microsoft.Extensions.Options;
using System.Diagnostics;

namespace LantanaGroup.Link.Audit.Listeners
{
    public class AuditEventListener : BackgroundService
    {
        private readonly ILogger<AuditEventListener> _logger;
        private readonly IServiceScopeFactory _scopeFactory;
        private readonly IAuditFactory _auditFactory;
        private readonly IKafkaConsumerFactory _kafkaConsumerFactory;      
      
        public AuditEventListener(ILogger<AuditEventListener> logger, IServiceScopeFactory scopeFactory, IAuditFactory auditFactory, IKafkaConsumerFactory kafkaConsumerFactory, IOptions<KafkaConnection> kafkaConnection) 
        { 
            _logger = logger ?? throw new ArgumentNullException(nameof(logger));
            _scopeFactory = scopeFactory ?? throw new ArgumentNullException(nameof(scopeFactory));
            _auditFactory = auditFactory ?? throw new ArgumentNullException(nameof(auditFactory));
            _kafkaConsumerFactory = kafkaConsumerFactory ?? throw new ArgumentNullException(nameof(kafkaConsumerFactory));     
        }

        protected override Task ExecuteAsync(CancellationToken stoppingToken)
        {
            return Task.Run(() => StartConsumerLoop(stoppingToken), stoppingToken);
        }

        private async void StartConsumerLoop(CancellationToken cancellationToken)
        {
            using (var _consumer = _kafkaConsumerFactory.CreateAuditableEventConsumer(enableAutoCommit: false))
            {
                try 
                {                
                    _consumer.Subscribe(nameof(KafkaTopic.AuditableEventOccurred));                    
                    _logger.LogConsumerStarted(nameof(KafkaTopic.AuditableEventOccurred), DateTime.UtcNow);

                    while (!cancellationToken.IsCancellationRequested)
                    {
                        try
                        {                         
                            await _consumer.ConsumeWithInstrumentation(async (result, cancellationToken) => {

                                if (result != null && result.Message.Value != null)
                                {      
                                    AuditEventMessage messageValue = result.Message.Value;

                                    if (result.Message.Headers.TryGetLastBytes("X-Correlation-Id", out var headerValue))
                                    {
                                        messageValue.CorrelationId = System.Text.Encoding.UTF8.GetString(headerValue);                                        
                                    }                                   

                                    //create audit event
                                    CreateAuditEventModel eventModel = _auditFactory.Create(result.Message.Key, messageValue.ServiceName, messageValue.CorrelationId, messageValue.EventDate, messageValue.UserId, messageValue.User, messageValue.Action, messageValue.Resource, messageValue.PropertyChanges, messageValue.Notes);                                                                      
                                    _logger.LogAuditableEventConsumption(result.Message.Key, messageValue.ServiceName ?? string.Empty, eventModel);

                                    //create scoped create audit event command
                                    //deals with issue of non-singleton services being used within singleton hosted service
                                    using (var scope = _scopeFactory.CreateScope())
                                    {
                                        var _createAuditEventCommand = scope.ServiceProvider.GetRequiredService<ICreateAuditEventCommand>();
                                        _ = await _createAuditEventCommand.Execute(eventModel, cancellationToken);
                                    }                                                                      

                                    //consume the result and offset
                                    _consumer.Commit(result);

                                }
                            }, cancellationToken);          

                        }
                        catch (ConsumeException ex)
                        {
                            Activity.Current?.SetStatus(ActivityStatusCode.Error);                                                     
                            _logger.LogConsumerException(nameof(KafkaTopic.AuditableEventOccurred), ex.Message);
                            if (ex.Error.IsFatal)
                            {
                                break;
                            }
                        }
                        catch (Exception ex)
                        {
                            Activity.Current?.SetStatus(ActivityStatusCode.Error);                            
                            _logger.LogConsumerException(nameof(KafkaTopic.AuditableEventOccurred), ex.Message);
                            break;
                        }
                    }

                    _consumer.Close();
                    _consumer.Dispose();

                }
                catch(OperationCanceledException oce)
                {
                    Activity.Current?.SetStatus(ActivityStatusCode.Error);                    
                    _logger.LogOperationCanceledException(nameof(KafkaTopic.AuditableEventOccurred), oce.Message);
                    _consumer.Close();
                    _consumer.Dispose();
                }
            }
        }

    }
}