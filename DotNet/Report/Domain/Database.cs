﻿using LantanaGroup.Link.Report.Entities;
using LantanaGroup.Link.Shared.Application.Models.Configs;
using LantanaGroup.Link.Shared.Application.Repositories.Interfaces;
using Microsoft.Extensions.Options;
using MongoDB.Driver;

namespace LantanaGroup.Link.Report.Domain
{
    public interface IDatabase
    {
        IEntityRepository<PatientResourceModel> PatientResourceRepository { get; set; }
        IEntityRepository<SharedResourceModel> SharedResourceRepository { get; set; }
        IEntityRepository<MeasureReportScheduleModel> ReportScheduledRepository { get; set; }
        IEntityRepository<MeasureReportSubmissionEntryModel> SubmissionEntryRepository { get; set; }
        IEntityRepository<MeasureReportConfigModel> ReportConfigRepository { get; set; }
        IEntityRepository<MeasureReportSubmissionModel> ReportSubmissionRepository { get; set; }
    }

    public class Database : IDatabase
    {
        protected IMongoDatabase DbContext { get; set; }

        public IEntityRepository<PatientResourceModel> PatientResourceRepository { get; set; }
        public IEntityRepository<SharedResourceModel> SharedResourceRepository { get; set; }
        public IEntityRepository<MeasureReportScheduleModel> ReportScheduledRepository { get; set; }
        public IEntityRepository<MeasureReportSubmissionEntryModel> SubmissionEntryRepository { get; set; }
        public IEntityRepository<MeasureReportConfigModel> ReportConfigRepository { get; set; }
        public IEntityRepository<MeasureReportSubmissionModel> ReportSubmissionRepository { get; set; }

        public Database(IOptions<MongoConnection> mongoSettings,
            IEntityRepository<PatientResourceModel> patientResourceRepository,
            IEntityRepository<SharedResourceModel> sharedResourceRepository,
            IEntityRepository<MeasureReportScheduleModel> reportScheduledRepository,
            IEntityRepository<MeasureReportSubmissionEntryModel> submissionEntryRepository,
            IEntityRepository<MeasureReportConfigModel> reportConfigRepository,
            IEntityRepository<MeasureReportSubmissionModel> reportSubmissionRepository)
        {
            var client = new MongoClient(mongoSettings.Value.ConnectionString);
            DbContext = client.GetDatabase(mongoSettings.Value.DatabaseName);

            PatientResourceRepository = patientResourceRepository;
            SharedResourceRepository = sharedResourceRepository;
            ReportScheduledRepository = reportScheduledRepository;
            SubmissionEntryRepository = submissionEntryRepository;
            ReportConfigRepository = reportConfigRepository;
            ReportSubmissionRepository = reportSubmissionRepository;
        }

        public async Task<MeasureReportScheduleModel?> GetMeasureReportSchedule(string facilityId, DateTime startDate,
        DateTime endDate, string reportType, CancellationToken cancellationToken = default)
        {
            // find existing report scheduled for this facility, report type, and date range
            return (await ReportScheduledRepository.FindAsync(
                r => r.FacilityId == facilityId && r.ReportStartDate == startDate && r.ReportEndDate == endDate &&
                     r.ReportType == reportType, cancellationToken))?.SingleOrDefault();
        }

        public async Task<List<MeasureReportScheduleModel>?> GetMeasureReportSchedules(string facilityId,
            DateTime startDate, DateTime endDate, CancellationToken cancellationToken = default)
        {
            // find existing report scheduled for this facility, report type, and date range
            return (await ReportScheduledRepository.FindAsync(
                r => r.FacilityId == facilityId && r.ReportStartDate == startDate && r.ReportEndDate == endDate,
                cancellationToken))?.ToList();
        }

        public async Task<MeasureReportSubmissionEntryModel?> GetPatientSubmissionEntry(string measureReportScheduleId,
            string patientId, CancellationToken cancellationToken = default)
        {
            // find existing report scheduled for this facility, report type, and date range
            return (await SubmissionEntryRepository.FindAsync(
                    s => s.MeasureReportScheduleId == measureReportScheduleId && s.PatientId == patientId,
                    cancellationToken))
                ?.SingleOrDefault();
        }
    }
}