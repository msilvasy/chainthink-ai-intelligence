import { Injectable, inject } from '@angular/core';
import { HttpClient } from '@angular/common/http';
import { Observable, Subject, tap } from 'rxjs';
import { API_CONFIG } from '../config/api.config';
import { DocumentService } from './document.service';
import { AuthUser, LoginRequest, LoginResponse, QuerySettings } from '../../models/auth.models';
import { QuerySettingsService } from '../../shared/services/query-settings.service';

const STORAGE_KEY = 'auth_user';

@Injectable({
  providedIn: 'root'
})
export class AuthService {
  private http = inject(HttpClient);
  private documentService = inject(DocumentService);
  private querySettingsService = inject(QuerySettingsService);
  private currentUser: AuthUser | null = null;
  private authChanged = new Subject<boolean>();

  authChanged$ = this.authChanged.asObservable();

  constructor() {
    this.loadFromStorage();
    if (this.currentUser?.querySettings) {
      this.querySettingsService.update(this.currentUser.querySettings);
    }
  }

  login(credentials: LoginRequest): Observable<LoginResponse> {
    return this.http.post<LoginResponse>(API_CONFIG.endpoints.auth.login, credentials).pipe(
      tap(response => {
        if (response.success) {
          this.saveUser(response);
          this.authChanged.next(true);
        }
      })
    );
  }

  logout(): void {
    this.currentUser = null;
    localStorage.removeItem(STORAGE_KEY);
    this.documentService.clearCache();
    this.querySettingsService.reset();
    this.authChanged.next(false);
  }

  isLoggedIn(): boolean {
    return !!this.currentUser?.token;
  }

  getToken(): string | null {
    return this.currentUser?.token ?? null;
  }

  getUser(): AuthUser | null {
    return this.currentUser;
  }

  updateQuerySettings(querySettings: QuerySettings): void {
    if (!this.currentUser) {
      return;
    }

    this.currentUser = { ...this.currentUser, querySettings };
    localStorage.setItem(STORAGE_KEY, JSON.stringify(this.currentUser));
  }

  private saveUser(response: LoginResponse): void {
    this.currentUser = {
      token: response.token,
      email: response.email,
      userId: response.userId,
      firstName: response.firstName,
      lastName: response.lastName,
      querySettings: response.querySettings
    };
    this.querySettingsService.update(response.querySettings);
    localStorage.setItem(STORAGE_KEY, JSON.stringify(this.currentUser));
  }

  private loadFromStorage(): void {
    const stored = localStorage.getItem(STORAGE_KEY);
    if (!stored) {
      return;
    }

    try {
      this.currentUser = JSON.parse(stored);
    } catch {
      this.currentUser = null;
      localStorage.removeItem(STORAGE_KEY);
    }
  }
}
