import { Component } from '@angular/core';
import { StaticPageLayoutComponent } from '../shared/static-page.layout';

@Component({
  selector: 'app-terms',
  standalone: true,
  imports: [StaticPageLayoutComponent],
  template: `
    <app-static-page-layout title="Terms of Service" subtitle="Guidelines for using Safarni's travel planning platform.">

      <div class="legal">
        <p class="legal-date">Last updated: August 1, 2026</p>

        <section class="legal-section">
          <h2 class="legal-h2">1. Acceptance of Terms</h2>
          <p class="legal-p">By accessing or using Safarni (the "Service"), you agree to these Terms. If you do not agree, do not use the Service.</p>
        </section>

        <section class="legal-section">
          <h2 class="legal-h2">2. Description of Service</h2>
          <p class="legal-p">Safarni provides an AI-powered travel planning platform that helps users create personalized itineraries. The Service also includes a marketplace connecting users with verified travel agencies. The Service is provided "as is" and may be modified at any time.</p>
        </section>

        <section class="legal-section">
          <h2 class="legal-h2">3. Account Registration</h2>
          <p class="legal-p">To access certain features, you must create an account. You agree to:</p>
          <ul class="legal-ul">
            <li>Provide accurate and complete information</li>
            <li>Maintain the security of your password</li>
            <li>Accept responsibility for all activity under your account</li>
            <li>Notify us of any unauthorized access</li>
          </ul>
        </section>

        <section class="legal-section">
          <h2 class="legal-h2">4. User Conduct</h2>
          <p class="legal-p">You agree not to:</p>
          <ul class="legal-ul">
            <li>Use the Service for unlawful purposes</li>
            <li>Attempt unauthorized access to any part of the Service</li>
            <li>Interfere with or disrupt the Service</li>
            <li>Use automated systems to access the Service without permission</li>
            <li>Transmit harmful code or malicious content</li>
            <li>Impersonate another person</li>
          </ul>
        </section>

        <section class="legal-section">
          <h2 class="legal-h2">5. AI-Generated Content</h2>
          <p class="legal-p">The Service uses AI to generate travel recommendations and itineraries. This content is for informational purposes only. While we strive for accuracy, we do not guarantee AI-generated content is complete, current, or error-free. Verify all travel information independently before making booking decisions.</p>
        </section>

        <section class="legal-section">
          <h2 class="legal-h2">6. Marketplace and Bookings</h2>
          <p class="legal-p">When you book through our marketplace, you enter a direct contract with the travel agency. Safarni acts as an intermediary platform and is not a party to that agreement. Agency listings, descriptions, and pricing are provided by the agencies.</p>
        </section>

        <section class="legal-section">
          <h2 class="legal-h2">7. Payment Terms</h2>
          <p class="legal-p">Paid subscriptions and purchases are processed through Stripe. By providing payment information, you agree to Stripe's terms. All fees are non-refundable unless stated otherwise in our cancellation policy.</p>
        </section>

        <section class="legal-section">
          <h2 class="legal-h2">8. Intellectual Property</h2>
          <p class="legal-p">All content, features, and functionality of the Service are the exclusive property of Safarni or its licensors and are protected by copyright, trademark, and other intellectual property laws.</p>
        </section>

        <section class="legal-section">
          <h2 class="legal-h2">9. Limitation of Liability</h2>
          <p class="legal-p">To the maximum extent permitted by law, Safarni shall not be liable for any indirect, incidental, special, consequential, or punitive damages, or any loss of profits, revenues, data, or goodwill resulting from your use of the Service.</p>
        </section>

        <section class="legal-section">
          <h2 class="legal-h2">10. Termination</h2>
          <p class="legal-p">We may terminate or suspend your account at our discretion, without prior notice, for conduct that violates these Terms or is harmful to users, us, or third parties.</p>
        </section>

        <section class="legal-section">
          <h2 class="legal-h2">11. Governing Law</h2>
          <p class="legal-p">These Terms are governed by applicable laws without regard to conflict of law principles. Disputes shall be resolved in competent courts of the applicable jurisdiction.</p>
        </section>

        <section class="legal-section">
          <h2 class="legal-h2">12. Changes to These Terms</h2>
          <p class="legal-p">We may modify these Terms at any time. Material changes will be posted on this page. Continued use after changes take effect constitutes acceptance.</p>
        </section>

        <section class="legal-section">
          <h2 class="legal-h2">13. Contact</h2>
          <p class="legal-p">Questions about these Terms? Contact us at <a href="mailto:legal&#64;safarni.com" class="legal-link">legal&#64;safarni.com</a>.</p>
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

    .legal-link {
      color: var(--primary);
      text-decoration: none;
    }
    .legal-link:hover { text-decoration: underline; }
  `]
})
export class TermsComponent {}
