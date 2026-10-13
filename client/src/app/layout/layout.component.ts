import { Component, inject } from '@angular/core';
import { CommonModule } from '@angular/common';
import { RouterModule, RouterOutlet } from '@angular/router';
import { AuthService } from '../core/services/auth.service';

@Component({
  selector: 'app-layout',
  standalone: true,
  imports: [CommonModule, RouterModule, RouterOutlet],
  templateUrl: './layout.component.html',
  styleUrls: ['./layout.component.scss']
})
export class LayoutComponent {
  private authService = inject(AuthService);

  menuItems = [
    // { path: '/documents', label: 'Documents', icon: '📄' },
    // { path: '/upload', label: 'Upload', icon: '⬆️' },
    // { path: '/chat', label: 'Chat', icon: '💬' },
    // { path: '/reports', label: 'Reports', icon: '📊' },
    // { path: '/embeddings', label: 'Embeddings', icon: '🔍' }
    { path: '/upload', label: 'Upload', icon: 'assets/icon-upload.png' },
    { path: '/chat', label: 'Chat', icon: 'assets/icon-chat.png' },
    { path: '/query-logs', label: 'Query Logs', icon: 'assets/icon-query.png' },
    { path: '/reports', label: 'Reports', icon: 'assets/icon-reports.png' },
    { path: '/embeddings', label: 'Embeddings', icon: 'assets/icon-embeddings.png' },
    { path: '/architecture-docs', label: 'Architecture Docs', icon: 'assets/icon-docs.png' }
  ];

  get userDisplayName(): string {
    const user = this.authService.getUser();
    if (!user) {
      return '';
    }
    return `${user.firstName} ${user.lastName}`.trim();
  }

  logout(): void {
    this.authService.logout();
  }
}

