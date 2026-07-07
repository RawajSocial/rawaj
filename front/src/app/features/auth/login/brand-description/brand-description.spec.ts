import { ComponentFixture, TestBed } from '@angular/core/testing';

import { BrandDescription } from './brand-description';

describe('BrandDescription', () => {
  let component: BrandDescription;
  let fixture: ComponentFixture<BrandDescription>;

  beforeEach(async () => {
    await TestBed.configureTestingModule({
      imports: [BrandDescription],
    }).compileComponents();

    fixture = TestBed.createComponent(BrandDescription);
    component = fixture.componentInstance;
    await fixture.whenStable();
  });

  it('should create', () => {
    expect(component).toBeTruthy();
  });
});
