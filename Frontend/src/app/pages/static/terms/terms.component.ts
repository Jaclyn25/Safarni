import { Component } from '@angular/core';
import { LegalPageLayoutComponent } from '../shared/legal-page.layout';

@Component({
  selector: 'app-terms',
  standalone: true,
  imports: [LegalPageLayoutComponent],
  template: `
    <app-legal-page
      title="Terms of Service"
      intro="Guidelines for using Safarni's travel planning platform."
      updated="August 1, 2026"
      updatedIso="2026-08-01"
      [toc]="toc">

      <section id="t-acceptance">
        <h2>1. Acceptance of Terms</h2>
        <p>By accessing or using Safarni (the "Service"), you agree to these Terms. If you do not agree, do not use the Service.</p>
      </section>

      <section id="t-service">
        <h2>2. Description of Service</h2>
        <p>Safarni provides an AI-powered travel planning platform that helps users create personalized itineraries. The Service also includes a marketplace connecting users with verified travel agencies. The Service is provided "as is" and may be modified at any time.</p>
      </section>

      <section id="t-account">
        <h2>3. Account Registration</h2>
        <p>To access certain features, you must create an account. You agree to:</p>
        <ul>
          <li>Provide accurate and complete information</li>
          <li>Maintain the security of your password</li>
          <li>Accept responsibility for all activity under your account</li>
          <li>Notify us of any unauthorized access</li>
        </ul>
      </section>

      <section id="t-conduct">
        <h2>4. User Conduct</h2>
        <p>You agree not to:</p>
        <ul>
          <li>Use the Service for unlawful purposes</li>
          <li>Attempt unauthorized access to any part of the Service</li>
          <li>Interfere with or disrupt the Service</li>
          <li>Use automated systems to access the Service without permission</li>
          <li>Transmit harmful code or malicious content</li>
          <li>Impersonate another person</li>
        </ul>
      </section>

      <section id="t-ai">
        <h2>5. AI-Generated Content</h2>
        <p>The Service uses AI to generate travel recommendations and itineraries. This content is for informational purposes only. While we strive for accuracy, we do not guarantee AI-generated content is complete, current, or error-free. Verify all travel information independently before making booking decisions.</p>
      </section>

      <section id="t-marketplace">
        <h2>6. Marketplace and Bookings</h2>
        <p>When you book through our marketplace, you enter a direct contract with the travel agency. Safarni acts as an intermediary platform and is not a party to that agreement. Agency listings, descriptions, and pricing are provided by the agencies.</p>
      </section>

      <section id="t-payments">
        <h2>7. Payment Terms</h2>
        <p>Paid subscriptions and purchases are processed through Stripe. By providing payment information, you agree to Stripe's terms. All fees are non-refundable unless stated otherwise in our cancellation policy.</p>
      </section>

      <section id="t-ip">
        <h2>8. Intellectual Property</h2>
        <p>All content, features, and functionality of the Service are the exclusive property of Safarni or its licensors and are protected by copyright, trademark, and other intellectual property laws.</p>
      </section>

      <section id="t-liability">
        <h2>9. Limitation of Liability</h2>
        <p>To the maximum extent permitted by law, Safarni shall not be liable for any indirect, incidental, special, consequential, or punitive damages, or any loss of profits, revenues, data, or goodwill resulting from your use of the Service.</p>
      </section>

      <section id="t-termination">
        <h2>10. Termination</h2>
        <p>We may terminate or suspend your account at our discretion, without prior notice, for conduct that violates these Terms or is harmful to users, us, or third parties.</p>
      </section>

      <section id="t-law">
        <h2>11. Governing Law</h2>
        <p>These Terms are governed by applicable laws without regard to conflict of law principles. Disputes shall be resolved in competent courts of the applicable jurisdiction.</p>
      </section>

      <section id="t-changes">
        <h2>12. Changes to These Terms</h2>
        <p>We may modify these Terms at any time. Material changes will be posted on this page. Continued use after changes take effect constitutes acceptance.</p>
      </section>

      <section id="t-contact">
        <h2>13. Contact</h2>
        <p>Questions about these Terms? Contact us at <a href="mailto:legal&#64;safarni.com">legal&#64;safarni.com</a>.</p>
      </section>

    </app-legal-page>
  `
})
export class TermsComponent {
  toc = [
    { id: 't-acceptance', label: '1. Acceptance of Terms' },
    { id: 't-service', label: '2. Description of Service' },
    { id: 't-account', label: '3. Account Registration' },
    { id: 't-conduct', label: '4. User Conduct' },
    { id: 't-ai', label: '5. AI-Generated Content' },
    { id: 't-marketplace', label: '6. Marketplace and Bookings' },
    { id: 't-payments', label: '7. Payment Terms' },
    { id: 't-ip', label: '8. Intellectual Property' },
    { id: 't-liability', label: '9. Limitation of Liability' },
    { id: 't-termination', label: '10. Termination' },
    { id: 't-law', label: '11. Governing Law' },
    { id: 't-changes', label: '12. Changes to These Terms' },
    { id: 't-contact', label: '13. Contact' }
  ];
}
