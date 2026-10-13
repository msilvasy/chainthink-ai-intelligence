# Document AI - Angular Client

Angular 20+ client application for the Document AI system.

## Setup

1. Install dependencies:
```bash
npm install
```

2. Start development server:
```bash
npm start
```

The application will be available at `http://localhost:4200`

## API Configuration

The base API URL is configured in `src/app/core/config/api.config.ts`. 
Default: `http://localhost:5000`

## Project Structure

```
src/app/
├── core/              # Core services, interceptors, config
│   ├── config/        # API configuration
│   ├── interceptors/ # HTTP interceptors
│   └── services/      # Core services (Document, Chat, Report)
├── features/          # Feature modules
│   ├── documents/     # Document upload and list
│   ├── chat/          # Q&A chat interface
│   └── reports/       # Report generation and viewing
├── layout/            # Layout component with sidebar
├── models/            # TypeScript interfaces/DTOs
└── shared/            # Shared components and services
    ├── components/    # Reusable components (Toast, LoadingSpinner)
    └── services/      # Shared services (Notification)
```

## Features

- **Document Upload**: Drag & drop file upload with progress indicator
- **Document List**: View all uploaded documents
- **Chat Interface**: RAG-based Q&A with source references
- **Report Generation**: Generate structured AI reports from documents
- **Report Viewer**: View and download generated reports

## API Endpoints

The application expects the following endpoints:

- `POST /api/documents/upload` - Upload document
- `GET /api/documents` - List documents (to be implemented in backend)
- `POST /api/documents/query` - Query documents
- `POST /api/documents/report` - Generate report

## Notes

- All components are standalone
- Uses RxJS for state management (BehaviorSubject)
- Centralized error handling via HTTP interceptor
- Toast notifications for user feedback
- Responsive design (desktop-first)

