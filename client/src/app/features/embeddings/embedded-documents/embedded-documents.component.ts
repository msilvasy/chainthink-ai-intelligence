import { Component, OnInit, inject, ViewChild } from '@angular/core';
import { CommonModule } from '@angular/common';
import { Router } from '@angular/router';
import { EmbeddingsService } from '../../../core/services/embeddings.service';
import { NotificationService } from '../../../shared/services/notification.service';
import { LoadingSpinnerComponent } from '../../../shared/components/loading-spinner/loading-spinner.component';
import { ConfirmationDialogComponent } from '../../../shared/components/confirmation-dialog/confirmation-dialog.component';
import { EmbeddedDocument } from '../../../models/embeddings.models';

@Component({
  selector: 'app-embedded-documents',
  standalone: true,
  imports: [CommonModule, LoadingSpinnerComponent, ConfirmationDialogComponent],
  templateUrl: './embedded-documents.component.html',
  styleUrls: ['./embedded-documents.component.scss']
})
export class EmbeddedDocumentsComponent implements OnInit {
  private embeddingsService = inject(EmbeddingsService);
  private notificationService = inject(NotificationService);
  private router = inject(Router);

  @ViewChild(ConfirmationDialogComponent) confirmationDialog!: ConfirmationDialogComponent;

  documents: EmbeddedDocument[] = [];
  isLoading = false;
  selectedDocumentId?: string;

  ngOnInit(): void {
    this.loadDocuments();
  }

  loadDocuments(): void {
    this.isLoading = true;
    this.embeddingsService.getEmbeddedDocuments().subscribe({
      next: (documents) => {
        this.documents = documents;
        this.isLoading = false;
      },
      error: () => {
        this.isLoading = false;
      }
    });
  }

  viewChunks(documentId: string): void {
    this.router.navigate(['/embeddings', documentId, 'chunks']);
  }

  confirmDelete(documentId: string, documentName: string): void {
    this.selectedDocumentId = documentId;
    this.confirmationDialog.title = 'Delete Document Embeddings';
    this.confirmationDialog.message = `Are you sure you want to delete all embeddings for "${documentName}"? This action cannot be undone.`;
    this.confirmationDialog.confirmText = 'Delete';
    this.confirmationDialog.isDestructive = true;
    this.confirmationDialog.show();
  }

  onDeleteConfirmed(): void {
    if (!this.selectedDocumentId) {
      return;
    }

    const documentId = this.selectedDocumentId;
    this.selectedDocumentId = undefined;

    this.embeddingsService.deleteDocumentEmbeddings(documentId).subscribe({
      next: () => {
        this.notificationService.showSuccess('Document embeddings deleted successfully');
        this.loadDocuments();
      },
      error: () => {
        // Error handled by interceptor
      }
    });
  }

  formatDate(dateString: string): string {
    return new Date(dateString).toLocaleDateString();
  }
}

