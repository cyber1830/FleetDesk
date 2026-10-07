import { Component, OnInit, computed, signal } from '@angular/core';
import { DatePipe } from '@angular/common';
import { FormsModule } from '@angular/forms';
import { HttpErrorResponse } from '@angular/common/http';
import { ApiService } from './api.service';
import { AuthService } from './auth.service';
import { CrewMember, Job, JobPriority, JobStatus, Vessel } from './models';

const COLUMNS: { status: JobStatus; label: string }[] = [
  { status: 'Planned', label: 'Planned' },
  { status: 'InProgress', label: 'In progress' },
  { status: 'Completed', label: 'Completed' },
  { status: 'Cancelled', label: 'Cancelled' },
];

@Component({
  selector: 'app-jobs',
  standalone: true,
  imports: [FormsModule, DatePipe],
  template: `
    <section class="toolbar">
      <input placeholder="Search jobs…" [(ngModel)]="search" (ngModelChange)="reload()" />
      <select [(ngModel)]="vesselFilter" (ngModelChange)="reload()">
        <option [ngValue]="null">All vessels</option>
        @for (v of vessels(); track v.id) {
          <option [ngValue]="v.id">{{ v.name }}</option>
        }
      </select>
      @if (auth.canEdit()) {
        <button class="primary" (click)="showForm.set(!showForm())">
          {{ showForm() ? 'Close' : 'New job' }}
        </button>
      }
    </section>

    @if (message()) {
      <p class="error">{{ message() }}</p>
    }

    @if (showForm() && auth.canEdit()) {
      <section class="card">
        <h3>New job</h3>
        <div class="grid">
          <label>Title <input [(ngModel)]="draft.title" name="title" /></label>
          <label>Vessel
            <select [(ngModel)]="draft.vesselId" name="vessel">
              @for (v of vessels(); track v.id) {
                <option [ngValue]="v.id">{{ v.name }}</option>
              }
            </select>
          </label>
          <label>Priority
            <select [(ngModel)]="draft.priority" name="priority">
              @for (p of priorities; track p) {
                <option [ngValue]="p">{{ p }}</option>
              }
            </select>
          </label>
          <label>Start <input type="datetime-local" [(ngModel)]="draft.start" name="start" /></label>
          <label>End <input type="datetime-local" [(ngModel)]="draft.end" name="end" /></label>
          <label class="wide">Description <input [(ngModel)]="draft.description" name="description" /></label>
        </div>
        <button class="primary" (click)="create()">Create job</button>
      </section>
    }

    <section class="board">
      @for (col of columns; track col.status) {
        <div class="column">
          <h3>{{ col.label }} <span class="count">{{ byStatus()[col.status].length }}</span></h3>
          @for (job of byStatus()[col.status]; track job.id) {
            <article class="job" [class]="'prio-' + job.priority.toLowerCase()">
              <header>
                <strong>{{ job.title }}</strong>
                <span class="pill">{{ job.priority }}</span>
              </header>
              <p class="muted">{{ job.vesselName }}</p>
              <p class="small">{{ job.scheduledStart | date: 'MMM d, HH:mm' }} – {{ job.scheduledEnd | date: 'MMM d, HH:mm' }}</p>

              <div class="crew">
                @for (c of job.crew; track c.id) {
                  <span class="chip">
                    {{ c.name }} <small>({{ c.rank }})</small>
                    @if (auth.canEdit()) {
                      <button class="x" (click)="unassign(job, c)" title="Remove">×</button>
                    }
                  </span>
                }
              </div>

              @if (auth.canEdit()) {
                <div class="actions">
                  <select #crewPick (change)="assign(job, crewPick.value); crewPick.value = ''">
                    <option value="">Assign crew…</option>
                    @for (c of crew(); track c.id) {
                      <option [value]="c.id">{{ c.name }} – {{ c.rank }}</option>
                    }
                  </select>
                  <select #statusPick (change)="setStatus(job, statusPick.value)">
                    <option value="">Move to…</option>
                    @for (s of columns; track s.status) {
                      @if (s.status !== job.status) {
                        <option [value]="s.status">{{ s.label }}</option>
                      }
                    }
                  </select>
                  @if (auth.isAdmin()) {
                    <button class="danger" (click)="remove(job)">Delete</button>
                  }
                </div>
              }
            </article>
          } @empty {
            <p class="muted small">No jobs</p>
          }
        </div>
      }
    </section>
  `,
})
export class JobsComponent implements OnInit {
  readonly columns = COLUMNS;
  readonly priorities: JobPriority[] = ['Low', 'Normal', 'High', 'Urgent'];

  jobs = signal<Job[]>([]);
  vessels = signal<Vessel[]>([]);
  crew = signal<CrewMember[]>([]);
  message = signal('');
  showForm = signal(false);

  search = '';
  vesselFilter: number | null = null;
  draft = this.emptyDraft();

  byStatus = computed(() => {
    const groups: Record<JobStatus, Job[]> = { Planned: [], InProgress: [], Completed: [], Cancelled: [] };
    for (const j of this.jobs()) groups[j.status].push(j);
    return groups;
  });

  constructor(private api: ApiService, public auth: AuthService) {}

  ngOnInit(): void {
    this.api.vessels().subscribe((v) => this.vessels.set(v));
    this.api.crew().subscribe((c) => this.crew.set(c));
    this.reload();
  }

  reload(): void {
    this.api.jobs({ vesselId: this.vesselFilter, search: this.search }).subscribe({
      next: (jobs) => this.jobs.set(jobs),
      error: (e) => this.fail(e),
    });
  }

  create(): void {
    this.message.set('');
    if (!this.draft.title.trim() || !this.draft.vesselId || !this.draft.start || !this.draft.end) {
      this.message.set('Title, vessel, start and end are required.');
      return;
    }
    this.api
      .createJob({
        title: this.draft.title,
        description: this.draft.description || null,
        vesselId: this.draft.vesselId,
        priority: this.draft.priority,
        scheduledStart: new Date(this.draft.start).toISOString(),
        scheduledEnd: new Date(this.draft.end).toISOString(),
      })
      .subscribe({
        next: () => {
          this.draft = this.emptyDraft();
          this.showForm.set(false);
          this.reload();
        },
        error: (e) => this.fail(e),
      });
  }

  setStatus(job: Job, status: string): void {
    if (!status) return;
    this.api.setStatus(job.id, status as JobStatus).subscribe({
      next: (j) => this.replace(j),
      error: (e) => this.fail(e),
    });
  }

  assign(job: Job, crewId: string): void {
    if (!crewId) return;
    this.message.set('');
    this.api.assign(job.id, Number(crewId)).subscribe({
      next: (j) => this.replace(j),
      error: (e) => this.fail(e),
    });
  }

  unassign(job: Job, member: CrewMember): void {
    this.api.unassign(job.id, member.id).subscribe({
      next: (j) => this.replace(j),
      error: (e) => this.fail(e),
    });
  }

  remove(job: Job): void {
    if (!confirm(`Delete "${job.title}"?`)) return;
    this.api.deleteJob(job.id).subscribe({
      next: () => this.jobs.update((list) => list.filter((j) => j.id !== job.id)),
      error: (e) => this.fail(e),
    });
  }

  private replace(updated: Job): void {
    this.jobs.update((list) => list.map((j) => (j.id === updated.id ? updated : j)));
  }

  private fail(e: HttpErrorResponse): void {
    this.message.set(e.error?.error ?? `Request failed (${e.status}).`);
  }

  private emptyDraft() {
    return {
      title: '',
      description: '',
      vesselId: null as number | null,
      priority: 'Normal' as JobPriority,
      start: '',
      end: '',
    };
  }
}
