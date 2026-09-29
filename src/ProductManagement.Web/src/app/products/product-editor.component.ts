import { Component, OnInit } from '@angular/core';
import { FormArray, FormBuilder, FormGroup, Validators } from '@angular/forms';
import { ActivatedRoute, Router } from '@angular/router';
import { finalize } from 'rxjs';
import { Product, ProductStatus } from '../core/models/product';
import { ProductApiService } from '../core/services/product-api.service';

@Component({
  selector: 'app-product-editor',
  templateUrl: './product-editor.component.html',
  styleUrls: ['./product-editor.component.scss'],
})
export class ProductEditorComponent implements OnInit {
  readonly form: FormGroup;
  readonly statuses: ProductStatus[] = ['Draft', 'Published', 'Archived'];
  product: Product | null = null;
  productId: string | null = null;
  etag = '';
  isLoading = false;
  isSaving = false;
  errorMessage = '';
  conflictMessage = '';
  stockMessage = '';
  serverErrors: Record<string, string[]> = {};
  selectedVariantIndex = 0;

  get attributes(): FormArray {
    return this.form.get('attributes') as FormArray;
  }
  get variants(): FormArray {
    return this.form.get('variants') as FormArray;
  }
  get isNew(): boolean {
    return !this.productId;
  }

  constructor(
    private readonly formBuilder: FormBuilder,
    private readonly productApi: ProductApiService,
    private readonly route: ActivatedRoute,
    private readonly router: Router,
  ) {
    this.form = this.formBuilder.group({
      name: ['', [Validators.required, Validators.maxLength(200)]],
      slug: [
        '',
        [Validators.required, Validators.pattern(/^[a-z0-9]+(?:-[a-z0-9]+)*$/)],
      ],
      description: ['', Validators.maxLength(10000)],
      categoryId: ['', Validators.required],
      attributes: this.formBuilder.array([]),
      variants: this.formBuilder.array([this.variantGroup()]),
    });
  }

  ngOnInit(): void {
    this.productId = this.route.snapshot.paramMap.get('id');
    if (this.productId) this.loadProduct(this.productId);
  }

  private variantGroup(value?: {
    sku: string;
    price: number;
    currency: string;
    stock: number;
  }): FormGroup {
    return this.formBuilder.group({
      sku: [value?.sku || '', Validators.required],
      price: [value?.price ?? 0, [Validators.required, Validators.min(0)]],
      currency: [value?.currency || 'USD', Validators.required],
      stock: [value?.stock ?? 0, [Validators.required, Validators.min(0)]],
    });
  }

  private attributeGroup(definitionId = '', value = ''): FormGroup {
    return this.formBuilder.group({
      definitionId: [definitionId, Validators.required],
      value: [value, Validators.required],
    });
  }

  loadProduct(id: string): void {
    this.isLoading = true;
    this.productApi
      .get(id)
      .pipe(finalize(() => (this.isLoading = false)))
      .subscribe({
        next: (response) => {
          this.product = response.body;
          this.etag = response.headers.get('ETag') || response.body?.etag || '';
          if (!this.product) return;
          this.form.patchValue({
            name: this.product.name,
            slug: this.product.slug,
            description: this.product.description || '',
            categoryId: this.product.categoryId,
          });
          this.attributes.clear();
          this.product.attributes.forEach((attribute) =>
            this.attributes.push(
              this.attributeGroup(
                attribute.definitionId,
                attribute.value || '',
              ),
            ),
          );
          this.variants.clear();
          this.product.variants.forEach((variant) =>
            this.variants.push(
              this.variantGroup({
                sku: variant.sku,
                price: variant.price,
                currency: variant.currency,
                stock: variant.stockOnHand,
              }),
            ),
          );
        },
        error: (error) => (this.errorMessage = this.productApi.problem(error)),
      });
  }

  addAttribute(): void {
    this.attributes.push(this.attributeGroup());
  }
  removeAttribute(index: number): void {
    this.attributes.removeAt(index);
  }
  addVariant(): void {
    this.variants.push(this.variantGroup());
  }
  removeVariant(index: number): void {
    if (this.variants.length > 1) this.variants.removeAt(index);
  }

  save(): void {
    this.form.markAllAsTouched();
    if (this.form.invalid || this.isSaving) return;
    this.isSaving = true;
    this.errorMessage = '';
    this.serverErrors = {};
    const value = this.form.getRawValue();
    const body = {
      name: value.name,
      slug: value.slug,
      description: value.description,
      categoryId: value.categoryId,
      attributes: value.attributes,
      variants: value.variants.map(
        (variant: {
          sku: string;
          price: number;
          currency: string;
          stock: number;
        }) => ({
          ...variant,
          sku: variant.sku.toUpperCase(),
          currency: variant.currency.toUpperCase(),
        }),
      ),
    };
    const request = this.isNew
      ? this.productApi.create(body)
      : this.productApi.update(this.productId!, body, this.etag);
    request.pipe(finalize(() => (this.isSaving = false))).subscribe({
      next: (response) => {
        this.product = response.body;
        this.etag =
          response.headers.get('ETag') || response.body?.etag || this.etag;
        this.router.navigate([
          '/products',
          response.body?.id || this.productId,
          'edit',
        ]);
      },
      error: (error) => this.handleMutationError(error),
    });
  }

  changeStatus(status: ProductStatus): void {
    if (
      !this.productId ||
      !this.etag ||
      !confirm(`Change this product to ${status}?`)
    )
      return;
    this.productApi.status(this.productId, status, this.etag).subscribe({
      next: (response) => {
        this.product = response.body;
        this.etag = response.headers.get('ETag') || this.etag;
      },
      error: (error) => this.handleMutationError(error),
    });
  }

  adjustStock(): void {
    if (!this.product || !this.variants.length) return;
    const variant = this.product.variants[this.selectedVariantIndex];
    const rawQuantity = window.prompt(
      'Adjustment quantity (negative values reduce stock):',
      '1',
    );
    const quantity = Number(rawQuantity);
    if (!rawQuantity || !Number.isInteger(quantity) || quantity === 0) return;
    const reason = window.prompt(
      'Reason for this adjustment:',
      'Manual management adjustment',
    );
    if (!reason) return;
    this.productApi.adjustStock(variant.id, { quantity, reason }).subscribe({
      next: (result) => {
        this.stockMessage = `${result.newStock} units on hand. ${result.wasReplay ? 'Existing idempotent request replayed.' : 'Adjustment recorded.'}`;
        this.loadProduct(this.productId!);
      },
      error: (error) => (this.stockMessage = this.productApi.problem(error)),
    });
  }

  backToList(): void {
    this.router.navigate(['/']);
  }

  private handleMutationError(error: { status?: number }): void {
    if (error.status === 409 || error.status === 412) {
      this.conflictMessage =
        'This product changed in another session. Reload the latest version before trying again.';
    } else if (
      error.status === 400 &&
      (error as { error?: { errors?: Record<string, string[]> } }).error?.errors
    ) {
      this.serverErrors = (
        error as { error: { errors: Record<string, string[]> } }
      ).error.errors;
      this.errorMessage = 'Review the highlighted fields and try again.';
    } else {
      this.errorMessage = this.productApi.problem(error);
    }
  }

  fieldError(name: string): string {
    const key = Object.keys(this.serverErrors).find(
      (item) => item.toLowerCase() === name.toLowerCase(),
    );
    return key ? this.serverErrors[key][0] : '';
  }
}
