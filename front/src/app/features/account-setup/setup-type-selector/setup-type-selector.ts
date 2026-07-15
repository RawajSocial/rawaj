import { Component, output } from '@angular/core';

@Component({
  selector: 'app-setup-type-selector',
  imports: [],
  templateUrl: './setup-type-selector.html',
  styleUrl: './setup-type-selector.css',
})
export class SetupTypeSelector {
  readonly typeSelected = output<'agency' | 'business'>();

  protected select(type: 'agency' | 'business'): void {
    this.typeSelected.emit(type);
  }
}
