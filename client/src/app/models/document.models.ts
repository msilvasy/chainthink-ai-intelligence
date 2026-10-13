export interface UploadDocumentResponse {
  documentId: string;
  fileName: string;
  chunkCount: number;
  characterCount: number;
  processedAt: string;
  message: string;
}

export interface DocumentInfo {
  id: string;
  fileName: string;
  contentType: string;
  fileSize: number;
  uploadedAt: string;
  chunkCount: number;
  characterCount: number;
}

export interface QueryRequest {
  query: string;
  topK?: number;
  documentId?: string;
  documentIds?: string[];
  modelId?: string;
}

export interface LlmModel {
  id: string;
  object: string;
  created: number;
  owned_by: string;
}

export interface LlmModelsResponse {
  object: string;
  data: LlmModel[];
}

export interface SourceReference {
  documentId: string;
  documentName: string;
  chunkId: string;
  excerpt: string;
  relevanceScore: number;
}

export interface QueryResponse {
  answer: string;
  sources: SourceReference[];
  processingTimeMs: number;
  answerFoundInContext: boolean;
}

export interface QueryLog {
  id: number;
  userId: string;
  query: string;
  response: string;
  relevanceScore: number | null;
  processingTimeMs: number;
  createdAt: string;
}

export interface ReportRequest {
  topic: string;
  topK?: number;
  modelId: string;
  documentId?: string;
  additionalInstructions?: string;
}

export interface ReportContent {
  executiveSummary: string;
  keyFindings: string[];
  risks: string[];
  recommendations: string[];
}

export interface ReportResponse {
  topic: string;
  report: ReportContent;
  sources: SourceReference[];
  generatedAt: string;
  processingTimeMs: number;
}

export interface ErrorResponse {
  error: string;
  message: string;
  details?: string;
  timestamp: string;
  traceId: string;
}

