import { HttpErrorResponse, HttpInterceptorFn } from '@angular/common/http';
import { inject } from '@angular/core';
import { throwError } from 'rxjs';
import { AuthService } from '../services/auth.service';

export const authInterceptor: HttpInterceptorFn = (req, next) => {
  if (req.url.includes('/api/Auth/login')) {
    return next(req);
  }

  const token = inject(AuthService).getToken();
  if (!token) {
    return throwError(() => new HttpErrorResponse({
      status: 401,
      statusText: 'Not authenticated',
      error: { message: 'Not authenticated', skipNotification: true }
    }));
  }

  return next(req.clone({
    setHeaders: {
      Authorization: `Bearer ${token}`
    }
  }));
};
