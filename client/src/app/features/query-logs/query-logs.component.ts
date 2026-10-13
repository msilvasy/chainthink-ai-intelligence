import { Component, OnInit, inject } from '@angular/core';
import { CommonModule } from '@angular/common';
import { FormsModule } from '@angular/forms';
import { ChatService } from '../../core/services/chat.service';
import { QueryLog } from '../../models/document.models';
import { LoadingSpinnerComponent } from '../../shared/components/loading-spinner/loading-spinner.component';

@Component({
  selector: 'app-query-logs',
  standalone: true,
  imports: [CommonModule, FormsModule, LoadingSpinnerComponent],
  templateUrl: './query-logs.component.html',
  styleUrls: ['./query-logs.component.scss']
})
export class QueryLogsComponent implements OnInit {
  private chatService = inject(ChatService);

  logs: QueryLog[] = [];
  userId = '';
  isLoading = false;
  expandedLogId: number | null = null;

  ngOnInit(): void {
    this.loadLogs();
  }

  loadLogs(): void {
    this.isLoading = true;
    this.expandedLogId = null;
    this.chatService.getQueryLogs(this.userId.trim() || undefined).subscribe({
      next: logs => {
        this.logs = logs;
        this.isLoading = false;
      },
      error: () => {
        this.logs = [];
        this.isLoading = false;
      }
    });
  }

  clearFilter(): void {
    this.userId = '';
    this.loadLogs();
  }

  toggleDetails(logId: number): void {
    this.expandedLogId = this.expandedLogId === logId ? null : logId;
  }

  get chartLogs(): QueryLog[] {
    return this.logs.slice(0, 10).reverse();
  }

  processingWidth(processingTimeMs: number): number {
    const maximum = Math.max(...this.chartLogs.map(log => log.processingTimeMs), 1);
    return (processingTimeMs / maximum) * 100;
  }

  relevanceWidth(relevanceScore: number | null): number {
    if (relevanceScore === null) {
      return 0;
    }

    const maximum = Math.max(...this.chartLogs.map(log => log.relevanceScore ?? 0), 1);
    return Math.max(0, relevanceScore / maximum) * 100;
  }

  formatProcessingTime(processingTimeMs: number): string {
    return `${(processingTimeMs / 1000).toFixed(1)} `;
  }
}
