import { ComponentFixture, TestBed } from '@angular/core/testing';

import { AccountingAccountNode } from './accounting-account-node';

describe('AccountingAccountNode', () => {
  let component: AccountingAccountNode;
  let fixture: ComponentFixture<AccountingAccountNode>;

  beforeEach(async () => {
    await TestBed.configureTestingModule({
      imports: [AccountingAccountNode]
    })
    .compileComponents();

    fixture = TestBed.createComponent(AccountingAccountNode);
    component = fixture.componentInstance;
    fixture.detectChanges();
  });

  it('should create', () => {
    expect(component).toBeTruthy();
  });
});
