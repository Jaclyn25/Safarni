import { Component } from '@angular/core';
import { RouterLink } from '@angular/router';
import { LegalPageLayoutComponent } from '../shared/legal-page.layout';

@Component({
  selector: 'app-privacy',
  standalone: true,
  imports: [RouterLink, LegalPageLayoutComponent],
  template: `
    <app-legal-page
      title="Privacy Policy"
      intro="How we collect, use, and protect your information."
      updated="August 1, 2026"
      updatedIso="2026-08-01"
      [toc]="toc">

      <section id="p-intro">
        <h2>1. Introduction</h2>
        <p>Welcome to Safarni ("we," "our," or "us"). We are committed to protecting your privacy. This Privacy Policy explains how we collect, use, disclose, and safeguard your information when you use our website, mobile application, and services.</p>
        <p>By using the Service, you agree to the collection and use of information in accordance with this policy. If you do not agree, please discontinue use.</p>
      </section>

      <section id="p-collect">
        <h2>2. Information We Collect</h2>
        <h3>Personal Information</h3>
        <p>We may collect information you provide directly:</p>
        <ul>
          <li>Name, email address, and account credentials</li>
          <li>Travel preferences and trip itineraries</li>
          <li>Payment information (processed securely via Stripe)</li>
          <li>Profile information you choose to share</li>
        </ul>
        <h3>Usage Information</h3>
        <p>We automatically collect certain data when you use the Service:</p>
        <ul>
          <li>Device type, browser, and operating system</li>
          <li>Pages viewed and features used</li>
          <li>IP address and approximate location</li>
          <li>Referring website or source</li>
        </ul>
      </section>

      <section id="p-use">
        <h2>3. How We Use Your Information</h2>
        <ul>
          <li>Provide, maintain, and improve the Service</li>
          <li>Generate personalized travel itineraries and recommendations</li>
          <li>Process transactions and send related information</li>
          <li>Send administrative notifications (trip updates, security alerts)</li>
          <li>Respond to inquiries and provide support</li>
          <li>Monitor usage patterns to improve experience</li>
          <li>Detect, prevent, and address technical issues and fraud</li>
        </ul>
      </section>

      <section id="p-share">
        <h2>4. Information Sharing</h2>
        <p>We do not sell your personal information. We may share it with:</p>
        <ul>
          <li><strong>Service Providers</strong> who perform services on our behalf (payment, hosting, analytics)</li>
          <li><strong>Travel Agencies</strong> when you book a package through our marketplace</li>
          <li><strong>Legal Authorities</strong> when required by law or valid legal process</li>
          <li><strong>Business Transfers</strong> in connection with a merger, acquisition, or asset sale</li>
        </ul>
      </section>

      <section id="p-security">
        <h2>5. Data Security</h2>
        <p>We implement industry-standard security measures to protect your information. No method of transmission or storage is 100% secure, and we cannot guarantee absolute security.</p>
      </section>

      <section id="p-rights">
        <h2>6. Your Rights</h2>
        <p>Depending on your location, you may have the right to:</p>
        <ul>
          <li>Access the personal information we hold about you</li>
          <li>Request correction of inaccurate data</li>
          <li>Request deletion of your personal information</li>
          <li>Object to or restrict processing of your data</li>
          <li>Receive your data in a portable, machine-readable format</li>
          <li>Withdraw consent at any time</li>
        </ul>
        <p>To exercise these rights, contact us at <a href="mailto:privacy&#64;safarni.com">privacy&#64;safarni.com</a>.</p>
      </section>

      <section id="p-cookies">
        <h2>7. Cookies and Tracking</h2>
        <p>We use cookies and similar technologies to maintain sessions, remember preferences, and analyze usage. You can control cookies through your browser settings. See our <a routerLink="/cookies">Cookie Policy</a> for details.</p>
      </section>

      <section id="p-children">
        <h2>8. Children's Privacy</h2>
        <p>The Service is not intended for children under 13. We do not knowingly collect information from children under 13. If we become aware of such collection, we will delete the data promptly.</p>
      </section>

      <section id="p-changes">
        <h2>9. Changes to This Policy</h2>
        <p>We may update this policy from time to time. Material changes will be posted on this page with an updated revision date. Continued use after changes constitutes acceptance.</p>
      </section>

      <section id="p-contact">
        <h2>10. Contact</h2>
        <p>Questions about this policy? Contact us at <a href="mailto:privacy&#64;safarni.com">privacy&#64;safarni.com</a>.</p>
      </section>

    </app-legal-page>
  `
})
export class PrivacyComponent {
  toc = [
    { id: 'p-intro', label: '1. Introduction' },
    { id: 'p-collect', label: '2. Information We Collect' },
    { id: 'p-use', label: '3. How We Use Your Information' },
    { id: 'p-share', label: '4. Information Sharing' },
    { id: 'p-security', label: '5. Data Security' },
    { id: 'p-rights', label: '6. Your Rights' },
    { id: 'p-cookies', label: '7. Cookies and Tracking' },
    { id: 'p-children', label: "8. Children's Privacy" },
    { id: 'p-changes', label: '9. Changes to This Policy' },
    { id: 'p-contact', label: '10. Contact' }
  ];
}
