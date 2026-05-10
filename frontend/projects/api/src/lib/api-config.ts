import { InjectionToken } from '@angular/core';

export interface ApiConfig {
  readonly baseUrl: string;
}

export const API_CONFIG = new InjectionToken<ApiConfig>('Tickbox.ApiConfig');
