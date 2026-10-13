import { Injectable, inject } from '@angular/core';
import { HttpClient } from '@angular/common/http';
import { Observable, tap } from 'rxjs';
import { API_CONFIG } from '../../core/config/api.config';
import { QuerySettings } from '../../models/auth.models';

export type QueryMode = 'fast' | 'balanced' | 'accurate';

const MODE_SETTINGS: Record<QueryMode, QuerySettings> = {
  fast: { mode: 'fast', topK: 3, chunkSize: 256, chunkOverlap: 25 },
  balanced: { mode: 'balanced', topK: 5, chunkSize: 512, chunkOverlap: 50 },
  accurate: { mode: 'accurate', topK: 10, chunkSize: 768, chunkOverlap: 100 }
};

@Injectable({ providedIn: 'root' })
export class QuerySettingsService {
  private http = inject(HttpClient);
  private settings: QuerySettings = { ...MODE_SETTINGS.balanced };

  get current(): QuerySettings {
    return { ...this.settings };
  }

  getModeSettings(mode: QueryMode): QuerySettings {
    return { ...MODE_SETTINGS[mode] };
  }

  update(settings: QuerySettings): void {
    this.settings = { ...settings };
  }

  save(settings: QuerySettings): Observable<QuerySettings> {
    return this.http.post<QuerySettings>(API_CONFIG.endpoints.userSettings.save, settings).pipe(
      tap(savedSettings => this.update(savedSettings))
    );
  }

  reset(): void {
    this.settings = { ...MODE_SETTINGS.balanced };
  }
}
