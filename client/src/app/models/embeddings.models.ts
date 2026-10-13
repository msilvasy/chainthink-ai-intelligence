export interface EmbeddedDocument {
  documentId: string;
  documentName: string;
  chunkCount: number;
  uploadedAt: string;
}

export interface EmbeddedChunk {
  chunkId: string;
  previewText: string;
  embeddingVectorSize: number;
}

