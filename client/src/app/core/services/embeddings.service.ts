import { Injectable, inject } from '@angular/core';
import { HttpClient } from '@angular/common/http';
import { Observable } from 'rxjs';
import { API_CONFIG } from '../config/api.config';
import { EmbeddedDocument, EmbeddedChunk } from '../../models/embeddings.models';

@Injectable({
  providedIn: 'root'
})
export class EmbeddingsService {
  private http = inject(HttpClient);

  getEmbeddedDocuments(): Observable<EmbeddedDocument[]> {
    return this.http.get<EmbeddedDocument[]>(API_CONFIG.endpoints.embeddings.documents);
  }

  getDocumentChunks(documentId: string): Observable<EmbeddedChunk[]> {
    return this.http.get<EmbeddedChunk[]>(API_CONFIG.endpoints.embeddings.documentChunks(documentId));
  }

  deleteDocumentEmbeddings(documentId: string): Observable<void> {
    return this.http.delete<void>(API_CONFIG.endpoints.embeddings.deleteDocument(documentId));
  }

  deleteChunk(chunkId: string): Observable<void> {
    return this.http.delete<void>(API_CONFIG.endpoints.embeddings.deleteChunk(chunkId));
  }
}

