﻿using LantanaGroup.Link.LinkAdmin.BFF.Application.Commands.Integration;
using LantanaGroup.Link.LinkAdmin.BFF.Application.Interfaces;
using LantanaGroup.Link.LinkAdmin.BFF.Application.Models.Integration;
using Microsoft.OpenApi.Models;
using System.Security.Claims;

namespace LantanaGroup.Link.LinkAdmin.BFF.Presentation.Endpoints
{
    public class IntegrationTestingEndpoints : IApi
    {
        private readonly ILogger<IntegrationTestingEndpoints> _logger;
        private readonly ICreatePatientEvent _createPatientEvent;
        private readonly ICreateReportScheduled _createReportScheduled;
        private readonly ICreateDataAcquisitionRequested _createDataAcquisitionRequested;

        public IntegrationTestingEndpoints(ILogger<IntegrationTestingEndpoints> logger, ICreatePatientEvent createPatientEvent, ICreateReportScheduled createReportScheduled, ICreateDataAcquisitionRequested createDataAcquisitionRequested)
        {
            _logger = logger ?? throw new ArgumentNullException(nameof(logger));
            _createPatientEvent = createPatientEvent ?? throw new ArgumentNullException(nameof(createPatientEvent));
            _createReportScheduled = createReportScheduled ?? throw new ArgumentNullException(nameof(createReportScheduled));
            _createDataAcquisitionRequested = createDataAcquisitionRequested ?? throw new ArgumentNullException(nameof(createDataAcquisitionRequested));
        }

        public void RegisterEndpoints(WebApplication app)
        {
            var integrationEndpoints = app.MapGroup("/api/integration")
                .WithOpenApi(x => new OpenApiOperation(x)
                {
                    Tags = new List<OpenApiTag> { new() { Name = "Integration" } }
                });

            integrationEndpoints.MapPost("/patient-event", CreatePatientEvent)
                .WithOpenApi(x => new OpenApiOperation(x)
                {
                    Summary = "Integration Testing - Produce Patient Event",
                    Description = "Produces a new patient event that will be sent to the broker. Allows for testing processes outside of scheduled events."
                });

            integrationEndpoints.MapPost("/report-scheduled", CreateReportScheduled)
                .WithOpenApi(x => new OpenApiOperation(x)
                {
                    Summary = "Integration Testing - Produce Report Scheduled Event",
                    Description = "Produces a new report scheduled event that will be sent to the broker. Allows for testing processes outside of scheduled events."
                });

            integrationEndpoints.MapPost("/data-acquisition-requested", CreateDataAcquisitionRequested)
                .WithOpenApi(x => new OpenApiOperation(x)
                {
                    Summary = "Integration Testing - Produce Data Acquisition Requested Event",
                    Description = "Produces a new data acquisition requested event that will be sent to the broker. Allows for testing processes outside of scheduled events."
                });

        }

        public async Task<IResult> CreatePatientEvent(HttpContext context, PatientEvent model)
        {
            var user = context.User;

            var correlationId = await _createPatientEvent.Execute(model, user?.FindFirst(ClaimTypes.Email)?.Value);
            return Results.Ok(new { 
                Id = correlationId,
                Message = $"The patient event was created succcessfully with a correlation id of '{correlationId}'."
            });
        }

        public async Task<IResult> CreateReportScheduled(HttpContext context, ReportScheduled model)
        {
            var user = context.User;

            var correlationId = await _createReportScheduled.Execute(model, user?.FindFirst(ClaimTypes.Email)?.Value);
            return Results.Ok(new
            {
                Id = correlationId,
                Message = $"The report scheduled event was created succcessfully with a correlation id of '{correlationId}'."
            });
        }

        public async Task<IResult> CreateDataAcquisitionRequested(HttpContext context, DataAcquisitionRequested model)
        {
            var user = context.User;

            var correlationId = await _createDataAcquisitionRequested.Execute(model, user?.FindFirst(ClaimTypes.Email)?.Value);
            return Results.Ok(new
            {
                Id = correlationId,
                Message = $"The data acquisition requested event was created succcessfully with a correlation id of '{correlationId}'."
            });
        }
    }
}
