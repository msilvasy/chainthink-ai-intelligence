import { Injectable, inject } from '@angular/core';
import { HttpClient, HttpParams } from '@angular/common/http';
import { Observable } from 'rxjs';
import { API_CONFIG } from '../config/api.config';
import { LlmModelsResponse, QueryLog, QueryRequest, QueryResponse } from '../../models/document.models';

@Injectable({
  providedIn: 'root'
})
export class ChatService {
  private http = inject(HttpClient);

  query(request: QueryRequest): Observable<QueryResponse> {
    return this.http.post<QueryResponse>(API_CONFIG.endpoints.query, request);
  }

  getQueryLogs(userId?: string): Observable<QueryLog[]> {
    const params = userId ? new HttpParams().set('userId', userId) : undefined;
    return this.http.get<QueryLog[]>(API_CONFIG.endpoints.queryLogs, { params });
  }

  getModels(): Observable<LlmModelsResponse> {
    return this.http.get<LlmModelsResponse>(API_CONFIG.endpoints.models);
  }
}

