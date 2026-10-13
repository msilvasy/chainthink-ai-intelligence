import { HttpInterceptorFn } from '@angular/common/http';
import { API_CONFIG } from '../config/api.config';

export const apiInterceptor: HttpInterceptorFn = (req, next) => {
  // Only intercept requests that don't already have a full URL
  if (!req.url.startsWith('http://') && !req.url.startsWith('https://')) {
    const apiUrl = `${API_CONFIG.baseUrl}${req.url}`;
    req = req.clone({ url: apiUrl });
  }
  return next(req);
};

