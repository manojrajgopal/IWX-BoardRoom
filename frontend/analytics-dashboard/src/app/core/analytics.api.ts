import { Injectable, inject } from '@angular/core';
import { HttpClient, HttpParams } from '@angular/common/http';
import { Observable } from 'rxjs';
import { environment } from '../../environments/environment';

export interface AuditRecord {
  id: string;
  actor: string;
  action: string;
  resource: string;
  outcome?: string;
  payloadJson?: string;
  source: string;
  recordedAtUtc: string;
  hash: string;
  prevHash: string;
}

export interface ChainVerification {
  ok: boolean;
  checkedCount: number;
  failedId?: string | null;
}

@Injectable({ providedIn: 'root' })
export class AuditApi {
  private http = inject(HttpClient);
  private base = `${environment.apiBaseUrl}/api/security/audit`;

  list(filter: { actor?: string; resource?: string; action?: string; take?: number }): Observable<AuditRecord[]> {
    let params = new HttpParams();
    if (filter.actor) params = params.set('actor', filter.actor);
    if (filter.resource) params = params.set('resource', filter.resource);
    if (filter.action) params = params.set('action', filter.action);
    params = params.set('take', String(filter.take ?? 100));
    return this.http.get<AuditRecord[]>(this.base, { params });
  }

  verify(): Observable<ChainVerification> {
    return this.http.get<ChainVerification>(`${this.base}/verify`);
  }
}

export interface ScanRequest { subject: string; source: string; text: string; }
export interface ScanResult {
  id: string;
  source: string;
  subject: string;
  severity: string;
  category: string;
  blocked: boolean;
  score: number;
  reasons: string[];
  metadata: Record<string, string>;
  detectedAtUtc: string;
}

@Injectable({ providedIn: 'root' })
export class ThreatApi {
  private http = inject(HttpClient);
  private base = `${environment.apiBaseUrl}/api/security/scan`;

  scanPrompt(req: ScanRequest): Observable<ScanResult> {
    return this.http.post<ScanResult>(`${this.base}/prompt`, req);
  }
}
