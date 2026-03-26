import { Injectable, inject } from '@angular/core';
import { HttpClient } from '@angular/common/http';
import { Observable } from 'rxjs';

export interface PersonAstronaut {
  personId: number;
  name: string;
  photoUrl: string | null;
  currentRank: string;
  currentDutyTitle: string;
  careerStartDate: string | null;
  careerEndDate: string | null;
}

export interface AstronautDuty {
  id: number;
  personId: number;
  rank: string;
  dutyTitle: string;
  dutyStartDate: string;
  dutyEndDate: string | null;
}

export interface AstronautDutyWithPerson {
  id: number;
  personId: number;
  personName: string;
  rank: string;
  dutyTitle: string;
  dutyStartDate: string;
  dutyEndDate: string | null;
}

export interface AstronautDutiesResponse {
  success: boolean;
  message: string;
  responseCode: number;
  person: PersonAstronaut | null;
  astronautDuties: AstronautDuty[];
}

export interface AllDutiesResponse {
  success: boolean;
  message: string;
  responseCode: number;
  astronautDuties: AstronautDutyWithPerson[];
}

export interface PersonResponse {
  success: boolean;
  message: string;
  responseCode: number;
  person: PersonAstronaut | null;
}

export interface PeopleResponse {
  success: boolean;
  message: string;
  responseCode: number;
  people: PersonAstronaut[];
}

export interface CreatePersonResponse {
  success: boolean;
  message: string;
  responseCode: number;
  id: number;
}

export interface UpdatePersonResponse {
  success: boolean;
  message: string;
  responseCode: number;
  id: number;
}

export interface CreateDutyResponse {
  success: boolean;
  message: string;
  responseCode: number;
  id: number | null;
}

export interface UpdateDutyResponse {
  success: boolean;
  message: string;
  responseCode: number;
  id: number;
}

export interface CreateDutyRequest {
  name: string;
  rank: string;
  dutyTitle: string;
  dutyStartDate: string;
}

export interface UpdateDutyRequest {
  rank: string;
  dutyTitle: string;
  dutyStartDate: string;
  dutyEndDate: string | null;
}

export interface AuditLog {
  id: number;
  createdDate: string;
  message: string;
  isException: boolean;
}

export interface AuditLogsResponse {
  success: boolean;
  message: string;
  responseCode: number;
  logs: AuditLog[];
  totalCount: number;
  page: number;
  pageSize: number;
}

export interface StatsResponse {
  success: boolean;
  message: string;
  responseCode: number;
  totalPeople: number;
  activeAstronauts: number;
  retiredAstronauts: number;
  totalDuties: number;
}

@Injectable({ providedIn: 'root' })
export class AstronautService {
  private readonly http = inject(HttpClient);
  private readonly baseUrl = '/api';

  getAllPeople(): Observable<PeopleResponse> {
    return this.http.get<PeopleResponse>(`${this.baseUrl}/person`);
  }

  getPersonByName(name: string): Observable<PersonResponse> {
    return this.http.get<PersonResponse>(`${this.baseUrl}/person/${encodeURIComponent(name)}`);
  }

  addPerson(name: string): Observable<CreatePersonResponse> {
    return this.http.post<CreatePersonResponse>(`${this.baseUrl}/person`, JSON.stringify(name), {
      headers: { 'Content-Type': 'application/json' }
    });
  }

  updatePerson(currentName: string, newName: string): Observable<UpdatePersonResponse> {
    return this.http.put<UpdatePersonResponse>(
      `${this.baseUrl}/person/${encodeURIComponent(currentName)}`,
      JSON.stringify(newName),
      { headers: { 'Content-Type': 'application/json' } }
    );
  }

  getAllAstronautDuties(): Observable<AllDutiesResponse> {
    return this.http.get<AllDutiesResponse>(`${this.baseUrl}/astronautduty`);
  }

  getAstronautDutiesByName(name: string): Observable<AstronautDutiesResponse> {
    return this.http.get<AstronautDutiesResponse>(`${this.baseUrl}/astronautduty/${encodeURIComponent(name)}`);
  }

  addAstronautDuty(duty: CreateDutyRequest): Observable<CreateDutyResponse> {
    return this.http.post<CreateDutyResponse>(`${this.baseUrl}/astronautduty`, duty);
  }

  updateAstronautDuty(id: number, duty: UpdateDutyRequest): Observable<UpdateDutyResponse> {
    return this.http.put<UpdateDutyResponse>(`${this.baseUrl}/astronautduty/${id}`, duty);
  }

  getAuditLogs(page = 1, pageSize = 50, search?: string, sortBy = 'date', sortDirection = 'desc'): Observable<AuditLogsResponse> {
    let url = `${this.baseUrl}/auditlog?page=${page}&pageSize=${pageSize}&sortBy=${sortBy}&sortDirection=${sortDirection}`;
    if (search) url += `&search=${encodeURIComponent(search)}`;
    return this.http.get<AuditLogsResponse>(url);
  }

  getStats(): Observable<StatsResponse> {
    return this.http.get<StatsResponse>(`${this.baseUrl}/stats`);
  }

  uploadPhoto(name: string, file: File): Observable<{ success: boolean; message: string }> {
    const formData = new FormData();
    formData.append('file', file);
    return this.http.post<{ success: boolean; message: string }>(
      `${this.baseUrl}/person/${encodeURIComponent(name)}/photo`,
      formData
    );
  }

  deletePerson(name: string): Observable<{ success: boolean; message: string }> {
    return this.http.delete<{ success: boolean; message: string }>(
      `${this.baseUrl}/person/${encodeURIComponent(name)}`
    );
  }

  deleteDuty(id: number): Observable<{ success: boolean; message: string }> {
    return this.http.delete<{ success: boolean; message: string }>(
      `${this.baseUrl}/astronautduty/${id}`
    );
  }
}
