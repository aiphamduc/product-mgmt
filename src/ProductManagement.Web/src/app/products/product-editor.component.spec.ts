import { FormBuilder } from '@angular/forms';
import { ProductEditorComponent } from './product-editor.component';

describe('ProductEditorComponent', () => {
  it('starts with one variant row and invalid required fields', () => {
    const component = new ProductEditorComponent(
      new FormBuilder(),
      {} as never,
      {} as never,
      {} as never,
    );

    expect(component.variants.length).toBe(1);
    expect(component.form.invalid).toBeTrue();
  });
});
