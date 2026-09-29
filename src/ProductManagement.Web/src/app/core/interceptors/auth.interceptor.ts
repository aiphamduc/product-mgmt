import { Injectable } from '@angular/core';
import {
  HttpEvent,
  HttpHandler,
  HttpInterceptor,
  HttpRequest,
} from '@angular/common/http';
import { Observable } from 'rxjs';
import { AuthTokenService } from '../services/auth-token.service';

@Injectable()
export class AuthInterceptor implements HttpInterceptor {
  constructor(private readonly authToken: AuthTokenService) {}

  intercept(
    request: HttpRequest<unknown>,
    next: HttpHandler,
  ): Observable<HttpEvent<unknown>> {
    const token = this.authToken.token;
    return next.handle(
      token
        ? request.clone({ setHeaders: { Authorization: `Bearer ${token}` } })
        : request,
    );
  }
}
