import { HttpInterceptorFn, HttpErrorResponse } from '@angular/common/http';
import { inject } from '@angular/core';
import { catchError, throwError } from 'rxjs';
import { AuthService } from '../services/auth.service';
import { NotificationService } from '../../shared/services/notification.service';

export const errorInterceptor: HttpInterceptorFn = (req, next) => {
  const notificationService = inject(NotificationService);
  const authService = inject(AuthService);

  return next(req).pipe(
    catchError((error: HttpErrorResponse) => {
      const isLoginRequest = req.url.includes('/api/Auth/login');
      const isClientBlocked = error.error?.skipNotification === true;

      if (error.status === 401 && !isLoginRequest && !isClientBlocked) {
        if (authService.isLoggedIn()) {
          authService.logout();
          notificationService.showError('Session expired. Please log in again.');
        }
        return throwError(() => error);
      }

      const skipNotification = isLoginRequest || isClientBlocked;
      let errorMessage = 'An unexpected error occurred';

      if (error.error instanceof ErrorEvent) {
        // Client-side error
        errorMessage = error.error.message;
      } else {
        // Server-side error
        if (error.error?.message) {
          errorMessage = error.error.message;
        } else if (error.status === 0) {
          errorMessage = 'Unable to connect to the server. Please check your connection.';
        } else if (error.status >= 500) {
          errorMessage = 'Server error. Please try again later.';
        } else if (error.status === 404) {
          errorMessage = 'Resource not found.';
        } else if (error.status === 403) {
          errorMessage = 'Access forbidden.';
        } else if (error.status === 401) {
          errorMessage = 'Unauthorized. Please check your credentials.';
        }
      }

      if (!skipNotification) {
        notificationService.showError(errorMessage);
      }
      return throwError(() => error);
    })
  );
};

