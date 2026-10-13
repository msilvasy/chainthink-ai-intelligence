import { Component, OnInit, inject } from '@angular/core';
import { CommonModule } from '@angular/common';
import { FormsModule } from '@angular/forms';
import { ReportService } from '../../../core/services/report.service';
import { EmbeddingsService } from '../../../core/services/embeddings.service';
import { NotificationService } from '../../../shared/services/notification.service';
import { LoadingSpinnerComponent } from '../../../shared/components/loading-spinner/loading-spinner.component';
import { ReportViewerComponent } from '../report-viewer/report-viewer.component';
import { LlmModel, ReportRequest, ReportResponse } from '../../../models/document.models';
import { EmbeddedDocument } from '../../../models/embeddings.models';
import { Observable } from 'rxjs';
import { ChatService } from '../../../core/services/chat.service';
import { QuerySettingsService } from '../../../shared/services/query-settings.service';

@Component({
  selector: 'app-report-generator',
  standalone: true,
  imports: [CommonModule, FormsModule, LoadingSpinnerComponent, ReportViewerComponent],
  templateUrl: './report-generator.component.html',
  styleUrls: ['./report-generator.component.scss']
})
export class ReportGeneratorComponent implements OnInit {
  private reportService = inject(ReportService);
  private embeddingsService = inject(EmbeddingsService);
  private notificationService = inject(NotificationService);
  private chatService = inject(ChatService);
  private querySettingsService = inject(QuerySettingsService);
  models: LlmModel[] = [];
  selectedModelId = 'ministral-3:14b';
  topic = '';
  additionalInstructions = '';
  selectedDocumentId?: string;
  isGenerating = false;
  generatedReport?: ReportResponse;
  documents$: Observable<EmbeddedDocument[]> = this.embeddingsService.getEmbeddedDocuments();

  ngOnInit(): void {
    this.embeddingsService.getEmbeddedDocuments().subscribe();
    this.chatService.getModels().subscribe({
      next: (response) => {
        this.models = response.data;
        if (this.models.length > 0 && !this.selectedModelId) {
          this.selectedModelId = this.models[0].id;
        }
      },
      error: () => {
        this.notificationService.showError('Failed to load models');
      }
    });
  }

  generateReport(): void {
    if (!this.topic.trim()) {
      this.notificationService.showError('Please enter a topic for the report');
      return;
    }

    if (this.topic.trim().length < 10) {
      this.notificationService.showError('Topic must be at least 10 characters');
      return;
    }

    this.isGenerating = true;
    this.generatedReport = undefined;

    const querySettings = this.querySettingsService.current;
    const request: ReportRequest = {
      topic: this.topic.trim(),
      topK: querySettings.topK,
      modelId: this.selectedModelId,
      documentId: this.selectedDocumentId || undefined,
      additionalInstructions: this.additionalInstructions.trim() || undefined
    };

    this.reportService.generateReport(request).subscribe({
      next: (response) => {
        this.generatedReport = response;
        this.isGenerating = false;
        this.notificationService.showSuccess('Report generated successfully!');
      },
      error: () => {
        this.isGenerating = false;
      }
    });
  }

  reset(): void {
    this.topic = '';
    this.additionalInstructions = '';
    this.selectedDocumentId = undefined;
    this.generatedReport = undefined;
  }

  get activeTopK(): number {
    return this.querySettingsService.current.topK;
  }
}

