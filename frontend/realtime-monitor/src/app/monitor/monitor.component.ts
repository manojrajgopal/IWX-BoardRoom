import { Component, OnInit, computed, inject, signal } from '@angular/core';
import { CommonModule } from '@angular/common';
import { TagModule } from 'primeng/tag';
import { ButtonModule } from 'primeng/button';
import { MonitorHubService } from '../core/monitor-hub.service';

@Component({
  selector: 'app-monitor',
  imports: [CommonModule, TagModule, ButtonModule],
  template: `
    <header class="border-b border-white/10 px-8 py-5 flex items-center justify-between">
      <div>
        <h1 class="text-2xl font-bold text-cyan-300">IWX Realtime Monitor</h1>
        <p class="text-sm text-white/60">Live event stream via SignalR → API Gateway → boardroom hub</p>
      </div>
      <div class="flex items-center gap-3">
        <span class="flex items-center gap-2 text-sm">
          <span class="w-2.5 h-2.5 rounded-full" [class.bg-emerald-400]="hub.connected()" [class.bg-rose-500]="!hub.connected()"></span>
          {{ hub.connected() ? 'live' : 'disconnected' }}
        </span>
        <p-button label="Clear" icon="pi pi-trash" severity="secondary" size="small" (onClick)="clear()" />
      </div>
    </header>

    <section class="grid grid-cols-12 gap-6 p-8">
      <div class="col-span-12 lg:col-span-3 space-y-3">
        <div *ngFor="let c of categories()" class="bg-white/5 border border-white/10 rounded-xl p-4">
          <div class="flex items-center justify-between mb-1">
            <span class="text-xs uppercase tracking-wider text-white/50">{{ c.label }}</span>
            <span class="text-2xl font-bold text-cyan-300">{{ c.count }}</span>
          </div>
          <p class="text-xs text-white/40">{{ c.types.join(', ') }}</p>
        </div>
      </div>

      <div class="col-span-12 lg:col-span-9 bg-white/5 border border-white/10 rounded-xl overflow-hidden">
        <div class="px-4 py-3 border-b border-white/10 text-sm font-semibold text-white/80">
          Event stream <span class="text-white/40">({{ hub.events().length }})</span>
        </div>
        <ul class="divide-y divide-white/5 max-h-[70vh] overflow-auto">
          <li *ngFor="let e of hub.events()" class="px-4 py-2.5 hover:bg-white/5 flex items-center gap-3 text-sm">
            <p-tag [value]="e.type" [severity]="severityFor(e.type)" />
            <span class="text-white/40 text-xs w-24">{{ e.receivedAtUtc | slice:11:19 }}</span>
            <pre class="flex-1 text-xs font-mono text-white/70 overflow-x-auto whitespace-pre-wrap break-all m-0">{{ stringify(e.payload) }}</pre>
          </li>
          <li *ngIf="hub.events().length === 0" class="px-4 py-12 text-center text-white/40">
            <i class="pi pi-wave-pulse text-4xl mb-3 block"></i>
            Waiting for events…
          </li>
        </ul>
      </div>
    </section>
  `
})
export class MonitorComponent implements OnInit {
  hub = inject(MonitorHubService);

  categories = computed(() => {
    const ev = this.hub.events();
    const buckets = [
      { label: 'CEO / Tasks',  types: ['TaskCreated','TaskApproved','TaskCompleted'] },
      { label: 'Automation',   types: ['WorkflowStepDispatched','SchedulerTick','TaskNodeReady'] },
      { label: 'Approvals',    types: ['ApprovalRequested','ApprovalDecided'] },
      { label: 'Security',     types: ['ThreatDetected','AuthIssued','AccessDenied'] }
    ];
    return buckets.map(b => ({ ...b, count: ev.filter(e => b.types.includes(e.type)).length }));
  });

  async ngOnInit() { await this.hub.start(); }

  clear() { this.hub.events.set([]); }

  severityFor(t: string): 'success'|'info'|'warn'|'danger'|'secondary'|'contrast' {
    if (t.startsWith('Threat') || t === 'AccessDenied') return 'danger';
    if (t.startsWith('Approval')) return 'warn';
    if (t === 'AuthIssued' || t === 'TaskCompleted') return 'success';
    return 'info';
  }

  stringify(v: unknown): string { try { return JSON.stringify(v); } catch { return String(v); } }
}
