import { Component } from '@angular/core';
import { CommonModule } from '@angular/common';

interface RegistryItem { key: string; name: string; port: number; stack: string; icon: string; }

@Component({
  selector: 'app-registries',
  imports: [CommonModule],
  template: `
    <h2 class="text-2xl font-bold text-white mb-6">Service Registries</h2>

    <div *ngFor="let section of sections" class="mb-8">
      <h3 class="text-sm uppercase tracking-wider text-white/50 mb-3">{{ section.title }}</h3>
      <div class="grid grid-cols-1 md:grid-cols-2 xl:grid-cols-3 gap-4">
        <div *ngFor="let item of section.items"
             class="bg-white/5 border border-white/10 rounded-xl p-4 flex items-start gap-3 hover:bg-white/10 transition">
          <i class="pi {{ item.icon }} text-2xl text-violet-300 mt-1"></i>
          <div class="flex-1">
            <div class="font-semibold text-white">{{ item.name }}</div>
            <div class="text-xs text-white/60 mb-1">{{ item.key }} · port {{ item.port }}</div>
            <div class="text-xs text-white/40">{{ item.stack }}</div>
          </div>
        </div>
      </div>
    </div>
  `
})
export class RegistriesComponent {
  sections: { title: string; items: RegistryItem[] }[] = [
    {
      title: 'Departments',
      items: ['hr','sales','finance','marketing','operations','development','research','legal','social-media','analytics','customer-support','automation','platform-intelligence']
        .map((k, i) => ({ key: k, name: k.replace(/-/g, ' '), port: 8082 + i, stack: '.NET 10 + EF + MassTransit', icon: 'pi-building' }))
    },
    {
      title: 'AI Engines',
      items: [
        { key: 'memory-engine',    name: 'Memory Engine',    port: 8100, stack: '.NET 10 + Redis', icon: 'pi-database' },
        { key: 'llm-router',       name: 'LLM Router',       port: 8101, stack: '.NET 10',         icon: 'pi-share-alt' },
        { key: 'prompt-engine',    name: 'Prompt Engine',    port: 8102, stack: '.NET 10',         icon: 'pi-comment' },
        { key: 'vector-engine',    name: 'Vector Engine',    port: 8103, stack: '.NET 10 + Chroma', icon: 'pi-th-large' },
        { key: 'rag-engine',       name: 'RAG Engine',       port: 8104, stack: '.NET 10',         icon: 'pi-search' },
        { key: 'reasoning-engine', name: 'Reasoning Engine', port: 8105, stack: '.NET 10',         icon: 'pi-bolt' }
      ]
    },
    {
      title: 'Platform Connectors',
      items: ['instagram','youtube','linkedin','twitter','facebook','reddit','whatsapp','email','websites']
        .map((k, i) => ({ key: `${k}-connector`, name: `${k} connector`, port: 8200 + i, stack: '.NET 10 worker', icon: 'pi-link' }))
    },
    {
      title: 'Automation Engines',
      items: [
        { key: 'workflow-engine',  name: 'Workflow Engine',  port: 8300, stack: '.NET 10 + Mongo', icon: 'pi-sitemap' },
        { key: 'scheduler-engine', name: 'Scheduler Engine', port: 8301, stack: '.NET 10 + Quartz + SQL', icon: 'pi-clock' },
        { key: 'task-engine',      name: 'Task Engine',      port: 8302, stack: '.NET 10 + Mongo', icon: 'pi-list' },
        { key: 'approval-engine',  name: 'Approval Engine',  port: 8303, stack: '.NET 10 + EF + SQL', icon: 'pi-check-square' }
      ]
    },
    {
      title: 'Security',
      items: [
        { key: 'java-security-engine', name: 'Java Security Engine', port: 8400, stack: 'Spring Boot 3 / Java 21', icon: 'pi-shield' },
        { key: 'auth-service',         name: 'Auth Service',         port: 8401, stack: '.NET 10 + JWT + SQL',     icon: 'pi-key' },
        { key: 'audit-service',        name: 'Audit Service',        port: 8402, stack: '.NET 10 + Mongo + Kafka', icon: 'pi-file-edit' }
      ]
    }
  ];
}
