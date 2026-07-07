import { ComponentFixture, TestBed } from '@angular/core/testing';

import { WhyCard } from './why-card';

describe('WhyCard', () => {
  let component: WhyCard;
  let fixture: ComponentFixture<WhyCard>;

  beforeEach(async () => {
    await TestBed.configureTestingModule({
      imports: [WhyCard],
    }).compileComponents();

    fixture = TestBed.createComponent(WhyCard);
    component = fixture.componentInstance;
    await fixture.whenStable();
  });

  it('should create', () => {
    expect(component).toBeTruthy();
  });
});
