export interface PermissionDefinition {
  id: number;
  screenId?: number | null;
  action?: string | null;
  permissionKey?: string | null;
  code: string;
  name: string;
  description?: string | null;
  applicationId?: number | null;
  isDeleted: boolean;
  createdBy: number;
  createdDate: string;
  modifiedBy?: number | null;
  modifiedDate?: string | null;
}

export interface AppDefinition {
  id: number;
  code: string;
  name: string;
  isDeleted: boolean;
  createdBy: number;
  createdDate: string;
  modifiedBy?: number | null;
  modifiedDate?: string | null;
}

export interface ScreenDefinition {
  id: number;
  appId: number;
  code: string;
  name: string;
  route?: string | null;
  isDeleted: boolean;
  createdBy: number;
  createdDate: string;
  modifiedBy?: number | null;
  modifiedDate?: string | null;
}

export interface ProfileDefinition {
  id: number;
  name: string;
  isAdmin: boolean;
  isDeleted: boolean;
  createdBy: number;
  createdDate: string;
  modifiedBy?: number | null;
  modifiedDate?: string | null;
}

export interface ProfilePermission {
  id: number;
  profileId: number;
  permissionId: number;
  canAdd: boolean;
  canModify: boolean;
  canDelete: boolean;
  canSee: boolean;
  hasAccessToMenu: boolean;
  hasAccessToApp: boolean;
  isDeleted: boolean;
  createdBy: number;
  createdDate: string;
  modifiedBy?: number | null;
  modifiedDate?: string | null;
}

export interface UserDefinition {
  id: number;
  profileId: number;
  username: string;
  fullName: string;
  email?: string | null;
  password?: string | null;
  isActive: boolean;
  isDeleted: boolean;
  createdBy: number;
  createdDate: string;
  modifiedBy?: number | null;
  modifiedDate?: string | null;
  departmentId?: number | null;
}

export interface LoginRequest {
  username: string;
  password: string;
}

export interface LoginAccessApp {
  id: number;
  code: string;
  name: string;
  hasAccessToApp: boolean;
}

export interface LoginAccessScreen {
  id: number;
  appId: number;
  code: string;
  name: string;
  route?: string | null;
  hasAccessToMenu: boolean;
}

export interface LoginAccessPermission {
  id: number;
  applicationId?: number | null;
  screenId?: number | null;
  code: string;
  name: string;
  canSee: boolean;
  canAdd: boolean;
  canModify: boolean;
  canDelete: boolean;
  hasAccessToMenu: boolean;
  hasAccessToApp: boolean;
}

export interface LoginAccess {
  applications: LoginAccessApp[];
  screens: LoginAccessScreen[];
  permissions: LoginAccessPermission[];
}

export interface LoginResponse {
  id: number;
  username: string;
  fullName: string;
  departmentId?: number | null;
  departmentName?: string | null;
  access: LoginAccess;
  /** True when the user's profile has IsAdmin (server-side); bypasses permission checks in the client. */
  isAdmin?: boolean;
}
