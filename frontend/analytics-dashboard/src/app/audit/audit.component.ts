import { Component, computed, inject, signal } from '@angular/core';
import { CommonModule } from '@angular/common';
import { FormsModule } from '@angular/forms';
import { TableModule } from 'primeng/table';
import { ButtonModule } from 'primeng/button';
import { InputTextModule } from 'primeng/inputtext';
import { TagModule } from 'primeng/tag';
import { AuditApi, AuditRecord, ChainVerification } from '../core/analytics.api';

@Component({
  selector: 'app-audit',
  imports: [CommonModule, FormsModule, TableModule, ButtonModule, InputTextModule, TagModule],
  template: `
    <div class="flex items-center justify-between mb-6">
      <div>
        <h2 class="text-2xl font-bold text-white">Audit Log</h2>
        <p class="text-sm text-white/60">Append-only, SHA-256 hash-chained · <code>audit-service</code></p>
      </div>
      <div class="flex gap-2">
        <p-button label="Verify Chain" icon="pi pi-check-circle" severity="warn" (onClick)="verify()" [loading]="verifying()" />
        <p-button label="Refresh" icon="pi pi-refresh" severity="secondary" (onClick)="load()" />
      </div>
    </div>

    <div *ngIf="verification() as v"
         class="mb-6 p-4 rounded-xl border"
         [class.bg-emerald-500\\/10]="v.ok"
         [class.border-emerald-500\\/30]="v.ok"
         [class.bg-rose-500\\/10]="!v.ok"
         [class.border-rose-500\\/30]="!v.ok">
      <i class="pi mr-2" [class.pi-check-circle]="v.ok" [class.pi-times-circle]="!v.ok"></i>
      <strong>{{ v.ok ? 'Chain intact' : 'CHAIN BROKEN' }}</strong> · checked {{ v.checkedCount }} record(s)
      <span *ngIf="v.failedId" class="text-rose-300 ml-2">first failure: {{ v.failedId }}</span>
    </div>

    <div class="grid grid-cols-4 gap-3 mb-4">
      <input pInputText [(ngModel)]="filter.actor" placeholder="actor" />
      <input pInputText [(ngModel)]="filter.resource" placeholder="resource" />
      <input pInputText [(ngModel)]="filter.action" placeholder="action" />
      <p-button label="Filter" icon="pi pi-filter" (onClick)="load()" />
    </div>

    <div class="bg-white/5 border border-white/10 rounded-xl overflow-hidden">
      <p-table [value]="records()" [loading]="loading()" styleClass="p-datatable-sm">
        <ng-template pTemplate="header">
          <tr>
            <th>Time</th><th>Actor</th><th>Action</th><th>Resource</th><th>Outcome</th><th>Source</th><th>Hash</th>
          </tr>
        </ng-template>
        <ng-template pTemplate="body" let-r>
          <tr>
            <td class="text-xs text-white/60">{{ r.recordedAtUtc | slice:0:19 }}</td>
            <td class="text-amber-300 font-medium">{{ r.actor }}</td>
            <td><p-tag [value]="r.action" severity="info" /></td>
            <td>{{ r.resource }}</td>
            <td>{{ r.outcome || '—' }}</td>
            <td class="text-xs text-white/50">{{ r.source }}</td>
            <td class="text-xs font-mono text-white/40">{{ r.hash | slice:0:12 }}…</td>
          </tr>
        </ng-template>
      </p-table>
    </div>
  `
})
export class AuditComponent {
  private api = inject(AuditApi);
  records = signal<AuditRecord[]>([]);
  loading = signal(false);
  verifying = signal(false);
  verification = signal<ChainVerification | null>(null);
  filter: { actor?: string; resource?: string; action?: string; take?: number } = { take: 200 };

  constructor() { this.load(); }

  load() {
    this.loading.set(true);
    this.api.list(this.filter).subscribe({
      next: r => { this.records.set(r); this.loading.set(false); },
      error: () => this.loading.set(false)
    });
  }

  verify() {
    this.verifying.set(true);
    this.api.verify().subscribe({
      next: v => { this.verification.set(v); this.verifying.set(false); },
      error: () => this.verifying.set(false)
    });
  }
}
