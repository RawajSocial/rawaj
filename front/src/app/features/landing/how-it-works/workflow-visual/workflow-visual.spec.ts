import { ComponentFixture, TestBed } from '@angular/core/testing';

import { WorkflowVisual } from './workflow-visual';

describe('WorkflowVisual', () => {
  let component: WorkflowVisual;
  let fixture: ComponentFixture<WorkflowVisual>;

  beforeEach(async () => {
    await TestBed.configureTestingModule({
      imports: [WorkflowVisual],
    }).compileComponents();

    fixture = TestBed.createComponent(WorkflowVisual);
    component = fixture.componentInstance;
    await fixture.whenStable();
  });

  it('should create', () => {
    expect(component).toBeTruthy();
  });
});
