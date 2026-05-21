import { Routes } from '@angular/router';

export const routes: Routes = [
  { path: '', loadComponent: () => import('./boardroom/boardroom.component').then(m => m.BoardroomComponent) },
  { path: '**', redirectTo: '' }
];
