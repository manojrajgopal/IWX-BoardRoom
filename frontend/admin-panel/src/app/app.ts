import { Component } from '@angular/core';
import { RouterLink, RouterLinkActive, RouterOutlet } from '@angular/router';

@Component({
  selector: 'app-root',
  imports: [RouterOutlet, RouterLink, RouterLinkActive],
  template: `
    <div class="iwx-dark min-h-screen flex">
      <aside class="w-60 p-6 border-r border-white/10 backdrop-blur">
        <h1 class="text-xl font-bold text-violet-300 mb-1">IWX</h1>
        <p class="text-xs text-white/60 mb-6">Admin Panel</p>
        <nav class="flex flex-col gap-2 text-sm">
          <a routerLink="/users" routerLinkActive="text-violet-300 font-semibold" class="px-3 py-2 rounded hover:bg-white/5">
            <i class="pi pi-users mr-2"></i>Users &amp; Roles
          </a>
          <a routerLink="/registries" routerLinkActive="text-violet-300 font-semibold" class="px-3 py-2 rounded hover:bg-white/5">
            <i class="pi pi-server mr-2"></i>Service Registries
          </a>
          <a routerLink="/login" routerLinkActive="text-violet-300 font-semibold" class="px-3 py-2 rounded hover:bg-white/5">
            <i class="pi pi-sign-in mr-2"></i>Login
          </a>
        </nav>
      </aside>
      <main class="flex-1 p-8 overflow-auto">
        <router-outlet />
      </main>
    </div>
  `
})
export class App {}
