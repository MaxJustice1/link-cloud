package com.lantanagroup.link.measureeval.services;

import com.lantanagroup.link.measureeval.kafka.Headers;
import com.lantanagroup.link.measureeval.kafka.Topics;
import com.lantanagroup.link.measureeval.models.NormalizationStatus;
import com.lantanagroup.link.measureeval.records.DataAcquisitionRequested;
import com.lantanagroup.link.measureeval.records.ResourceEvaluated;
import com.lantanagroup.link.measureeval.records.ResourceNormalized;
import com.lantanagroup.link.measureeval.repositories.AbstractResourceRepository;
import com.lantanagroup.link.measureeval.repositories.PatientReportingEvaluationStatusRepository;
import org.apache.kafka.clients.consumer.ConsumerRecord;
import org.hl7.fhir.r4.model.MeasureReport;
import org.springframework.context.annotation.Bean;
import org.springframework.kafka.annotation.KafkaListener;
import org.springframework.kafka.annotation.RetryableTopic;
import org.springframework.kafka.core.KafkaTemplate;
import org.springframework.kafka.retrytopic.RetryTopicConfiguration;
import org.springframework.kafka.retrytopic.RetryTopicConfigurationBuilder;
import org.springframework.messaging.handler.annotation.Header;
import org.springframework.stereotype.Service;

import java.util.function.Predicate;

@Service
public class ResourceNormalizedConsumer extends AbstractResourceConsumer<ResourceNormalized> {
    public ResourceNormalizedConsumer(
            AbstractResourceRepository resourceRepository,
            PatientReportingEvaluationStatusRepository patientStatusRepository,
            DataAcquisitionClient dataAcquisitionClient,
            MeasureEvaluatorCache measureEvaluatorCache,
            MeasureReportNormalizer measureReportNormalizer,
            Predicate<MeasureReport> reportabilityPredicate,
            KafkaTemplate<String, DataAcquisitionRequested> dataAcquisitionRequestedTemplate,
            KafkaTemplate<ResourceEvaluated.Key, ResourceEvaluated> resourceEvaluatedTemplate) {
        super(
                resourceRepository,
                patientStatusRepository,
                dataAcquisitionClient,
                measureEvaluatorCache,
                measureReportNormalizer,
                reportabilityPredicate,
                dataAcquisitionRequestedTemplate,
                resourceEvaluatedTemplate);
    }

    @Override
    protected NormalizationStatus getNormalizationStatus() {
        return NormalizationStatus.NORMALIZED;
    }

    @KafkaListener(topics = Topics.RESOURCE_NORMALIZED)
    //@RetryableTopic()
    public void consume(
            @Header(Headers.CORRELATION_ID) String correlationId,
            ConsumerRecord<String, ResourceNormalized> record) {
        doConsume(correlationId, record);
    }
}
