import { ComponentFixture, TestBed } from '@angular/core/testing';

import { Problems } from './problems';

describe('Problems', () => {
  let component: Problems;
  let fixture: ComponentFixture<Problems>;

  beforeEach(async () => {
    await TestBed.configureTestingModule({
      imports: [Problems],
    }).compileComponents();

    fixture = TestBed.createComponent(Problems);
    component = fixture.componentInstance;
    await fixture.whenStable();
  });

  it('should create', () => {
    expect(component).toBeTruthy();
  });
});
