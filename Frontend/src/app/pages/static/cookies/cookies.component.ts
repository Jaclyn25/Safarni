import { Component } from '@angular/core';
import { StaticPageLayoutComponent } from '../shared/static-page.layout';

@Component({
  selector: 'app-cookies',
  standalone: true,
  imports: [StaticPageLayoutComponent],
  template: `
    <app-static-page-layout title="Cookie Policy" subtitle="How we use cookies and similar technologies.">

      <div class="legal">
        <p class="legal-date">Last updated: August 1, 2026</p>

        <section class="legal-section">
          <h2 class="legal-h2">1. What Are Cookies</h2>
          <p class="legal-p">Cookies are small text files placed on your device when you visit a website. They help sites function, improve efficiency, and provide reporting information to operators.</p>
        </section>

        <section class="legal-section">
          <h2 class="legal-h2">2. How We Use Cookies</h2>
          <h3 class="legal-h3">Essential Cookies</h3>
          <p class="legal-p">Required for the Service to function. These enable authentication, security, and session management.</p>
          <h3 class="legal-h3">Preference Cookies</h3>
          <p class="legal-p">Remember your settings (language, currency, theme) to provide a more personalized experience.</p>
          <h3 class="legal-h3">Analytics Cookies</h3>
          <p class="legal-p">Help us understand how visitors interact with the Service by collecting anonymous usage data. This helps us improve the platform.</p>
        </section>

        <section class="legal-section">
          <h2 class="legal-h2">3. Third-Party Cookies</h2>
          <p class="legal-p">Some cookies are placed by third-party services on our pages:</p>
          <ul class="legal-ul">
            <li><strong>Stripe</strong> for payment processing</li>
            <li><strong>Analytics providers</strong> for usage data</li>
            <li><strong>Google Identity</strong> for authentication</li>
          </ul>
          <p class="legal-p">These third parties use cookies according to their own privacy policies. We do not control third-party cookies.</p>
        </section>

        <section class="legal-section">
          <h2 class="legal-h2">4. Managing Cookies</h2>
          <p class="legal-p">You can control cookies through your browser:</p>
          <ul class="legal-ul">
            <li><strong>Block all cookies:</strong> May prevent some features from working</li>
            <li><strong>Block third-party cookies:</strong> Most browsers support this while allowing first-party cookies</li>
            <li><strong>Delete cookies:</strong> Remove cookies already stored on your device</li>
          </ul>
          <p class="legal-p">Browser-specific instructions are available in your browser's help section.</p>
        </section>

        <section class="legal-section">
          <h2 class="legal-h2">5. Do Not Track</h2>
          <p class="legal-p">Some browsers offer a "Do Not Track" (DNT) feature. There is currently no universal standard for how companies should respond to DNT signals. We currently do not respond to DNT signals but will update this policy if a standard is adopted.</p>
        </section>

        <section class="legal-section">
          <h2 class="legal-h2">6. Changes to This Policy</h2>
          <p class="legal-p">We may update this Cookie Policy from time to time. Changes will be posted on this page with an updated revision date.</p>
        </section>

        <section class="legal-section">
          <h2 class="legal-h2">7. Contact</h2>
          <p class="legal-p">Questions about our use of cookies? Contact us at <a href="mailto:privacy&#64;safarni.com" class="legal-link">privacy&#64;safarni.com</a>.</p>
        </section>
      </div>
    </app-static-page-layout>
  `,
  styles: [`
    .legal {}

    .legal-date {
      font: 500 0.75rem/1 Manrope, sans-serif;
      color: var(--on-surface-variant);
      margin: 0 0 2rem;
    }

    .legal-section { margin-bottom: 2rem; }
    .legal-section:last-child { margin-bottom: 0; }

    .legal-h2 {
      font: 600 1.0625rem/1.4 Sora, sans-serif;
      color: var(--on-surface);
      margin: 0 0 0.625rem;
    }

    .legal-h3 {
      font: 600 0.9375rem/1.4 Manrope, sans-serif;
      color: var(--on-surface);
      margin: 1.25rem 0 0.375rem;
    }

    .legal-p {
      font: 400 0.875rem/1.7 Manrope, sans-serif;
      color: var(--on-surface-variant);
      margin: 0 0 0.5rem;
    }
    .legal-p:last-child { margin-bottom: 0; }

    .legal-ul {
      margin: 0.375rem 0 0.5rem;
      padding-left: 1.25rem;
      font: 400 0.875rem/1.7 Manrope, sans-serif;
      color: var(--on-surface-variant);
    }
    .legal-ul li { margin-bottom: 0.25rem; }
    .legal-ul li:last-child { margin-bottom: 0; }
    .legal-ul strong { color: var(--on-surface); font-weight: 600; }

    .legal-link {
      color: var(--primary);
      text-decoration: none;
    }
    .legal-link:hover { text-decoration: underline; }
  `]
})
export class CookiesComponent {}
