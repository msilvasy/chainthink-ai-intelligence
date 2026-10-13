import { Injectable, inject } from '@angular/core';
import { HttpClient } from '@angular/common/http';
import { Observable } from 'rxjs';
import { API_CONFIG } from '../config/api.config';
import { ReportRequest, ReportResponse } from '../../models/document.models';

@Injectable({
  providedIn: 'root'
})
export class ReportService {
  private http = inject(HttpClient);

  generateReport(request: ReportRequest): Observable<ReportResponse> {
    return this.http.post<ReportResponse>(API_CONFIG.endpoints.report, request);
  }
}

