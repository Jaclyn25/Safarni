import { Component, ElementRef, ViewChild } from '@angular/core';
import { LegalPageLayoutComponent } from '../shared/legal-page.layout';

@Component({
  selector: 'app-cookies',
  standalone: true,
  imports: [LegalPageLayoutComponent],
  template: `
    <app-legal-page
      title="Cookie Policy"
      intro="How we use cookies and similar technologies, and how you can control them."
      updated="August 1, 2026"
      updatedIso="2026-08-01"
      [toc]="toc">

      <section id="c-what">
        <h2>1. What Are Cookies</h2>
        <p>Cookies are small text files placed on your device when you visit a website. They help sites function, improve efficiency, and provide reporting information to operators.</p>
      </section>

      <section id="c-how">
        <h2>2. How We Use Cookies</h2>
        <h3>Essential Cookies</h3>
        <p>Required for the Service to function. These enable authentication, security, and session management.</p>
        <h3>Preference Cookies</h3>
        <p>Remember your settings (language, currency, theme) to provide a more personalized experience.</p>
        <h3>Analytics Cookies</h3>
        <p>Help us understand how visitors interact with the Service by collecting anonymous usage data. This helps us improve the platform.</p>
      </section>

      <section id="c-third-party">
        <h2>3. Third-Party Cookies</h2>
        <p>Some cookies are placed by third-party services on our pages:</p>
        <ul>
          <li><strong>Stripe</strong> for payment processing</li>
          <li><strong>Analytics providers</strong> for usage data</li>
          <li><strong>Google Identity</strong> for authentication</li>
        </ul>
        <p>These third parties use cookies according to their own privacy policies. We do not control third-party cookies.</p>
      </section>

      <section id="c-manage">
        <h2>4. Managing Cookies</h2>
        <p>You can control cookies through your browser:</p>
        <ul>
          <li><strong>Block all cookies:</strong> May prevent some features from working</li>
          <li><strong>Block third-party cookies:</strong> Most browsers support this while allowing first-party cookies</li>
          <li><strong>Delete cookies:</strong> Remove cookies already stored on your device</li>
        </ul>
        <p>Browser-specific instructions are available in your browser's help section. You can also review a summary of the categories we use:</p>
        <p>
          <button type="button" (click)="togglePreferences()" [attr.aria-expanded]="showPreferences"
            aria-controls="cookies-preferences-panel" class="pub-btn pub-btn--primary pub-btn--sm">
            <span class="material-symbols-outlined" style="font-size:16px">tune</span>
            {{ showPreferences ? 'Hide cookie summary' : 'View cookie summary' }}
          </button>
        </p>

        @if (showPreferences) {
          <section #prefPanel id="cookies-preferences-panel" tabindex="-1" aria-label="Cookie category summary"
            class="cookies-preferences">
            <h3>Cookie categories on this site</h3>
            <ul>
              <li><strong>Essential</strong> &mdash; always active. Required for authentication, security, and sessions.</li>
              <li><strong>Preferences</strong> &mdash; active by default. Remember language, currency, and theme choices.</li>
              <li><strong>Analytics</strong> &mdash; active by default. Anonymous usage data that helps us improve the platform.</li>
            </ul>
            <p>
              A full consent preference center will be available once we roll out the cookie banner. Until then, you can
              block or delete cookies through your browser settings at any time.
            </p>
          </section>
        }
      </section>

      <section id="c-dnt">
        <h2>5. Do Not Track</h2>
        <p>Some browsers offer a "Do Not Track" (DNT) feature. There is currently no universal standard for how companies should respond to DNT signals. We currently do not respond to DNT signals but will update this policy if a standard is adopted.</p>
      </section>

      <section id="c-changes">
        <h2>6. Changes to This Policy</h2>
        <p>We may update this Cookie Policy from time to time. Changes will be posted on this page with an updated revision date.</p>
      </section>

      <section id="c-contact">
        <h2>7. Contact</h2>
        <p>Questions about our use of cookies? Contact us at <a href="mailto:privacy&#64;safarni.com">privacy&#64;safarni.com</a>.</p>
      </section>

    </app-legal-page>
  `,
  styles: [`
    .cookies-preferences {
      margin-block-start: 1.25rem;
      padding: 1.25rem 1.5rem;
      border: 1px solid var(--outline-variant);
      border-radius: 14px;
      background: var(--surface-container-lowest);
      box-shadow: 0 0 0 3px color-mix(in srgb, var(--primary) 8%, transparent);
    }
    .cookies-preferences:focus { outline: none; }
  `]
})
export class CookiesComponent {
  @ViewChild('prefPanel') private prefPanel?: ElementRef<HTMLElement>;

  showPreferences = false;

  toc = [
    { id: 'c-what', label: '1. What Are Cookies' },
    { id: 'c-how', label: '2. How We Use Cookies' },
    { id: 'c-third-party', label: '3. Third-Party Cookies' },
    { id: 'c-manage', label: '4. Managing Cookies' },
    { id: 'c-dnt', label: '5. Do Not Track' },
    { id: 'c-changes', label: '6. Changes to This Policy' },
    { id: 'c-contact', label: '7. Contact' }
  ];

  togglePreferences(): void {
    this.showPreferences = !this.showPreferences;
    if (this.showPreferences) {
      setTimeout(() => {
        this.prefPanel?.nativeElement.scrollIntoView({ behavior: 'smooth', block: 'center' });
        this.prefPanel?.nativeElement.focus();
      });
    }
  }
}
