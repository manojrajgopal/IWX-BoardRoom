import { Injectable, signal } from '@angular/core';
import * as signalR from '@microsoft/signalr';
import { environment } from '../../environments/environment';

export interface ThinkingEvent {
  taskId: string;
  department: string;
  stage: string;
  message: string;
  timestampUtc: string;
}

export interface CompletedEvent {
  taskId: string;
  targetDepartment: string;
  resultSummary: string;
  resultPayloadJson: string;
  completedAtUtc: string;
}

@Injectable({ providedIn: 'root' })
export class BoardroomHubService {
  private connection?: signalR.HubConnection;
  readonly thinking = signal<ThinkingEvent[]>([]);
  readonly completed = signal<CompletedEvent[]>([]);
  readonly connected = signal(false);

  async start(): Promise<void> {
    if (this.connection) return;
    this.connection = new signalR.HubConnectionBuilder()
      .withUrl(environment.hubUrl, { withCredentials: false })
      .withAutomaticReconnect()
      .configureLogging(signalR.LogLevel.Warning)
      .build();

    this.connection.on('agentThinking', (evt: ThinkingEvent) => {
      this.thinking.update(list => [evt, ...list].slice(0, 200));
    });
    this.connection.on('taskCompleted', (evt: CompletedEvent) => {
      this.completed.update(list => [evt, ...list].slice(0, 100));
    });

    this.connection.onreconnected(() => this.connected.set(true));
    this.connection.onclose(() => this.connected.set(false));

    try {
      await this.connection.start();
      this.connected.set(true);
    } catch (e) {
      console.warn('Hub connect failed', e);
      this.connected.set(false);
    }
  }
}
