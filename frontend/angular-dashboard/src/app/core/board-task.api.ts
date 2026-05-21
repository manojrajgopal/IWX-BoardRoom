import { Injectable, inject } from '@angular/core';
import { HttpClient } from '@angular/common/http';
import { Observable } from 'rxjs';
import { environment } from '../../environments/environment';

export interface BoardTask {
  id: string;
  title: string;
  description: string;
  targetDepartment: string;
  priority: string;
  status: number;
  createdAtUtc: string;
  approvedAtUtc?: string | null;
  completedAtUtc?: string | null;
  resultSummary?: string | null;
}

export interface CreateBoardTaskRequest {
  title: string;
  description: string;
  targetDepartment: string;
  priority: string;
}

@Injectable({ providedIn: 'root' })
export class BoardTaskApi {
  private http = inject(HttpClient);
  private base = `${environment.apiBaseUrl}/api/ceo/tasks`;

  list(): Observable<BoardTask[]> {
    return this.http.get<BoardTask[]>(`${this.base}/`);
  }

  create(req: CreateBoardTaskRequest): Observable<{ id: string }> {
    return this.http.post<{ id: string }>(`${this.base}/`, req);
  }

  approve(id: string): Observable<unknown> {
    return this.http.post(`${this.base}/${id}/approve`, {});
  }
}
