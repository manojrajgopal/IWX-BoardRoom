import { Component, inject, signal } from '@angular/core';
import { CommonModule } from '@angular/common';
import { FormsModule } from '@angular/forms';
import { TableModule } from 'primeng/table';
import { ButtonModule } from 'primeng/button';
import { InputTextModule } from 'primeng/inputtext';
import { TagModule } from 'primeng/tag';
import { AuthApi, CreateUserRequest, IwxUser } from '../core/auth.api';

@Component({
  selector: 'app-users',
  imports: [CommonModule, FormsModule, TableModule, ButtonModule, InputTextModule, TagModule],
  template: `
    <div class="flex items-center justify-between mb-6">
      <div>
        <h2 class="text-2xl font-bold text-white">Users &amp; Roles</h2>
        <p class="text-sm text-white/60">Managed by <code>auth-service</code> · JWT + RBAC</p>
      </div>
      <p-button label="Refresh" icon="pi pi-refresh" severity="secondary" (onClick)="load()" />
    </div>

    <div class="grid grid-cols-1 lg:grid-cols-3 gap-6">
      <div class="lg:col-span-2 bg-white/5 rounded-xl border border-white/10 p-4">
        <p-table [value]="users()" [loading]="loading()" styleClass="p-datatable-sm">
          <ng-template pTemplate="header">
            <tr>
              <th>Username</th><th>Email</th><th>Tenant</th><th>Roles</th><th>Last Login</th>
            </tr>
          </ng-template>
          <ng-template pTemplate="body" let-u>
            <tr>
              <td class="font-medium text-violet-300">{{ u.username }}</td>
              <td>{{ u.email }}</td>
              <td>{{ u.tenantId }}</td>
              <td>
                <p-tag *ngFor="let r of u.roles" [value]="r" severity="info" styleClass="mr-1" />
              </td>
              <td class="text-xs text-white/60">{{ u.lastLoginAtUtc || '—' }}</td>
            </tr>
          </ng-template>
        </p-table>
      </div>

      <div class="bg-white/5 rounded-xl border border-white/10 p-5">
        <h3 class="text-lg font-semibold mb-4">Create User</h3>
        <div class="flex flex-col gap-3">
          <input pInputText [(ngModel)]="form.username" placeholder="username" />
          <input pInputText [(ngModel)]="form.email" placeholder="email" />
          <input pInputText [(ngModel)]="form.password" type="password" placeholder="password" />
          <input pInputText [(ngModel)]="form.tenantId" placeholder="tenant (default: default)" />
          <input pInputText [(ngModel)]="rolesCsv" placeholder="roles (csv: user,director)" />
          <p-button label="Create" icon="pi pi-user-plus" (onClick)="create()" [loading]="creating()" />
          <p class="text-xs" [class.text-rose-400]="error()" [class.text-emerald-400]="!error() && message()">{{ message() }}</p>
        </div>
      </div>
    </div>
  `
})
export class UsersComponent {
  private api = inject(AuthApi);
  users = signal<IwxUser[]>([]);
  loading = signal(false);
  creating = signal(false);
  error = signal(false);
  message = signal('');
  form: CreateUserRequest = { username: '', email: '', password: '', tenantId: 'default' };
  rolesCsv = 'user';

  constructor() { this.load(); }

  load() {
    this.loading.set(true);
    this.api.listUsers().subscribe({
      next: u => { this.users.set(u); this.loading.set(false); },
      error: e => { this.loading.set(false); this.error.set(true); this.message.set(e?.message ?? 'failed'); }
    });
  }

  create() {
    this.creating.set(true);
    this.error.set(false);
    const body: CreateUserRequest = {
      ...this.form,
      roles: this.rolesCsv.split(',').map(s => s.trim()).filter(Boolean)
    };
    this.api.createUser(body).subscribe({
      next: () => { this.creating.set(false); this.message.set('user created'); this.load(); },
      error: e => { this.creating.set(false); this.error.set(true); this.message.set(e?.error?.error ?? e?.message ?? 'failed'); }
    });
  }
}
