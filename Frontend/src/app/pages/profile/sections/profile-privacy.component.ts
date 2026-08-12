import { Component } from '@angular/core';
import { CommonModule } from '@angular/common';

interface PrivacySection {
  icon: string;
  title: string;
  items: string[];
}

interface PrivacyIntro {
  icon: string;
  title: string;
  text: string;
}

@Component({
  selector: 'app-profile-privacy',
  standalone: true,
  imports: [CommonModule],
  templateUrl: './profile-privacy.component.html',
  styleUrls: ['../profile-shared.scss'],
})
export class ProfilePrivacyComponent {
  readonly intro: PrivacyIntro[] = [
    {
      icon: 'database',
      title: 'Data we collect',
      text: 'Account details (name, email, phone), trip plans and itineraries, chat conversations with the AI assistant, agency listings and booking requests, and technical data such as device type and usage patterns.'
    },
    {
      icon: 'cookie',
      title: 'Cookies & local storage',
      text: 'We use local storage to remember your session, theme, language, and currency preferences. We do not sell your personal data. Analytical data is used only to improve the product.'
    },
    {
      icon: 'smart_toy',
      title: 'AI & third-party services',
      text: 'Trip planning, itinerary generation, and the concierge assistant are powered by AI providers (e.g., Gemini/Groq). Your prompts and trip data may be sent to these providers solely to generate responses. Payments are processed by Stripe; maps by Google.'
    },
    {
      icon: 'verified_user',
      title: 'Your rights',
      text: 'You can view and update your profile, download or delete your data, and opt out of marketing emails at any time. Unused accounts may be archived after a period of inactivity.'
    },
    {
      icon: 'contact_support',
      title: 'Contact',
      text: 'For privacy questions, data requests, or account closure, contact our support team at privacy@safarni.example — we respond within 30 days.'
    }
  ];

  readonly practices: PrivacySection[] = [
    {
      icon: 'travel_explore',
      title: 'Travel data',
      items: [
        'Your trips, itineraries, and saved locations are private by default and only visible to you.',
        'Agency listings and reviews you submit are public on the marketplace.',
        'You can control profile visibility and contact details from your notifications & privacy settings.'
      ]
    },
    {
      icon: 'mark_email_read',
      title: 'Communications',
      items: [
        'Transactional emails (booking updates, verification, password recovery) are always sent.',
        'Marketing emails are optional and can be turned off in your notification settings.',
        'Push notifications are only enabled if you grant browser permission.'
      ]
    },
    {
      icon: 'admin_panel_settings',
      title: 'Data retention & deletion',
      items: [
        'We keep trip and chat data while your account is active so you can pick up where you left off.',
        'To close your account, submit a closure request in the Security section — our team processes it and confirms once your data is removed.',
        'Backups may retain data for up to 30 days after deletion.'
      ]
    }
  ];

  readonly lastUpdated = 'August 12, 2026';
}