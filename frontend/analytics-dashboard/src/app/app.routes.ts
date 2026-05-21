import { Routes } from '@angular/router';

export const routes: Routes = [
  { path: '', redirectTo: 'audit', pathMatch: 'full' },
  { path: 'audit', loadComponent: () => import('./audit/audit.component').then(m => m.AuditComponent) },
  { path: 'threats', loadComponent: () => import('./threats/threats.component').then(m => m.ThreatsComponent) },
  { path: '**', redirectTo: 'audit' }
];
