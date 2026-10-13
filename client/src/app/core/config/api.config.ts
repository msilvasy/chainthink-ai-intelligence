export const API_CONFIG = {
  baseUrl: 'https://chainthink.cloud',
  endpoints: {
    auth: {
      login: '/api/Auth/login'
    },
    userSettings: {
      save: '/api/user-settings/save'
    },
    documents: '/api/documents',
    upload: '/api/documents/upload',
    query: '/api/documents/query',
    queryLogs: '/api/documents/query-logs',
    models: '/api/documents/models',
    report: '/api/documents/report',
    health: '/api/documents/health',
    embeddings: {
      documents: '/api/embeddings/documents',
      documentChunks: (documentId: string) => `/api/embeddings/documents/${documentId}/chunks`,
      deleteDocument: (documentId: string) => `/api/embeddings/documents/${documentId}`,
      deleteChunk: (chunkId: string) => `/api/embeddings/chunks/${chunkId}`
    }
  }
} as const;

