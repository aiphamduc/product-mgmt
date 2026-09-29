export type ProductStatus = 'Draft' | 'Published' | 'Archived';

export interface ProductVariant {
  id: string;
  sku: string;
  price: number;
  currency: string;
  stockOnHand: number;
}

export interface ProductAttribute {
  definitionId: string;
  value: string | null;
}

export interface Product {
  id: string;
  name: string;
  slug: string;
  description?: string | null;
  categoryId: string;
  status: ProductStatus;
  attributes: ProductAttribute[];
  variants: ProductVariant[];
  etag: string;
}

export interface ProductListResponse {
  items: Product[];
  nextCursor: string | null;
  limit: number;
}

export interface ProductQuery {
  categoryId?: string;
  status?: ProductStatus | '';
  q?: string;
  sort?: string;
  limit?: number;
  cursor?: string;
}

export interface ProductProblemDetails {
  title?: string;
  detail?: string;
  errors?: Record<string, string[]>;
}
