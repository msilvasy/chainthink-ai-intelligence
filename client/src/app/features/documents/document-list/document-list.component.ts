import { Component, OnInit, inject } from '@angular/core';
import { CommonModule } from '@angular/common';
import { EmbeddingsService } from '../../../core/services/embeddings.service';
import { EmbeddedDocument } from '../../../models/embeddings.models';
import { LoadingSpinnerComponent } from '../../../shared/components/loading-spinner/loading-spinner.component';
import { Observable } from 'rxjs';

@Component({
  selector: 'app-document-list',
  standalone: true,
  imports: [CommonModule, LoadingSpinnerComponent],
  templateUrl: './document-list.component.html',
  styleUrls: ['./document-list.component.scss']
})
export class DocumentListComponent implements OnInit {
  private embeddingsService = inject(EmbeddingsService);

  documents$: Observable<EmbeddedDocument[]> = this.embeddingsService.getEmbeddedDocuments();
  isLoading = false;

  ngOnInit(): void {
    this.loadDocuments();
  }

  loadDocuments(): void {
    this.isLoading = true;
    this.embeddingsService.getEmbeddedDocuments().subscribe({
      next: () => {
        this.isLoading = false;
      },
      error: () => {
        this.isLoading = false;
      }
    });
  }

  formatDate(dateString: string): string {
    return new Date(dateString).toLocaleDateString();
  }
}

