import { Routes } from '@angular/router';

export const routes: Routes = [
  { path: '', loadComponent: () => import('./monitor/monitor.component').then(m => m.MonitorComponent) },
  { path: '**', redirectTo: '' }
];
