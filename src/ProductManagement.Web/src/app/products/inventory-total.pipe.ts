import { Pipe, PipeTransform } from '@angular/core';
import { ProductVariant } from '../core/models/product';

@Pipe({ name: 'inventoryTotal' })
export class InventoryTotalPipe implements PipeTransform {
  transform(variants: ProductVariant[]): number {
    return variants.reduce((total, variant) => total + variant.stockOnHand, 0);
  }
}
