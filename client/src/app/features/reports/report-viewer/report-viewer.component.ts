import { Component, Input } from '@angular/core';
import { CommonModule } from '@angular/common';
import { ReportResponse, SourceReference } from '../../../models/document.models';

@Component({
  selector: 'app-report-viewer',
  standalone: true,
  imports: [CommonModule],
  templateUrl: './report-viewer.component.html',
  styleUrls: ['./report-viewer.component.scss']
})
export class ReportViewerComponent {
  @Input() report!: ReportResponse;

  downloadReport(): void {
    const content = this.formatReportForDownload();
    const blob = new Blob([content], { type: 'text/plain' });
    const url = window.URL.createObjectURL(blob);
    const link = document.createElement('a');
    link.href = url;
    link.download = `report-${this.report.topic.replace(/[^a-z0-9]/gi, '-').toLowerCase()}-${new Date().toISOString().split('T')[0]}.txt`;
    document.body.appendChild(link);
    link.click();
    document.body.removeChild(link);
    window.URL.revokeObjectURL(url);
  }

  formatReportForDownload(): string {
    let content = `REPORT: ${this.report.topic}\n`;
    content += `Generated: ${new Date(this.report.generatedAt).toLocaleString()}\n`;
    content += `Processing Time: ${this.report.processingTimeMs.toFixed(0)}ms\n\n`;
    content += `EXECUTIVE SUMMARY\n`;
    content += `${'='.repeat(50)}\n`;
    content += `${this.report.report.executiveSummary}\n\n`;
    content += `KEY FINDINGS\n`;
    content += `${'='.repeat(50)}\n`;
    this.report.report.keyFindings.forEach((finding, index) => {
      content += `${index + 1}. ${finding}\n`;
    });
    content += `\nRISKS\n`;
    content += `${'='.repeat(50)}\n`;
    this.report.report.risks.forEach((risk, index) => {
      content += `${index + 1}. ${risk}\n`;
    });
    content += `\nRECOMMENDATIONS\n`;
    content += `${'='.repeat(50)}\n`;
    this.report.report.recommendations.forEach((rec, index) => {
      content += `${index + 1}. ${rec}\n`;
    });
    content += `\nSOURCES\n`;
    content += `${'='.repeat(50)}\n`;
    this.report.sources.forEach((source, index) => {
      content += `${index + 1}. ${source.documentName} (${(source.relevanceScore * 100).toFixed(1)}% match)\n`;
      content += `   ${source.excerpt}\n\n`;
    });
    return content;
  }

  formatDate(dateString: string): string {
    return new Date(dateString).toLocaleString();
  }
}

