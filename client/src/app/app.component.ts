import { Component, ElementRef, HostListener, ViewChild, inject, OnDestroy, OnInit } from '@angular/core';
import { Router, RouterOutlet } from '@angular/router';
import { ToastComponent } from './shared/components/toast/toast.component';
import { CommonModule } from '@angular/common';
import { FormsModule } from '@angular/forms';
import { AuthService } from './core/services/auth.service';
import { NotificationService } from './shared/services/notification.service';
import { QueryMode, QuerySettingsService } from './shared/services/query-settings.service';
import { finalize, Subscription } from 'rxjs';

@Component({
  selector: 'app-root',
  standalone: true,
  imports: [RouterOutlet, ToastComponent, CommonModule, FormsModule],
  templateUrl: './app.component.html',
  styleUrls: ['./app.component.scss']
})
export class AppComponent implements OnInit, OnDestroy {

  @ViewChild('introVideo')
  introVideo!: ElementRef<HTMLVideoElement>;

  private authService = inject(AuthService);
  private router = inject(Router);
  private notificationService = inject(NotificationService);
  private querySettingsService = inject(QuerySettingsService);
  private authSubscription?: Subscription;

  isLoggedIn = this.authService.isLoggedIn();
  showVideoModal = !this.isLoggedIn;
  email = '';
  password = '';
  loginError = '';
  isLoggingIn = false;
  dataInputModalOpen = false;
  selectedQueryMode: QueryMode = 'balanced';
  chunkingSize = 512;
  overlapSize = 50;
  topKSize = 5;

  ngOnInit(): void {
    this.authSubscription = this.authService.authChanged$.subscribe(loggedIn => {
      this.isLoggedIn = loggedIn;

      if (!loggedIn) {
        this.email = '';
        this.password = '';
        this.loginError = '';
        this.showVideoModal = true;
        setTimeout(() => this.playVideo(), 100);
        return;
      }

      this.router.navigateByUrl('/', { skipLocationChange: true }).then(() => {
        this.router.navigate(['/upload']);
      });
    });
  }

  ngOnDestroy(): void {
    this.authSubscription?.unsubscribe();
  }

  ngAfterViewInit(): void {
    if (this.showVideoModal) {
      this.playVideo();
    }
  }

  playVideo(): void {
    setTimeout(() => {
      const video = this.introVideo?.nativeElement;

      if (video) {
        video.muted = true;

        video.play().catch(err => {
          console.error('Video autoplay failed', err);
        });
      }
    }, 100);
  }

  login(): void {
    if (!this.email.trim() || !this.password) {
      this.loginError = 'Email and password are required.';
      return;
    }

    this.loginError = '';
    this.isLoggingIn = true;

    this.authService.login({ email: this.email.trim(), password: this.password })
      .pipe(finalize(() => { this.isLoggingIn = false; }))
      .subscribe({
        next: response => {
          if (response.success) {
            this.closeModal();
          } else {
            this.loginError = response.message || 'Login failed.';
          }
        },
        error: err => {
          this.loginError = err.error?.message || 'Login failed. Please check your credentials.';
        }
      });
  }

  closeModal(): void {
    const video = this.introVideo?.nativeElement;
    if (video) {
      video.pause();
    }
    this.showVideoModal = false;
  }

  openInfoModal(): void {
    const settings = this.querySettingsService.current;
    this.selectedQueryMode = settings.mode;
    this.topKSize = settings.topK;
    this.chunkingSize = settings.chunkSize;
    this.overlapSize = settings.chunkOverlap;
    this.dataInputModalOpen = true;
  }

  closeInfoModal(): void {
    this.dataInputModalOpen = false;
  }

  closeInfoModalOnBackdrop(event: MouseEvent): void {
    if (event.target === event.currentTarget) {
      this.closeInfoModal();
    }
  }

  saveQuerySettings(): void {
    this.querySettingsService.save({
      mode: this.selectedQueryMode,
      topK: this.topKSize,
      chunkSize: this.chunkingSize,
      chunkOverlap: this.overlapSize
    }).subscribe({
      next: savedSettings => {
        this.authService.updateQuerySettings(savedSettings);
        this.notificationService.showSuccess('Query value settings saved.');
        this.closeInfoModal();
      },
      error: () => this.notificationService.showError('Unable to save query value settings.')
    });
  }

  onQueryModeChange(): void {
    const settings = this.querySettingsService.getModeSettings(this.selectedQueryMode);
    this.topKSize = settings.topK;
    this.chunkingSize = settings.chunkSize;
    this.overlapSize = settings.chunkOverlap;
  }

  clearQuerySettings(): void {
    this.selectedQueryMode = 'balanced';
    this.onQueryModeChange();
  }

  @HostListener('document:keydown.escape')
  onEscape(): void {
    if (this.dataInputModalOpen) {
      this.closeInfoModal();
    }
  }

  onVideoReady(): void {
    this.introVideo.nativeElement.play().catch(() => {});
  }
}
