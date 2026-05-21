import { Component } from '@angular/core';
import { RouterLink, RouterLinkActive, RouterOutlet } from '@angular/router';

@Component({
  selector: 'app-root',
  imports: [RouterOutlet, RouterLink, RouterLinkActive],
  template: `
    <div class="iwx-dark min-h-screen flex">
      <aside class="w-60 p-6 border-r border-white/10">
        <h1 class="text-xl font-bold text-amber-300 mb-1">IWX</h1>
        <p class="text-xs text-white/60 mb-6">Analytics Dashboard</p>
        <nav class="flex flex-col gap-2 text-sm">
          <a routerLink="/audit" routerLinkActive="text-amber-300 font-semibold" class="px-3 py-2 rounded hover:bg-white/5">
            <i class="pi pi-file-edit mr-2"></i>Audit Log
          </a>
          <a routerLink="/threats" routerLinkActive="text-amber-300 font-semibold" class="px-3 py-2 rounded hover:bg-white/5">
            <i class="pi pi-shield mr-2"></i>Threat Scanner
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
