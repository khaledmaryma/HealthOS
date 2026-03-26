import { inject, Injectable } from '@angular/core';
import { HttpClient, HttpParams } from '@angular/common/http';
import {
  AppDefinition,
  PermissionDefinition,
  ProfileDefinition,
  ProfilePermission,
  ScreenDefinition,
  UserDefinition,
  LoginRequest,
  LoginResponse
} from '../models/user-management';
import { ApiEndpointsService } from '../api/api-endpoints.service';

@Injectable({ providedIn: 'root' })
export class UserManagementService {
  private http = inject(HttpClient);
  private readonly endpoints = inject(ApiEndpointsService);
  private readonly baseUrl = this.endpoints.userManagement;

  getPermissions() {
    return this.http.get<PermissionDefinition[]>(`${this.baseUrl}/permissions`);
  }

  createPermission(payload: Partial<PermissionDefinition>) {
    return this.http.post<PermissionDefinition>(`${this.baseUrl}/permissions`, payload);
  }

  updatePermission(id: number, payload: Partial<PermissionDefinition>) {
    return this.http.put<void>(`${this.baseUrl}/permissions/${id}`, { ...payload, id });
  }

  deletePermission(id: number) {
    return this.http.delete<void>(`${this.baseUrl}/permissions/${id}`);
  }

  getApplications() {
    return this.http.get<AppDefinition[]>(`${this.baseUrl}/applications`);
  }

  getScreens(appId?: number) {
    let params = new HttpParams();
    if (appId !== undefined) {
      params = params.set('appId', appId.toString());
    }
    return this.http.get<ScreenDefinition[]>(`${this.baseUrl}/screens`, { params });
  }

  getProfiles() {
    return this.http.get<ProfileDefinition[]>(`${this.baseUrl}/profiles`);
  }

  createProfile(payload: Partial<ProfileDefinition>) {
    return this.http.post<ProfileDefinition>(`${this.baseUrl}/profiles`, payload);
  }

  updateProfile(id: number, payload: Partial<ProfileDefinition>) {
    return this.http.put<void>(`${this.baseUrl}/profiles/${id}`, { ...payload, id });
  }

  deleteProfile(id: number) {
    return this.http.delete<void>(`${this.baseUrl}/profiles/${id}`);
  }

  getProfilePermissions(profileId?: number) {
    let params = new HttpParams();
    if (profileId !== undefined) {
      params = params.set('profileId', profileId.toString());
    }
    return this.http.get<ProfilePermission[]>(`${this.baseUrl}/profile-permissions`, { params });
  }

  createProfilePermission(payload: Partial<ProfilePermission>) {
    return this.http.post<ProfilePermission>(`${this.baseUrl}/profile-permissions`, payload);
  }

  updateProfilePermission(id: number, payload: Partial<ProfilePermission>) {
    return this.http.put<void>(`${this.baseUrl}/profile-permissions/${id}`, { ...payload, id });
  }

  deleteProfilePermission(id: number) {
    return this.http.delete<void>(`${this.baseUrl}/profile-permissions/${id}`);
  }

  getUsers() {
    return this.http.get<UserDefinition[]>(`${this.baseUrl}/users`);
  }

  createUser(payload: Partial<UserDefinition>) {
    return this.http.post<UserDefinition>(`${this.baseUrl}/users`, payload);
  }

  updateUser(id: number, payload: Partial<UserDefinition>) {
    return this.http.put<void>(`${this.baseUrl}/users/${id}`, { ...payload, id });
  }

  deleteUser(id: number) {
    return this.http.delete<void>(`${this.baseUrl}/users/${id}`);
  }

  login(payload: LoginRequest) {
    return this.http.post<LoginResponse>(`${this.baseUrl}/users/login`, payload);
  }
}
