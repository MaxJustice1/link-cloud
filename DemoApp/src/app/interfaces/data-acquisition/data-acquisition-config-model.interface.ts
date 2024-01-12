import { FhirVersion } from "src/app/models/FhirVersion.enum";
import { IDataAcquisitionAuthenticationConfigModel } from "./data-acquisition-auth-config-model.interface";

export interface ITenantDataAcquisitionConfigModel {
    id: string;
    tenantId: string;
    facilities: IDataAcquisitionFacilityModel[];
  }

export interface IDataAcquisitionFacilityModel { 
    facilityId: string;
    fhirVersion: FhirVersion;
    baseFhirUrl: string;
    auth?: IDataAcquisitionAuthenticationConfigModel;
    resourceSettings: IDataAcquisitionConfigResourceModel[];
}

export interface IDataAcquisitionQueryConfigModel {
    id: string;
    facilityId: string;
    fhirServerBaseUrl: string;
    authentication?: IDataAcquisitionAuthenticationConfigModel;
    queryPlanIds: string[];
}

//export interface IDataAcquisitionAuthenticationConfigModel {
//    authType: string;
//    key: string;
//    tokenUrl: string;
//    audience: string;
//    clientId: string;
//    userName: string;
//    password: string;
//}

export interface IDataAcquisitionConfigResourceModel {
    resourceType: string[];
    configType: string;
    isBulk: boolean;
    usCore: IUsCoreResourceModel;
    userBasicAuth: boolean;
    auth: IDataAcquisitionAuthenticationConfigModel;
}

export interface IUsCoreResourceModel {
    useBaseFhirEndpoint: boolean;
    baseFhirUrl: string;
    useDefaultRelativePath: boolean;
    relativeFhirPath: string;
    useDefaultParameters: boolean;
    parameters: IOverrideTenantParametersModel[];
}

export interface IOverrideTenantParametersModel {
    name: string;
    values: string[];
    isQuery: boolean;
}

