import { Component } from '@angular/core';
import { RouterLink } from '@angular/router';
import { StaticPageLayoutComponent } from '../shared/static-page.layout';

@Component({
  selector: 'app-privacy',
  standalone: true,
  imports: [RouterLink, StaticPageLayoutComponent],
  template: `
    <app-static-page-layout title="Privacy Policy" subtitle="How we collect, use, and protect your information.">

      <div class="legal">
        <p class="legal-date">Last updated: August 1, 2026</p>

        <section class="legal-section">
          <h2 class="legal-h2">1. Introduction</h2>
          <p class="legal-p">Welcome to Safarni ("we," "our," or "us"). We are committed to protecting your privacy. This Privacy Policy explains how we collect, use, disclose, and safeguard your information when you use our website, mobile application, and services.</p>
          <p class="legal-p">By using the Service, you agree to the collection and use of information in accordance with this policy. If you do not agree, please discontinue use.</p>
        </section>

        <section class="legal-section">
          <h2 class="legal-h2">2. Information We Collect</h2>
          <h3 class="legal-h3">Personal Information</h3>
          <p class="legal-p">We may collect information you provide directly:</p>
          <ul class="legal-ul">
            <li>Name, email address, and account credentials</li>
            <li>Travel preferences and trip itineraries</li>
            <li>Payment information (processed securely via Stripe)</li>
            <li>Profile information you choose to share</li>
          </ul>
          <h3 class="legal-h3">Usage Information</h3>
          <p class="legal-p">We automatically collect certain data when you use the Service:</p>
          <ul class="legal-ul">
            <li>Device type, browser, and operating system</li>
            <li>Pages viewed and features used</li>
            <li>IP address and approximate location</li>
            <li>Referring website or source</li>
          </ul>
        </section>

        <section class="legal-section">
          <h2 class="legal-h2">3. How We Use Your Information</h2>
          <ul class="legal-ul">
            <li>Provide, maintain, and improve the Service</li>
            <li>Generate personalized travel itineraries and recommendations</li>
            <li>Process transactions and send related information</li>
            <li>Send administrative notifications (trip updates, security alerts)</li>
            <li>Respond to inquiries and provide support</li>
            <li>Monitor usage patterns to improve experience</li>
            <li>Detect, prevent, and address technical issues and fraud</li>
          </ul>
        </section>

        <section class="legal-section">
          <h2 class="legal-h2">4. Information Sharing</h2>
          <p class="legal-p">We do not sell your personal information. We may share it with:</p>
          <ul class="legal-ul">
            <li><strong>Service Providers</strong> who perform services on our behalf (payment, hosting, analytics)</li>
            <li><strong>Travel Agencies</strong> when you book a package through our marketplace</li>
            <li><strong>Legal Authorities</strong> when required by law or valid legal process</li>
            <li><strong>Business Transfers</strong> in connection with a merger, acquisition, or asset sale</li>
          </ul>
        </section>

        <section class="legal-section">
          <h2 class="legal-h2">5. Data Security</h2>
          <p class="legal-p">We implement industry-standard security measures to protect your information. No method of transmission or storage is 100% secure, and we cannot guarantee absolute security.</p>
        </section>

        <section class="legal-section">
          <h2 class="legal-h2">6. Your Rights</h2>
          <p class="legal-p">Depending on your location, you may have the right to:</p>
          <ul class="legal-ul">
            <li>Access the personal information we hold about you</li>
            <li>Request correction of inaccurate data</li>
            <li>Request deletion of your personal information</li>
            <li>Object to or restrict processing of your data</li>
            <li>Receive your data in a portable, machine-readable format</li>
            <li>Withdraw consent at any time</li>
          </ul>
          <p class="legal-p">To exercise these rights, contact us at <a href="mailto:privacy&#64;safarni.com" class="legal-link">privacy&#64;safarni.com</a>.</p>
        </section>

        <section class="legal-section">
          <h2 class="legal-h2">7. Cookies and Tracking</h2>
          <p class="legal-p">We use cookies and similar technologies to maintain sessions, remember preferences, and analyze usage. You can control cookies through your browser settings. See our <a routerLink="/cookies" class="legal-link">Cookie Policy</a> for details.</p>
        </section>

        <section class="legal-section">
          <h2 class="legal-h2">8. Children's Privacy</h2>
          <p class="legal-p">The Service is not intended for children under 13. We do not knowingly collect information from children under 13. If we become aware of such collection, we will delete the data promptly.</p>
        </section>

        <section class="legal-section">
          <h2 class="legal-h2">9. Changes to This Policy</h2>
          <p class="legal-p">We may update this policy from time to time. Material changes will be posted on this page with an updated revision date. Continued use after changes constitutes acceptance.</p>
        </section>

        <section class="legal-section">
          <h2 class="legal-h2">10. Contact</h2>
          <p class="legal-p">Questions about this policy? Contact us at <a href="mailto:privacy&#64;safarni.com" class="legal-link">privacy&#64;safarni.com</a>.</p>
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
export class PrivacyComponent {}
