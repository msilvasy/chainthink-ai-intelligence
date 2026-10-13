export interface LoginRequest {
  email: string;
  password: string;
}

export interface QuerySettings {
  mode: 'fast' | 'balanced' | 'accurate';
  topK: number;
  chunkSize: number;
  chunkOverlap: number;
}

export interface LoginResponse {
  success: boolean;
  message: string;
  token: string;
  email: string;
  userId: string;
  firstName: string;
  lastName: string;
  querySettings: QuerySettings;
}

export interface AuthUser {
  token: string;
  email: string;
  userId: string;
  firstName: string;
  lastName: string;
  querySettings: QuerySettings;
}
