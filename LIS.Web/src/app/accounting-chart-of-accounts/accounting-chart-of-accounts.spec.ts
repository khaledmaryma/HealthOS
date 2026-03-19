import { ComponentFixture, TestBed } from '@angular/core/testing';

import { AccountingChartOfAccounts } from './accounting-chart-of-accounts';

describe('AccountingChartOfAccounts', () => {
  let component: AccountingChartOfAccounts;
  let fixture: ComponentFixture<AccountingChartOfAccounts>;

  beforeEach(async () => {
    await TestBed.configureTestingModule({
      imports: [AccountingChartOfAccounts]
    })
    .compileComponents();

    fixture = TestBed.createComponent(AccountingChartOfAccounts);
    component = fixture.componentInstance;
    fixture.detectChanges();
  });

  it('should create', () => {
    expect(component).toBeTruthy();
  });
});
