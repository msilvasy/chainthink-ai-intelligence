import { Injectable, inject } from '@angular/core';
import { HttpClient, HttpEvent, HttpEventType } from '@angular/common/http';
import { Observable, BehaviorSubject } from 'rxjs';
import { map, tap } from 'rxjs/operators';
import { API_CONFIG } from '../config/api.config';
import { UploadDocumentResponse, DocumentInfo } from '../../models/document.models';
import { EmbeddingsService } from './embeddings.service';
import { QuerySettingsService } from '../../shared/services/query-settings.service';

@Injectable({
  providedIn: 'root'
})
export class DocumentService {
  private http = inject(HttpClient);
  private embeddingsService = inject(EmbeddingsService);
  private querySettingsService = inject(QuerySettingsService);
  private documents$ = new BehaviorSubject<DocumentInfo[]>([]);

  getDocuments(): Observable<DocumentInfo[]> {
    return this.documents$.asObservable();
  }

  loadDocuments(): Observable<DocumentInfo[]> {
    return this.http.get<DocumentInfo[]>(API_CONFIG.endpoints.documents).pipe(
      tap(documents => this.documents$.next(documents))
    );
  }

  clearCache(): void {
    this.documents$.next([]);
  }

  uploadDocument(file: File): Observable<{ progress: number; response?: UploadDocumentResponse }> {
    const formData = new FormData();
    formData.append('file', file);
    const querySettings = this.querySettingsService.current;
    formData.append('chunkSize', String(querySettings.chunkSize));
    formData.append('chunkOverlap', String(querySettings.chunkOverlap));

    return this.http.post<UploadDocumentResponse>(
      API_CONFIG.endpoints.upload,
      formData,
      {
        reportProgress: true,
        observe: 'events'
      }
    ).pipe(
      map((event: HttpEvent<UploadDocumentResponse>) => {
        switch (event.type) {
          case HttpEventType.UploadProgress:
            const progress = event.total
              ? Math.round((100 * event.loaded) / event.total)
              : 0;
            return { progress };
          case HttpEventType.Response:
            // Refresh embedded documents list after successful upload
            this.embeddingsService.getEmbeddedDocuments().subscribe();
            return { progress: 100, response: event.body! };
          default:
            return { progress: 0 };
        }
      })
    );
  }
}

