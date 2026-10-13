import { Component, OnInit, inject, ViewChild } from '@angular/core';
import { CommonModule } from '@angular/common';
import { ActivatedRoute, Router } from '@angular/router';
import { EmbeddingsService } from '../../../core/services/embeddings.service';
import { NotificationService } from '../../../shared/services/notification.service';
import { LoadingSpinnerComponent } from '../../../shared/components/loading-spinner/loading-spinner.component';
import { ConfirmationDialogComponent } from '../../../shared/components/confirmation-dialog/confirmation-dialog.component';
import { EmbeddedChunk } from '../../../models/embeddings.models';

@Component({
  selector: 'app-document-chunks',
  standalone: true,
  imports: [CommonModule, LoadingSpinnerComponent, ConfirmationDialogComponent],
  templateUrl: './document-chunks.component.html',
  styleUrls: ['./document-chunks.component.scss']
})
export class DocumentChunksComponent implements OnInit {
  private embeddingsService = inject(EmbeddingsService);
  private notificationService = inject(NotificationService);
  private route = inject(ActivatedRoute);
  private router = inject(Router);

  @ViewChild(ConfirmationDialogComponent) confirmationDialog!: ConfirmationDialogComponent;

  documentId?: string;
  documentName?: string;
  chunks: EmbeddedChunk[] = [];
  isLoading = false;
  selectedChunkId?: string;
  expandedChunks = new Set<string>();

  ngOnInit(): void {
    this.documentId = this.route.snapshot.paramMap.get('documentId') || undefined;
    if (!this.documentId) {
      this.router.navigate(['/embeddings']);
      return;
    }
    this.loadChunks();
  }

  loadChunks(): void {
    if (!this.documentId) {
      return;
    }

    this.isLoading = true;
    this.embeddingsService.getDocumentChunks(this.documentId).subscribe({
      next: (chunks) => {
        this.chunks = chunks;
        this.isLoading = false;
      },
      error: () => {
        this.isLoading = false;
      }
    });
  }

  toggleExpand(chunkId: string): void {
    if (this.expandedChunks.has(chunkId)) {
      this.expandedChunks.delete(chunkId);
    } else {
      this.expandedChunks.add(chunkId);
    }
  }

  isExpanded(chunkId: string): boolean {
    return this.expandedChunks.has(chunkId);
  }

  getPreviewText(chunk: EmbeddedChunk): string {
    return chunk.previewText;
  }

  shouldShowExpandButton(chunk: EmbeddedChunk): boolean {
    // Show expand button if preview text ends with "..." indicating truncation
    return chunk.previewText.endsWith('...') || chunk.previewText.length >= 200;
  }

  confirmDelete(chunkId: string): void {
    this.selectedChunkId = chunkId;
    this.confirmationDialog.title = 'Delete Chunk';
    this.confirmationDialog.message = 'Are you sure you want to delete this chunk? This action cannot be undone.';
    this.confirmationDialog.confirmText = 'Delete';
    this.confirmationDialog.isDestructive = true;
    this.confirmationDialog.show();
  }

  onDeleteConfirmed(): void {
    if (!this.selectedChunkId) {
      return;
    }

    const chunkId = this.selectedChunkId;
    this.selectedChunkId = undefined;

    this.embeddingsService.deleteChunk(chunkId).subscribe({
      next: () => {
        this.notificationService.showSuccess('Chunk deleted successfully');
        this.loadChunks();
      },
      error: () => {
        // Error handled by interceptor
      }
    });
  }

  goBack(): void {
    this.router.navigate(['/embeddings']);
  }
}

