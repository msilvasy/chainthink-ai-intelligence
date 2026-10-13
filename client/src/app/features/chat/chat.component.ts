import { Component, OnInit, inject } from '@angular/core';
import { CommonModule } from '@angular/common';
import { FormsModule } from '@angular/forms';
import { ChatService } from '../../core/services/chat.service';
import { EmbeddingsService } from '../../core/services/embeddings.service';
import { NotificationService } from '../../shared/services/notification.service';
import { QuerySettingsService } from '../../shared/services/query-settings.service';
import { LoadingSpinnerComponent } from '../../shared/components/loading-spinner/loading-spinner.component';
import { LlmModel, QueryRequest, QueryResponse, SourceReference } from '../../models/document.models';
import { EmbeddedDocument } from '../../models/embeddings.models';
import { Observable } from 'rxjs';

export interface ChatMessage {
  id: string;
  text: string;
  isUser: boolean;
  timestamp: Date;
  sources?: SourceReference[];
  processingTime?: number;
}

@Component({
  selector: 'app-chat',
  standalone: true,
  imports: [CommonModule, FormsModule, LoadingSpinnerComponent],
  templateUrl: './chat.component.html',
  styleUrls: ['./chat.component.scss']
})
export class ChatComponent implements OnInit {
  private chatService = inject(ChatService);
  private embeddingsService = inject(EmbeddingsService);
  private notificationService = inject(NotificationService);
  private querySettingsService = inject(QuerySettingsService);

  messages: ChatMessage[] = [];
  queryText = '';
  isLoading = false;
  selectedDocumentIds: string[] = [];
  preSelectedModelId = 'minimax-m2.7';
  selectedModelId: string | null = null;
  models: LlmModel[] = [];
  documents$: Observable<EmbeddedDocument[]> = this.embeddingsService.getEmbeddedDocuments();

  ngOnInit(): void {
    this.chatService.getModels().subscribe({
      next: (response) => {
        this.models = response.data;
        // if (this.models.length > 0 && !this.selectedModelId) {
        //   this.selectedModelId = this.models[0].id;
        // }
             const modelExist=   this.models.find(model => model.id === this.preSelectedModelId);
              console.log('Model exist:', modelExist);
              if (modelExist) {
               //   this.selectedModelId = this.preSelectedModelId;
              }
    
      },
      error: () => {
        this.notificationService.showError('Failed to load models');
      }
    });
  }

  sendMessage(): void {
    if (!this.queryText.trim() || this.isLoading) {
      return;
    }

    const userMessage: ChatMessage = {
      id: this.generateId(),
      text: this.queryText.trim(),
      isUser: true,
      timestamp: new Date()
    };

    this.messages.push(userMessage);
    const query = this.queryText.trim();
    this.queryText = '';
    this.isLoading = true;

    const querySettings = this.querySettingsService.current;
    const request: QueryRequest = {
      query,
      topK: querySettings.topK,
      documentIds: this.selectedDocumentIds.length > 0 ? this.selectedDocumentIds : undefined,
      modelId: this.selectedModelId || undefined
    };

    this.chatService.query(request).subscribe({
      next: (response: QueryResponse) => {
        const aiMessage: ChatMessage = {
          id: this.generateId(),
          text: response.answer,
          isUser: false,
          timestamp: new Date(),
          sources: response.sources,
          processingTime: response.processingTimeMs
        };
        this.messages.push(aiMessage);
        this.isLoading = false;
      },
      error: () => {
        this.isLoading = false;
      }
    });
  }

  onKeyPress(event: KeyboardEvent): void {
    if (event.key === 'Enter' && !event.shiftKey) {
      event.preventDefault();
      this.sendMessage();
    }
  }

  clearChat(): void {
    this.messages = [];
  }

  onDocumentSelectionChange(selectedValues: string[]): void {
    this.selectedDocumentIds = selectedValues || [];
  }

  private generateId(): string {
    return `${Date.now()}-${Math.random().toString(36).substr(2, 9)}`;
  }

  formatTime(date: Date): string {
    return date.toLocaleTimeString([], { hour: '2-digit', minute: '2-digit' });
  }
  onModelSelectionChange(selectedModelId: any): void {
   if(!selectedModelId) {
     this.selectedModelId = selectedModelId.target.value;
   }
    console.log('Selected model ID:', selectedModelId);
  }
}

