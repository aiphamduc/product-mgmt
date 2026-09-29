import { Injectable } from '@angular/core';

@Injectable({ providedIn: 'root' })
export class AuthTokenService {
  private readonly storageKey = 'product-management.access-token';

  get token(): string | null {
    return localStorage.getItem(this.storageKey);
  }

  set token(value: string | null) {
    if (value) localStorage.setItem(this.storageKey, value);
    else localStorage.removeItem(this.storageKey);
  }
}
