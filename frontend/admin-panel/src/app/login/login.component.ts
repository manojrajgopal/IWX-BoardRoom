import { Component, inject, signal } from '@angular/core';
import { CommonModule } from '@angular/common';
import { FormsModule } from '@angular/forms';
import { InputTextModule } from 'primeng/inputtext';
import { ButtonModule } from 'primeng/button';
import { CardModule } from 'primeng/card';
import { AuthApi, LoginResponse } from '../core/auth.api';

@Component({
  selector: 'app-login',
  imports: [CommonModule, FormsModule, InputTextModule, ButtonModule, CardModule],
  template: `
    <div class="max-w-md mx-auto mt-12 bg-white/5 rounded-xl border border-white/10 p-8">
      <h2 class="text-2xl font-bold mb-1">Sign In</h2>
      <p class="text-sm text-white/60 mb-6">Authenticate with <code>auth-service</code> (JWT HS256)</p>
      <div class="flex flex-col gap-3">
        <input pInputText [(ngModel)]="username" placeholder="username" autocomplete="username" />
        <input pInputText [(ngModel)]="password" type="password" placeholder="password" autocomplete="current-password" />
        <p-button label="Sign In" icon="pi pi-sign-in" (onClick)="submit()" [loading]="busy()" />
        <p class="text-xs" [class.text-rose-400]="error()" [class.text-emerald-400]="!error() && message()">{{ message() }}</p>
      </div>
      <div *ngIf="result() as r" class="mt-6 p-4 bg-black/30 rounded text-xs font-mono break-all">
        <div class="text-emerald-400 mb-2">subject: {{ r.subject }} · tenant: {{ r.tenant }}</div>
        <div class="text-violet-300 mb-2">roles: {{ r.roles.join(', ') }}</div>
        <div class="text-white/60 mb-2">expires: {{ r.expiresUtc }}</div>
        <div class="text-white/40">{{ r.token }}</div>
      </div>
    </div>
  `
})
export class LoginComponent {
  private api = inject(AuthApi);
  username = 'ceo';
  password = 'ChangeMe!2026';
  busy = signal(false);
  error = signal(false);
  message = signal('');
  result = signal<LoginResponse | null>(null);

  submit() {
    this.busy.set(true);
    this.error.set(false);
    this.api.login({ username: this.username, password: this.password }).subscribe({
      next: r => {
        this.busy.set(false);
        this.result.set(r);
        localStorage.setItem('iwx_token', r.token);
        this.message.set('token stored in localStorage');
      },
      error: e => { this.busy.set(false); this.error.set(true); this.message.set(e?.message ?? 'login failed'); }
    });
  }
}
