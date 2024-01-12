import { IDataAcquisitionAuthenticationConfigModel } from "./data-acquisition-auth-config-model.interface";

export interface IDataAcquisitionQueryConfigModel {
  id: string;
  facilityId: string;
  fhirServerBaseUrl: string;
  authentication?: IDataAcquisitionAuthenticationConfigModel;
  queryPlanIds: string[];
}
