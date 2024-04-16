﻿using LantanaGroup.Link.Census.Domain.Entities;
using LantanaGroup.Link.Shared.Application.Repositories.Interfaces;

namespace LantanaGroup.Link.Census.Application.Interfaces;

public interface ICensusHistoryRepository : IPersistenceRepository<PatientCensusHistoricEntity>
{
    Task<IEnumerable<PatientCensusHistoricEntity>> GetAllCensusReportsForFacility(string facilityId);
}