import { Pipe, PipeTransform } from '@angular/core';
import { ProductVariant } from '../core/models/product';

@Pipe({ name: 'lowestPrice' })
export class LowestPricePipe implements PipeTransform {
  transform(variants: ProductVariant[]): number {
    return variants.length
      ? Math.min(...variants.map((variant) => variant.price))
      : 0;
  }
}
