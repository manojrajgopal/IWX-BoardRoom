import { Injectable, signal } from '@angular/core';
import * as signalR from '@microsoft/signalr';
import { environment } from '../../environments/environment';

export interface MonitorEvent {
  channel: string;
  type: string;
  payload: unknown;
  receivedAtUtc: string;
}

@Injectable({ providedIn: 'root' })
export class MonitorHubService {
  private connection?: signalR.HubConnection;
  readonly connected = signal(false);
  readonly events = signal<MonitorEvent[]>([]);

  async start() {
    if (this.connection) return;
    this.connection = new signalR.HubConnectionBuilder()
      .withUrl(environment.hubUrl)
      .withAutomaticReconnect()
      .build();

    const channels = [
      'TaskCreated', 'TaskApproved', 'TaskCompleted',
      'WorkflowStepDispatched', 'SchedulerTick', 'TaskNodeReady',
      'ApprovalRequested', 'ApprovalDecided',
      'ThreatDetected', 'AuthIssued', 'AccessDenied'
    ];
    for (const ch of channels) {
      this.connection.on(ch, (payload: unknown) => this.push(ch, ch, payload));
    }
    this.connection.onreconnected(() => this.connected.set(true));
    this.connection.onclose(() => this.connected.set(false));

    try {
      await this.connection.start();
      this.connected.set(true);
    } catch (err) {
      this.connected.set(false);
      console.error('SignalR connection failed', err);
    }
  }

  private push(channel: string, type: string, payload: unknown) {
    const next = [{ channel, type, payload, receivedAtUtc: new Date().toISOString() }, ...this.events()];
    this.events.set(next.slice(0, 200));
  }
}
