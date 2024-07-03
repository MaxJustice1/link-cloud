﻿using LantanaGroup.Link.Report.Entities;
using System.Linq.Expressions;

namespace LantanaGroup.Link.Report.Domain.Managers
{
    public interface ISubmissionEntryManager
    {
        Task<MeasureReportSubmissionEntryModel?> GetPatientSubmissionEntry(string measureReportScheduleId,
            string patientId, CancellationToken cancellationToken = default);

        Task<List<MeasureReportSubmissionEntryModel>> FindAsync(
            Expression<Func<MeasureReportSubmissionEntryModel, bool>> predicate,
            CancellationToken cancellationToken = default);

        Task<MeasureReportSubmissionEntryModel> AddAsync(MeasureReportSubmissionEntryModel entity,
            CancellationToken cancellationToken = default);

        Task<MeasureReportSubmissionEntryModel> UpdateAsync(MeasureReportSubmissionEntryModel entity,
            CancellationToken cancellationToken = default);
    }

    public class SubmissionEntryManager : ISubmissionEntryManager
    {

        private readonly IDatabase _database;

        public SubmissionEntryManager(IDatabase database)
        {
            _database = database;
        }

        public async Task<MeasureReportSubmissionEntryModel?> GetPatientSubmissionEntry(string measureReportScheduleId, string patientId, CancellationToken cancellationToken = default)
        {
            // find existing report scheduled for this facility, report type, and date range
            return (await _database.SubmissionEntryRepository.FindAsync(s => s.MeasureReportScheduleId == measureReportScheduleId && s.PatientId == patientId, cancellationToken))?.SingleOrDefault();
        }

        public async Task<List<MeasureReportSubmissionEntryModel>> FindAsync(Expression<Func<MeasureReportSubmissionEntryModel, bool>> predicate, CancellationToken cancellationToken = default)
        {
            return await _database.SubmissionEntryRepository.FindAsync(predicate, cancellationToken);
        }

        public async Task<MeasureReportSubmissionEntryModel> AddAsync(MeasureReportSubmissionEntryModel entity, CancellationToken cancellationToken = default)
        {
            return await _database.SubmissionEntryRepository.AddAsync(entity, cancellationToken);
        }

        public async Task<MeasureReportSubmissionEntryModel> UpdateAsync(MeasureReportSubmissionEntryModel entity, CancellationToken cancellationToken = default)
        {
            return await _database.SubmissionEntryRepository.AddAsync(entity, cancellationToken);
        }
    }
}