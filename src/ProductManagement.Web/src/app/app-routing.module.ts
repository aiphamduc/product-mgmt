import { NgModule } from '@angular/core';
import { RouterModule, Routes } from '@angular/router';
import { ProductListComponent } from './products/product-list.component';
import { ProductEditorComponent } from './products/product-editor.component';

const routes: Routes = [
  { path: 'products/new', component: ProductEditorComponent },
  { path: 'products/:id/edit', component: ProductEditorComponent },
  { path: '', component: ProductListComponent },
  { path: '**', redirectTo: '' },
];

@NgModule({
  imports: [RouterModule.forRoot(routes)],
  exports: [RouterModule],
})
export class AppRoutingModule {}
