﻿using LantanaGroup.Link.DataAcquisition.Application.Models;
using System.Diagnostics;
using LantanaGroup.Link.Shared.Application.Models;
using LantanaGroup.Link.DataAcquisition.Domain.Settings;

namespace LantanaGroup.Link.DataAcquisition.Application.Services
{
    public class ServiceActivitySource
    {
        private static string _version = string.Empty;
        public static string ServiceName = DataAcquisitionConstants.ServiceName;
        public static ActivitySource Instance { get; private set; } = new ActivitySource(ServiceName, _version);

        public static void Initialize(ServiceInformation serviceInfo)
        {
            _version = serviceInfo.Version;
            Instance = new ActivitySource(ServiceName, _version);
        }
    }
}
