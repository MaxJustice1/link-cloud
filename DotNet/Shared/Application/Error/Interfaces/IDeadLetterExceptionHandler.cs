﻿using Confluent.Kafka;
using LantanaGroup.Link.Shared.Application.Error.Exceptions;

namespace LantanaGroup.Link.Shared.Application.Error.Interfaces
{
    public interface IDeadLetterExceptionHandler<K, V>
    {
        /// <summary>
        /// The Topic to use when publishing Retry Kafka events.
        /// </summary>
        public string Topic { get; set; }

        /// <summary>
        /// The name of the service that is consuming the IDeadLetterExceptionHandler.
        /// </summary>
        public string ServiceName { get; set; }

        void HandleException(ConsumeResult<K, V> consumeResult, string facilityId,string message = "");
        void HandleException(ConsumeResult<K, V> consumeResult, Exception ex, string facilityId);
        void HandleException(ConsumeResult<K, V> consumeResult, DeadLetterException ex, string facilityId);
        void HandleException(Headers headers, string key, string value, DeadLetterException ex, string facilityId);
        void HandleException(DeadLetterException ex, string facilityId);
        void HandleException(Exception ex, string facilityId);
        void HandleException(string message, string facilityId);
        void ProduceDeadLetter(K key, V value, Headers headers, string exceptionMessage);
        void ProduceNullConsumeResultDeadLetter(string key, string value, Headers headers, string exceptionMessage);
    }
}
