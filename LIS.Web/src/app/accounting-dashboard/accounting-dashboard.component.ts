import { AfterViewInit, Component, ElementRef, OnDestroy, ViewChildren, QueryList, inject } from '@angular/core';
import { CommonModule } from '@angular/common';
import { Chart, ChartConfiguration } from 'chart.js/auto';
import { AccountingService } from '../services/accounting.service';

@Component({
  selector: 'app-accounting-dashboard',
  standalone: true,
  imports: [CommonModule],
  templateUrl: './accounting-dashboard.component.html',
  styleUrl: './accounting-dashboard.component.scss'
})
export class AccountingDashboardComponent implements AfterViewInit, OnDestroy {
  @ViewChildren('chartCanvas') private readonly chartCanvases!: QueryList<ElementRef<HTMLCanvasElement>>;

  private charts: Chart[] = [];
  private accountingService = inject(AccountingService);
  isGridView = true;
  dailyCashRows: { department: string; lbp: number; usd: number }[] = [];
  totalLBP = 0;
  totalUSD = 0;
  incomeRows: { accountCode: string; accountDescription: string; lbp: number; usd: number }[] = [];
  incomeTotalLBP = 0;
  incomeTotalUSD = 0;

  // Simple plugin to draw value labels on top of bars
  private readonly valueOnTopPlugin = {
    id: 'valueOnTop',
    afterDatasetsDraw: (chart: any, _args: any, pluginOptions: any) => {
      const { ctx } = chart;
      ctx.save();
      const formatter: (v: number) => string = pluginOptions?.formatter ?? ((v: number) => String(v));
      chart.data.datasets.forEach((dataset: any, datasetIndex: number) => {
        const meta = chart.getDatasetMeta(datasetIndex);
        if (!meta || meta.type !== 'bar') return;
        meta.data.forEach((bar: any, index: number) => {
          const value = dataset.data[index];
          if (value == null) return;
          const label = formatter(Number(value));
          const pos = bar.tooltipPosition();
          ctx.font = '12px sans-serif';
          ctx.fillStyle = '#111827';
          ctx.textAlign = 'center';
          ctx.textBaseline = 'bottom';
          ctx.fillText(label, pos.x, pos.y - 4);
        });
      });
      ctx.restore();
    }
  };
  private static pluginRegistered = false;

  ngAfterViewInit(): void {
    if (!AccountingDashboardComponent.pluginRegistered) {
      Chart.register(this.valueOnTopPlugin as any);
      AccountingDashboardComponent.pluginRegistered = true;
    }
    // Initialize charts if canvases are present now and on future changes
    this.initChartsIfNeeded();
    this.chartCanvases.changes.subscribe(() => this.initChartsIfNeeded());

    // Load data for first chart: Daily cash by department
    this.accountingService.getDailyCashByDepartment().subscribe(data => {
      this.dailyCashRows = data;
      this.totalLBP = this.dailyCashRows.reduce((s, r) => s + (r.lbp || 0), 0);
      this.totalUSD = this.dailyCashRows.reduce((s, r) => s + (r.usd || 0), 0);
      this.bindDailyCashToFirstChart();
    });

    // Load data for second grid: Current month income by account
    this.accountingService.getCurrentMonthIncomeByAccount().subscribe(data => {
      this.incomeRows = data;
      this.incomeTotalLBP = this.incomeRows.reduce((s, r) => s + (r.lbp || 0), 0);
      this.incomeTotalUSD = this.incomeRows.reduce((s, r) => s + (r.usd || 0), 0);
      this.bindIncomeToSecondChart();
    });
  }

  toggleView(): void {
    this.isGridView = !this.isGridView;
    if (!this.isGridView) {
      // switched to charts view
      setTimeout(() => {
        this.destroyCharts();
        this.initChartsIfNeeded();
        this.bindDailyCashToFirstChart();
      });
    }
  }

  ngOnDestroy(): void {
    this.destroyCharts();
  }

  private buildBarConfig(title: string): ChartConfiguration<'bar'> {
    return {
      type: 'bar',
      data: {
        labels: ['Jan', 'Feb', 'Mar', 'Apr', 'May', 'Jun'],
        datasets: [
          { label: title, data: [12, 19, 3, 5, 2, 3] }
        ]
      },
      options: {
        responsive: true,
        plugins: { legend: { display: true }, title: { display: true, text: title } }
      }
    };
  }

  private buildLineConfig(title: string): ChartConfiguration<'line'> {
    return {
      type: 'line',
      data: {
        labels: ['Jan', 'Feb', 'Mar', 'Apr', 'May', 'Jun'],
        datasets: [
          { label: title, data: [5, 9, 7, 11, 15, 13], fill: false }
        ]
      },
      options: {
        responsive: true,
        plugins: { legend: { display: true }, title: { display: true, text: title } }
      }
    };
  }

  private buildDoughnutConfig(title: string): ChartConfiguration<'doughnut'> {
    return {
      type: 'doughnut',
      data: {
        labels: ['Lab', 'Radiology', 'Pharmacy'],
        datasets: [
          { label: title, data: [55, 25, 20] }
        ]
      },
      options: {
        responsive: true,
        plugins: { legend: { position: 'bottom' }, title: { display: true, text: title } }
      }
    };
  }

  private buildPieConfig(title: string): ChartConfiguration<'pie'> {
    return {
      type: 'pie',
      data: {
        labels: ['Salaries', 'Supplies', 'Maintenance', 'Other'],
        datasets: [
          { label: title, data: [40, 30, 20, 10] }
        ]
      },
      options: {
        responsive: true,
        plugins: { legend: { position: 'bottom' }, title: { display: true, text: title } }
      }
    };
  }

  private initChartsIfNeeded(): void {
    if (!this.chartCanvases || this.chartCanvases.length === 0) return;

    // Always rebuild charts for current canvases to handle view toggles
    this.destroyCharts();

    this.chartCanvases.forEach((canvasRef, index) => {
      const ctx = canvasRef.nativeElement.getContext('2d');
      if (!ctx) return;
      let config: ChartConfiguration;
      if (index === 0) {
        config = this.buildBarConfig('Daily Cash by Department (USD)');
      } else if (index === 1) {
        config = this.buildBarConfig('Daily Cash by Department (LBP)');
      } else if (index === 2) {
        config = this.buildBarConfig('Current Month Income by Account (USD)');
      } else if (index === 3) {
        config = this.buildBarConfig('Current Month Income by Account (LBP)');
      } else if (index === 4) {
        config = this.buildDoughnutConfig('Revenue by Department');
      } else if (index === 5) {
        config = this.buildPieConfig('Expense Breakdown');
      } else {
        config = this.buildLineConfig('Cash Flow');
      }
      const chart = new Chart(ctx, config);
      this.charts.push(chart);
    });

    this.bindDailyCashToFirstChart();
  }

  private bindDailyCashToFirstChart(): void {
    const usdChart = this.charts[0];
    const lbpChart = this.charts[1];
    if (!usdChart || !lbpChart || !this.dailyCashRows || this.dailyCashRows.length === 0) return;
    const labels = this.dailyCashRows.map(d => d.department);
    const lbp = this.dailyCashRows.map(d => d.lbp);
    const usd = this.dailyCashRows.map(d => d.usd);

    usdChart.data.labels = labels;
    usdChart.data.datasets = [
      { label: 'USD', data: usd, backgroundColor: 'rgba(255, 206, 86, 0.6)' }
    ];
    usdChart.options = {
      responsive: true,
      layout: { padding: { top: 20 } },
      plugins: {
        legend: { display: true },
        title: { display: true, text: 'Daily Cash by Department (USD)' },
        // pass formatter for USD
        valueOnTop: { formatter: (v: number) => new Intl.NumberFormat('en-US', { minimumFractionDigits: 2, maximumFractionDigits: 2 }).format(v) }
      } as any
    };
    usdChart.update();

    lbpChart.data.labels = labels;
    lbpChart.data.datasets = [
      { label: 'LBP', data: lbp, backgroundColor: 'rgba(54, 162, 235, 0.6)' }
    ];
    lbpChart.options = {
      responsive: true,
      layout: { padding: { top: 20 } },
      plugins: {
        legend: { display: true },
        title: { display: true, text: 'Daily Cash by Department (LBP)' },
        // pass formatter for LBP
        valueOnTop: { formatter: (v: number) => new Intl.NumberFormat('en-US', { maximumFractionDigits: 0 }).format(v) }
      } as any
    };
    lbpChart.update();
  }

  private bindIncomeToSecondChart(): void {
    const usdChart = this.charts[2];
    const lbpChart = this.charts[3];
    if (!usdChart || !lbpChart || !this.incomeRows || this.incomeRows.length === 0) return;
    const labels = this.incomeRows.map(d => d.accountDescription || d.accountCode);
    const lbp = this.incomeRows.map(d => d.lbp);
    const usd = this.incomeRows.map(d => d.usd);

    usdChart.data.labels = labels;
    usdChart.data.datasets = [
      { label: 'USD', data: usd, backgroundColor: 'rgba(75, 192, 192, 0.6)' }
    ];
    usdChart.options = {
      responsive: true,
      layout: { padding: { top: 20 } },
      plugins: {
        legend: { display: true },
        title: { display: true, text: 'Current Month Income by Account (USD)' },
        valueOnTop: { formatter: (v: number) => new Intl.NumberFormat('en-US', { minimumFractionDigits: 2, maximumFractionDigits: 2 }).format(v) }
      } as any
    };
    usdChart.update();

    lbpChart.data.labels = labels;
    lbpChart.data.datasets = [
      { label: 'LBP', data: lbp, backgroundColor: 'rgba(153, 102, 255, 0.6)' }
    ];
    lbpChart.options = {
      responsive: true,
      layout: { padding: { top: 20 } },
      plugins: {
        legend: { display: true },
        title: { display: true, text: 'Current Month Income by Account (LBP)' },
        valueOnTop: { formatter: (v: number) => new Intl.NumberFormat('en-US', { maximumFractionDigits: 0 }).format(v) }
      } as any
    };
    lbpChart.update();
  }

  private destroyCharts(): void {
    if (this.charts && this.charts.length) {
      this.charts.forEach(c => {
        try { c.destroy(); } catch {}
      });
    }
    this.charts = [];
  }
}


