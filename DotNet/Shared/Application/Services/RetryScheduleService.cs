﻿using Amazon.Runtime.Internal.Util;
using LantanaGroup.Link.Shared.Application.Models;
using LantanaGroup.Link.Shared.Application.Repositories.Implementations;
using LantanaGroup.Link.Shared.Jobs;
using MediatR;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using Quartz;
using Quartz.Spi;

namespace LantanaGroup.Link.Shared.Application.Services
{
    public class RetryScheduleService : BackgroundService
    {
        private readonly ILogger<RetryScheduleService> _logger;
        private readonly IJobFactory _jobFactory;
        private readonly ISchedulerFactory _schedulerFactory;
        private readonly RetryRepository _retryRepository;

        public IScheduler Scheduler { get; set; } = default!;

        public RetryScheduleService(ILogger<RetryScheduleService> logger, IJobFactory jobFactory, ISchedulerFactory schedulerFactory, RetryRepository repository)
        {
            _logger = logger;
            _jobFactory = jobFactory;
            _schedulerFactory = schedulerFactory;
            _retryRepository = repository;
        }


        protected override async Task ExecuteAsync(CancellationToken cancellationToken)
        {
            var scheduler = await _schedulerFactory.GetScheduler(cancellationToken);
            scheduler.JobFactory = _jobFactory;

            var retries = await _retryRepository.GetAllAsync(cancellationToken);

            foreach (var retry in retries)
            {
                try
                {
                    await CreateJobAndTrigger(retry, scheduler);
                }
                catch (Exception ex)
                {
                    _logger.LogError($"Could not schedule {retry.Id}: {ex.Message}");
                }
            }

            await scheduler.Start(cancellationToken);
            _logger.LogInformation("RetryScheduleService started.");
        }

        public override async Task StopAsync(CancellationToken cancellationToken)
        {
            await Scheduler.Shutdown(cancellationToken);
            await base.StopAsync(cancellationToken);
        }


        public static async Task CreateJobAndTrigger(RetryEntity entity, IScheduler scheduler)
        {
            IJobDetail job = CreateJob(entity);

            await scheduler.AddJob(job, true);

            ITrigger trigger = CreateTrigger(entity, job.Key);
            await scheduler.ScheduleJob(trigger);
        }


        public static IJobDetail CreateJob(RetryEntity entity)
        {
            JobDataMap jobDataMap = new JobDataMap();

            jobDataMap.Put("RetryEntity", entity);

            return JobBuilder
                .Create(typeof(RetryJob))
                .StoreDurably()
                .WithIdentity(entity.JobId)
                .WithDescription($"{entity.FacilityId}-{entity.Topic}")
                .UsingJobData(jobDataMap)
                .Build();
        }

        private static ITrigger CreateTrigger(RetryEntity entity, JobKey jobKey)
        {
            JobDataMap jobDataMap = new JobDataMap();

            jobDataMap.Put("RetryEntity", entity);

            var offset = DateBuilder.DateOf(entity.ScheduledTrigger.Hour, entity.ScheduledTrigger.Minute, entity.ScheduledTrigger.Second);

            return TriggerBuilder
                .Create()                
                .StartAt(offset)
                .ForJob(jobKey)
                .WithIdentity(Guid.NewGuid().ToString(), jobKey.Group)
                .WithDescription($"{entity.Id}-{entity.ScheduledTrigger}")
                .UsingJobData(jobDataMap)
                .Build();
        }


        public static async Task DeleteJob(RetryEntity entity, IScheduler scheduler)
        {
            JobKey jobKey = new JobKey(entity.JobId);
            await scheduler.DeleteJob(jobKey);
        }

        public static async Task RescheduleJob(RetryEntity entity, IScheduler scheduler)
        {
            await DeleteJob(entity, scheduler);
            await CreateJobAndTrigger(entity, scheduler);
        }
    }
}