import { Component, OnInit } from '@angular/core';
import { FormBuilder, FormGroup } from '@angular/forms';
import { finalize } from 'rxjs';
import {
  Product,
  ProductListResponse,
  ProductStatus,
} from '../core/models/product';
import { ProductApiService } from '../core/services/product-api.service';

@Component({
  selector: 'app-product-list',
  templateUrl: './product-list.component.html',
  styleUrls: ['./product-list.component.scss'],
})
export class ProductListComponent implements OnInit {
  readonly filters: FormGroup;
  readonly statuses: ProductStatus[] = ['Draft', 'Published', 'Archived'];
  readonly pageSize = 10;
  products: Product[] = [];
  nextCursor: string | null = null;
  isLoading = false;
  errorMessage = '';
  hasLoaded = false;

  constructor(
    private readonly formBuilder: FormBuilder,
    private readonly productApi: ProductApiService,
  ) {
    this.filters = this.formBuilder.group({
      q: [''],
      status: [''],
      categoryId: [''],
      sort: ['updatedUtc:desc'],
    });
  }

  ngOnInit(): void {
    this.load();
  }

  applyFilters(): void {
    this.load();
  }

  clearFilters(): void {
    this.filters.reset({
      q: '',
      status: '',
      categoryId: '',
      sort: 'updatedUtc:desc',
    });
    this.load();
  }

  load(cursor?: string): void {
    this.isLoading = true;
    this.errorMessage = '';
    const query = { ...this.filters.value, limit: this.pageSize, cursor };
    this.productApi
      .list(query)
      .pipe(
        finalize(() => {
          this.isLoading = false;
          this.hasLoaded = true;
        }),
      )
      .subscribe({
        next: (response: ProductListResponse) => {
          this.products = response.items;
          this.nextCursor = response.nextCursor;
        },
        error: (error: unknown) => {
          this.products = [];
          this.nextCursor = null;
          this.errorMessage = this.productApi.problem(error);
        },
      });
  }

  trackById(_: number, product: Product): string {
    return product.id;
  }

  statusClass(status: ProductStatus): string {
    return `status-${status.toLowerCase()}`;
  }
}
