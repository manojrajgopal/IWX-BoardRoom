import { Component, OnInit, computed, inject, signal } from '@angular/core';
import { FormsModule } from '@angular/forms';
import { DatePipe, NgClass } from '@angular/common';
import { ButtonModule } from 'primeng/button';
import { InputTextModule } from 'primeng/inputtext';
import { TextareaModule } from 'primeng/textarea';
import { SelectModule } from 'primeng/select';
import { TableModule } from 'primeng/table';
import { TagModule } from 'primeng/tag';
import { CardModule } from 'primeng/card';
import { BoardTask, BoardTaskApi } from '../core/board-task.api';
import { BoardroomHubService } from '../core/boardroom-hub.service';

const DEPARTMENTS = [
  'ceo', 'hr', 'sales', 'finance', 'marketing', 'operations',
  'development', 'research', 'legal', 'social-media', 'analytics',
  'customer-support', 'automation', 'platform-intelligence'
];

const PRIORITIES = ['Low', 'Normal', 'High', 'Critical'];

const STATUS_LABEL: Record<number, string> = {
  0: 'Draft', 1: 'Pending', 2: 'Approved', 3: 'Dispatched',
  4: 'In Progress', 5: 'Completed', 6: 'Failed', 7: 'Rejected'
};

@Component({
  selector: 'iwx-boardroom',
  standalone: true,
  imports: [
    FormsModule, DatePipe, NgClass,
    ButtonModule, InputTextModule, TextareaModule, SelectModule,
    TableModule, TagModule, CardModule
  ],
  templateUrl: './boardroom.component.html',
  styleUrl: './boardroom.component.scss'
})
export class BoardroomComponent implements OnInit {
  private api = inject(BoardTaskApi);
  hub = inject(BoardroomHubService);

  readonly departments = DEPARTMENTS.map(d => ({ label: d, value: d }));
  readonly priorities = PRIORITIES.map(p => ({ label: p, value: p }));

  readonly tasks = signal<BoardTask[]>([]);
  readonly loading = signal(false);

  readonly draft = signal({
    title: '',
    description: '',
    targetDepartment: 'marketing',
    priority: 'Normal'
  });

  readonly stats = computed(() => {
    const t = this.tasks();
    return {
      total: t.length,
      pending: t.filter(x => x.status === 1).length,
      inFlight: t.filter(x => [2, 3, 4].includes(x.status)).length,
      done: t.filter(x => x.status === 5).length
    };
  });

  statusLabel(s: number): string { return STATUS_LABEL[s] ?? String(s); }

  statusSeverity(s: number): 'info' | 'warn' | 'success' | 'danger' | 'secondary' {
    if (s === 5) return 'success';
    if (s === 6 || s === 7) return 'danger';
    if (s === 1) return 'warn';
    if (s === 2 || s === 3 || s === 4) return 'info';
    return 'secondary';
  }

  async ngOnInit(): Promise<void> {
    await this.hub.start();
    this.refresh();
    setInterval(() => this.refresh(), 5000);
  }

  refresh(): void {
    this.loading.set(true);
    this.api.list().subscribe({
      next: r => { this.tasks.set(r); this.loading.set(false); },
      error: () => this.loading.set(false)
    });
  }

  submit(): void {
    const d = this.draft();
    if (!d.title || !d.description) return;
    this.api.create(d).subscribe(() => {
      this.draft.set({ ...d, title: '', description: '' });
      this.refresh();
    });
  }

  approve(t: BoardTask): void {
    this.api.approve(t.id).subscribe(() => this.refresh());
  }
}
