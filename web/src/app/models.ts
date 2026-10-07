export type JobStatus = 'Planned' | 'InProgress' | 'Completed' | 'Cancelled';
export type JobPriority = 'Low' | 'Normal' | 'High' | 'Urgent';

export interface AuthResponse {
  token: string;
  email: string;
  role: 'Viewer' | 'Dispatcher' | 'Admin';
  expiresAtUtc: string;
}

export interface Vessel {
  id: number;
  name: string;
  imoNumber: string;
  vesselType: string;
}

export interface CrewMember {
  id: number;
  name: string;
  rank: string;
}

export interface Job {
  id: number;
  title: string;
  description: string | null;
  vesselId: number;
  vesselName: string;
  status: JobStatus;
  priority: JobPriority;
  scheduledStart: string;
  scheduledEnd: string;
  crew: CrewMember[];
}

export interface JobWrite {
  title: string;
  description: string | null;
  vesselId: number;
  priority: JobPriority;
  scheduledStart: string;
  scheduledEnd: string;
}
