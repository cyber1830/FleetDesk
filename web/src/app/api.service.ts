import { Injectable } from '@angular/core';
import { HttpClient, HttpParams } from '@angular/common/http';
import { Observable } from 'rxjs';
import { CrewMember, Job, JobStatus, JobWrite, Vessel } from './models';

@Injectable({ providedIn: 'root' })
export class ApiService {
  constructor(private http: HttpClient) {}

  vessels(): Observable<Vessel[]> {
    return this.http.get<Vessel[]>('/api/vessels');
  }

  crew(): Observable<CrewMember[]> {
    return this.http.get<CrewMember[]>('/api/crew');
  }

  jobs(filters: { status?: JobStatus | ''; vesselId?: number | null; search?: string }): Observable<Job[]> {
    let params = new HttpParams();
    if (filters.status) params = params.set('status', filters.status);
    if (filters.vesselId) params = params.set('vesselId', filters.vesselId);
    if (filters.search?.trim()) params = params.set('search', filters.search.trim());
    return this.http.get<Job[]>('/api/jobs', { params });
  }

  createJob(body: JobWrite): Observable<Job> {
    return this.http.post<Job>('/api/jobs', body);
  }

  setStatus(id: number, status: JobStatus): Observable<Job> {
    return this.http.patch<Job>(`/api/jobs/${id}/status`, { status });
  }

  deleteJob(id: number): Observable<void> {
    return this.http.delete<void>(`/api/jobs/${id}`);
  }

  assign(jobId: number, crewMemberId: number): Observable<Job> {
    return this.http.post<Job>(`/api/jobs/${jobId}/assignments`, { crewMemberId });
  }

  unassign(jobId: number, crewMemberId: number): Observable<Job> {
    return this.http.delete<Job>(`/api/jobs/${jobId}/assignments/${crewMemberId}`);
  }
}
