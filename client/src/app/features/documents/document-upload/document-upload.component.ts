import { Component, inject } from '@angular/core';
import { CommonModule } from '@angular/common';
import { FormsModule } from '@angular/forms';
import { DocumentService } from '../../../core/services/document.service';
import { NotificationService } from '../../../shared/services/notification.service';
import { LoadingSpinnerComponent } from '../../../shared/components/loading-spinner/loading-spinner.component';

@Component({
  selector: 'app-document-upload',
  standalone: true,
  imports: [CommonModule, FormsModule, LoadingSpinnerComponent],
  templateUrl: './document-upload.component.html',
  styleUrls: ['./document-upload.component.scss']
})
export class DocumentUploadComponent {
  private documentService = inject(DocumentService);
  private notificationService = inject(NotificationService);

  isDragging = false;
  isUploading = false;
  uploadProgress = 0;
  selectedFile: File | null = null;
  acceptedTypes = '.pdf,.docx,.txt';

  onDragOver(event: DragEvent): void {
    event.preventDefault();
    event.stopPropagation();
    this.isDragging = true;
  }

  onDragLeave(event: DragEvent): void {
    event.preventDefault();
    event.stopPropagation();
    this.isDragging = false;
  }

  onDrop(event: DragEvent): void {
    event.preventDefault();
    event.stopPropagation();
    this.isDragging = false;

    const files = event.dataTransfer?.files;
    if (files && files.length > 0) {
      this.handleFile(files[0]);
    }
  }

  onFileSelected(event: Event): void {
    const input = event.target as HTMLInputElement;
    if (input.files && input.files.length > 0) {
      this.handleFile(input.files[0]);
    }
  }

  private handleFile(file: File): void {
    if (!this.isValidFileType(file)) {
      this.notificationService.showError(
        `Invalid file type. Supported types: ${this.acceptedTypes}`
      );
      return;
    }

    if (file.size > 50 * 1024 * 1024) {
      this.notificationService.showError('File size must be less than 50MB');
      return;
    }

    this.selectedFile = file;
  }

  upload(): void {
    if (!this.selectedFile) {
      return;
    }

    this.isUploading = true;
    this.uploadProgress = 0;

    this.documentService.uploadDocument(this.selectedFile).subscribe({
      next: (result) => {
        this.uploadProgress = result.progress;
        if (result.response) {
          this.notificationService.showSuccess(
            `Document "${result.response.fileName}" uploaded successfully!`
          );
          this.selectedFile = null;
          this.uploadProgress = 0;
          this.isUploading = false;
        }
      },
      error: () => {
        this.isUploading = false;
        this.uploadProgress = 0;
      }
    });
  }

  cancel(): void {
    this.selectedFile = null;
    this.uploadProgress = 0;
    this.isUploading = false;
  }

  private isValidFileType(file: File): boolean {
    const extension = '.' + file.name.split('.').pop()?.toLowerCase();
    return this.acceptedTypes.split(',').includes(extension);
  }

  getFileSize(size: number): string {
    if (size < 1024) return size + ' B';
    if (size < 1024 * 1024) return (size / 1024).toFixed(2) + ' KB';
    return (size / (1024 * 1024)).toFixed(2) + ' MB';
  }
}

