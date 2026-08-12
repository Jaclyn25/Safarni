import { Component, signal } from '@angular/core';
import { CommonModule } from '@angular/common';
import { FormsModule } from '@angular/forms';
import { EditorialPageLayoutComponent } from '../shared/editorial-page.layout';

@Component({
  selector: 'app-contact',
  standalone: true,
  imports: [CommonModule, FormsModule, EditorialPageLayoutComponent],
  template: `
    <app-editorial-page
      title="Contact"
      intro="Questions, feedback, or partnership ideas? We're here."
      kicker="Contact">

      <div class="contact">

        <!-- Quick links -->
        <section class="contact-channels">
          <div class="contact-channel">
            <span class="material-symbols-outlined contact-channel__icon">mail</span>
            <div>
              <h3 class="contact-channel__label">General</h3>
              <a href="mailto:hello&#64;safarni.com" class="contact-channel__value">hello&#64;safarni.com</a>
            </div>
          </div>
          <div class="contact-channel">
            <span class="material-symbols-outlined contact-channel__icon">support_agent</span>
            <div>
              <h3 class="contact-channel__label">Support</h3>
              <a href="mailto:support&#64;safarni.com" class="contact-channel__value">support&#64;safarni.com</a>
            </div>
          </div>
          <div class="contact-channel">
            <span class="material-symbols-outlined contact-channel__icon">handshake</span>
            <div>
              <h3 class="contact-channel__label">Partnerships</h3>
              <a href="mailto:partners&#64;safarni.com" class="contact-channel__value">partners&#64;safarni.com</a>
            </div>
          </div>
        </section>

        <!-- Form -->
        <section class="pub-section">
          <h2 class="pub-heading">Send a message</h2>
          <form class="contact-form" (ngSubmit)="submitForm()">
            <div class="contact-row">
              <label class="pub-field">
                <span class="pub-label">Name</span>
                <input type="text" [(ngModel)]="formName" name="name" required class="pub-input" placeholder="Your name" />
              </label>
              <label class="pub-field">
                <span class="pub-label">Email</span>
                <input type="email" [(ngModel)]="formEmail" name="email" required class="pub-input" placeholder="your&#64;email.com" />
              </label>
            </div>

            <label class="pub-field">
              <span class="pub-label">Subject</span>
              <select [(ngModel)]="formSubject" name="subject" class="pub-input pub-select">
                <option value="">Select a topic</option>
                <option value="general">General inquiry</option>
                <option value="support">Support</option>
                <option value="feedback">Feedback</option>
                <option value="partnership">Partnership</option>
                <option value="press">Press</option>
              </select>
            </label>

            <label class="pub-field">
              <span class="pub-label">Message</span>
              <textarea [(ngModel)]="formMessage" name="message" required rows="5" class="pub-input pub-textarea" placeholder="How can we help?"></textarea>
            </label>

            @if (formSuccess()) {
              <div class="pub-status pub-status--success" role="status">
                <span class="material-symbols-outlined" style="font-size:18px">check_circle</span>
                Thanks! We'll respond within 24 hours.
              </div>
            }

            <button type="submit" class="pub-btn pub-btn--primary contact-submit" [disabled]="isSubmitting()">
              @if (isSubmitting()) {
                <span class="material-symbols-outlined animate-spin" style="font-size:16px">progress_activity</span>
                Sending
              } @else {
                Send message
              }
            </button>
          </form>
        </section>

        <!-- Social -->
        <section class="contact-social">
          <div class="contact-social__divider"></div>
          <p class="contact-social__text">Or find us on social media</p>
          <div class="contact-social__links">
            @for (s of socials; track s.label) {
              <a [href]="s.url" [attr.aria-label]="s.label" class="contact-social__link">
                <span [innerHTML]="s.svg"></span>
              </a>
            }
          </div>
        </section>

      </div>
    </app-editorial-page>
  `,
  styles: [`
    /* Channels */
    .contact-channels {
      display: grid;
      grid-template-columns: repeat(3, 1fr);
      gap: 1px;
      background: var(--outline-variant);
      border-radius: 12px;
      overflow: hidden;
      margin-block-end: 3rem;
    }

    .contact-channel {
      display: flex;
      align-items: flex-start;
      gap: 0.75rem;
      padding: 1.25rem;
      background: var(--surface-container-lowest);
    }

    .contact-channel__icon {
      font-size: 20px;
      color: var(--primary);
      flex-shrink: 0;
      margin-block-start: 0.125rem;
    }

    .contact-channel__label {
      font: 600 0.875rem/1.33 Manrope, sans-serif;
      color: var(--on-surface);
      margin: 0 0 0.125rem;
    }

    .contact-channel__value {
      font: 400 0.875rem/1.4 Manrope, sans-serif;
      color: var(--primary);
      text-decoration: none;
    }
    .contact-channel__value:hover { text-decoration: underline; }

    /* Form */
    .contact-form {
      display: flex;
      flex-direction: column;
      gap: 1.25rem;
      max-width: 45rem;
    }

    .contact-row {
      display: grid;
      grid-template-columns: 1fr 1fr;
      gap: 1rem;
    }

    .contact-submit { align-self: flex-start; }
    .contact-submit:disabled { opacity: 0.6; cursor: not-allowed; }

    /* Social */
    .contact-social { margin-block-start: 3rem; text-align: center; }

    .contact-social__divider {
      width: 3rem;
      height: 1px;
      background: var(--outline-variant);
      margin: 0 auto 1.25rem;
    }

    .contact-social__text {
      font: 400 0.875rem/1.5 Manrope, sans-serif;
      color: var(--on-surface-variant);
      margin: 0 0 0.875rem;
    }

    .contact-social__links {
      display: flex;
      justify-content: center;
      gap: 0.5rem;
    }

    .contact-social__link {
      display: inline-grid;
      place-items: center;
      width: 2.75rem;
      height: 2.75rem;
      border-radius: 12px;
      border: 1px solid var(--outline-variant);
      color: var(--on-surface-variant);
      text-decoration: none;
      transition: border-color 0.15s, color 0.15s;
    }
    .contact-social__link:hover {
      border-color: var(--primary);
      color: var(--primary);
    }

    @media (max-width: 640px) {
      .contact-channels { grid-template-columns: 1fr; }
      .contact-row { grid-template-columns: 1fr; }
    }
  `]
})
export class ContactComponent {
  formName = '';
  formEmail = '';
  formSubject = '';
  formMessage = '';

  isSubmitting = signal(false);
  formSuccess = signal(false);

  socials = [
    { label: 'Instagram', url: '#', svg: '<svg width="18" height="18" fill="currentColor" viewBox="0 0 24 24"><path d="M12 2.163c3.204 0 3.584.012 4.85.07 3.252.148 4.771 1.691 4.919 4.919.058 1.265.069 1.645.069 4.849 0 3.205-.012 3.584-.069 4.849-.149 3.225-1.664 4.771-4.919 4.919-1.266.058-1.644.07-4.85.07-3.204 0-3.584-.012-4.849-.07-3.26-.149-4.771-1.699-4.919-4.92-.058-1.265-.07-1.644-.07-4.849 0-3.204.013-3.583.07-4.849.149-3.227 1.664-4.771 4.919-4.919 1.266-.057 1.645-.069 4.849-.069zM12 0C8.741 0 8.333.014 7.053.072 2.695.272.273 2.69.073 7.052.014 8.333 0 8.741 0 12c0 3.259.014 3.668.072 4.948.2 4.358 2.618 6.78 6.98 6.98C8.333 23.986 8.741 24 12 24c3.259 0 3.668-.014 4.948-.072 4.354-.2 6.782-2.618 6.979-6.98.059-1.28.073-1.689.073-4.948 0-3.259-.014-3.667-.072-4.947-.196-4.354-2.617-6.78-6.979-6.98C15.668.014 15.259 0 12 0zm0 5.838a6.162 6.162 0 100 12.324 6.162 6.162 0 000-12.324zM12 16a4 4 0 110-8 4 4 0 010 8zm6.406-11.845a1.44 1.44 0 100 2.881 1.44 1.44 0 000-2.881z"/></svg>' },
    { label: 'Twitter', url: '#', svg: '<svg width="18" height="18" fill="currentColor" viewBox="0 0 24 24"><path d="M18.244 2.25h3.308l-7.227 8.26 8.502 11.24H16.17l-5.214-6.817L4.99 21.75H1.68l7.73-8.835L1.254 2.25H8.08l4.713 6.231zm-1.161 17.52h1.833L7.084 4.126H5.117z"/></svg>' },
    { label: 'YouTube', url: '#', svg: '<svg width="18" height="18" fill="currentColor" viewBox="0 0 24 24"><path d="M23.498 6.186a3.016 3.016 0 00-2.122-2.136C19.505 3.545 12 3.545 12 3.545s-7.505 0-9.377.505A3.017 3.017 0 00.502 6.186C0 8.07 0 12 0 12s0 3.93.502 5.814a3.016 3.016 0 002.122 2.136c1.871.505 9.376.505 9.376.505s7.505 0 9.377-.505a3.015 3.015 0 002.122-2.136C24 15.93 24 12 24 12s0-3.93-.502-5.814zM9.545 15.568V8.432L15.818 12l-6.273 3.568z"/></svg>' },
    { label: 'TikTok', url: '#', svg: '<svg width="18" height="18" fill="currentColor" viewBox="0 0 24 24"><path d="M12.525.02c1.31-.02 2.61-.01 3.91-.02.08 1.53.63 3.09 1.75 4.17 1.12 1.11 2.7 1.62 4.24 1.79v4.03c-1.44-.05-2.89-.35-4.2-.97-.57-.26-1.1-.59-1.62-.93-.01 2.92.01 5.84-.02 8.75-.08 1.4-.54 2.79-1.35 3.94-1.31 1.92-3.58 3.17-5.91 3.21-1.43.08-2.86-.31-4.08-1.03-2.02-1.19-3.44-3.37-3.65-5.71-.02-.5-.03-1-.01-1.49.18-1.9 1.12-3.72 2.58-4.96 1.66-1.44 3.98-2.13 6.15-1.72.02 1.48-.04 2.96-.04 4.44-.99-.32-2.15-.23-3.02.37-.63.41-1.11 1.04-1.36 1.75-.21.51-.15 1.07-.14 1.61.24 1.64 1.82 3.02 3.5 2.87 1.12-.01 2.19-.66 2.77-1.61.19-.33.4-.67.41-1.06.1-1.79.06-3.57.07-5.36.01-4.03-.01-8.05.02-12.07z"/></svg>' }
  ];

  submitForm(): void {
    if (this.isSubmitting()) return;
    this.isSubmitting.set(true);

    setTimeout(() => {
      this.isSubmitting.set(false);
      this.formSuccess.set(true);
      this.formName = '';
      this.formEmail = '';
      this.formSubject = '';
      this.formMessage = '';
      setTimeout(() => this.formSuccess.set(false), 4000);
    }, 1200);
  }
}
