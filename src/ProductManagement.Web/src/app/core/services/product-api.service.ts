import {
  HttpClient,
  HttpHeaders,
  HttpParams,
  HttpResponse,
} from '@angular/common/http';
import { Injectable } from '@angular/core';
import { Observable } from 'rxjs';
import { environment } from '../../../environments/environment';
import {
  Product,
  ProductListResponse,
  ProductProblemDetails,
  ProductQuery,
} from '../models/product';

@Injectable({ providedIn: 'root' })
export class ProductApiService {
  private readonly baseUrl = `${environment.apiUrl}/api/v1/products`;

  constructor(private readonly http: HttpClient) {}

  list(query: ProductQuery): Observable<ProductListResponse> {
    let params = new HttpParams();
    Object.entries(query).forEach(([key, value]) => {
      if (value !== undefined && value !== null && value !== '')
        params = params.set(key, String(value));
    });
    return this.http.get<ProductListResponse>(this.baseUrl, { params });
  }

  get(id: string): Observable<HttpResponse<Product>> {
    return this.http.get<Product>(`${this.baseUrl}/${id}`, {
      observe: 'response',
    });
  }

  update(
    id: string,
    body: unknown,
    etag: string,
  ): Observable<HttpResponse<Product>> {
    return this.http.put<Product>(`${this.baseUrl}/${id}`, body, {
      observe: 'response',
      headers: new HttpHeaders({ 'If-Match': etag }),
    });
  }

  create(body: unknown): Observable<HttpResponse<Product>> {
    return this.http.post<Product>(this.baseUrl, body, { observe: 'response' });
  }

  status(
    id: string,
    status: string,
    etag: string,
  ): Observable<HttpResponse<Product>> {
    return this.http.patch<Product>(
      `${this.baseUrl}/${id}/status`,
      { status },
      {
        observe: 'response',
        headers: new HttpHeaders({ 'If-Match': etag }),
      },
    );
  }

  adjustStock(
    variantId: string,
    body: { quantity: number; reason: string },
  ): Observable<{
    adjustmentId: string;
    variantId: string;
    previousStock: number;
    newStock: number;
    wasReplay: boolean;
  }> {
    const idempotencyKey = `${variantId}-${Date.now()}-${Math.random().toString(36).slice(2)}`;
    return this.http.post<{
      adjustmentId: string;
      variantId: string;
      previousStock: number;
      newStock: number;
      wasReplay: boolean;
    }>(
      `${environment.apiUrl}/api/v1/variants/${variantId}/stock-adjustments`,
      body,
      { headers: new HttpHeaders({ 'Idempotency-Key': idempotencyKey }) },
    );
  }

  problem(error: unknown): string {
    const response = error as { error?: ProductProblemDetails };
    return (
      response.error?.detail ||
      response.error?.title ||
      'The API could not complete the request.'
    );
  }
}
