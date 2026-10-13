import { Routes } from '@angular/router';
import { LayoutComponent } from './layout/layout.component';

export const routes: Routes = [
  {
    path: '',
    component: LayoutComponent,
    children: [
      {
        path: '',
        redirectTo: '/upload',
        pathMatch: 'full'
      },
      // {
      //   path: 'documents',
      //   loadComponent: () =>
      //     import('./features/documents/document-list/document-list.component').then(
      //       m => m.DocumentListComponent
      //     )
      // },
      {
        path: 'upload',
        loadComponent: () =>
          import('./features/documents/document-upload/document-upload.component').then(
            m => m.DocumentUploadComponent
          )
      },
      {
        path: 'chat',
        loadComponent: () =>
          import('./features/chat/chat.component').then(m => m.ChatComponent)
      },
      {
        path: 'query-logs',
        loadComponent: () =>
          import('./features/query-logs/query-logs.component').then(m => m.QueryLogsComponent)
      },
      {
        path: 'reports',
        loadComponent: () =>
          import('./features/reports/report-generator/report-generator.component').then(
            m => m.ReportGeneratorComponent
          )
      },
      {
        path: 'embeddings',
        loadComponent: () =>
          import('./features/embeddings/embedded-documents/embedded-documents.component').then(
            m => m.EmbeddedDocumentsComponent
          )
      },
      {
        path: 'embeddings/:documentId/chunks',
        loadComponent: () =>
          import('./features/embeddings/document-chunks/document-chunks.component').then(
            m => m.DocumentChunksComponent
          )
      },
      {
        path: 'architecture-docs',
        loadComponent: () =>
          import('./features/architecture-docs/architecture-docs.component').then(
            m => m.ArchitectureDocsComponent
          )
      }
    ]
  }
];

