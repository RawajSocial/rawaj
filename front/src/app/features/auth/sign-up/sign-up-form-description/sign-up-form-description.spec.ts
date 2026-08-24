import { ComponentFixture, TestBed } from '@angular/core/testing';

import { SignUpFormDescription } from './sign-up-form-description';

describe('SignUpFormDescription', () => {
  let component: SignUpFormDescription;
  let fixture: ComponentFixture<SignUpFormDescription>;

  beforeEach(async () => {
    await TestBed.configureTestingModule({
      imports: [SignUpFormDescription],
    }).compileComponents();

    fixture = TestBed.createComponent(SignUpFormDescription);
    component = fixture.componentInstance;
    await fixture.whenStable();
  });

  it('should create', () => {
    expect(component).toBeTruthy();
  });
});
