import { Component } from '@angular/core';
import { StaticPageLayoutComponent } from '../shared/static-page.layout';

@Component({
  selector: 'app-careers',
  standalone: true,
  imports: [StaticPageLayoutComponent],
  template: `
    <app-static-page-layout title="Careers" subtitle="Help us make travel planning feel effortless.">

      <div class="careers">

        <section class="careers-section">
          <p class="careers-lead">
            We're building the future of travel planning and looking for people who care about craft, clarity, and real user problems. If you're excited by the intersection of AI and travel, we'd like to talk.
          </p>
        </section>

        <!-- Open roles -->
        <section class="careers-section">
          <h2 class="careers-heading">Open roles</h2>
          <div class="careers-roles">
            @for (role of roles; track role.title) {
              <div class="careers-role">
                <div class="careers-role__top">
                  <h3 class="careers-role__title">{{ role.title }}</h3>
                  <span class="careers-apply">Apply</span>
                </div>
                <div class="careers-role__meta">
                  <span class="careers-badge">{{ role.department }}</span>
                  <span class="careers-detail">
                    <span class="material-symbols-outlined" style="font-size:14px">location_on</span>
                    {{ role.location }}
                  </span>
                  <span class="careers-detail">
                    <span class="material-symbols-outlined" style="font-size:14px">schedule</span>
                    {{ role.type }}
                  </span>
                </div>
                <p class="careers-role__desc">{{ role.description }}</p>
              </div>
            }
          </div>
        </section>

        <!-- Benefits -->
        <section class="careers-section">
          <h2 class="careers-heading">What we offer</h2>
          <div class="careers-perks">
            @for (b of benefits; track b.label) {
              <div class="careers-perk">
                <span class="material-symbols-outlined careers-perk__icon">{{ b.icon }}</span>
                <span class="careers-perk__label">{{ b.label }}</span>
              </div>
            }
          </div>
        </section>

        <!-- CTA -->
        <section class="careers-cta">
          <p class="careers-cta__text">Don't see a fit? Send your resume to <span class="careers-cta__email">careers&#64;safarni.com</span></p>
        </section>

      </div>
    </app-static-page-layout>
  `,
  styles: [`
    .careers {}

    .careers-section { margin-bottom: 3rem; }
    .careers-section:last-of-type { margin-bottom: 0; }

    .careers-heading {
      font: 600 1.125rem/1.33 Sora, sans-serif;
      color: var(--on-surface);
      margin: 0 0 1.25rem;
    }

    .careers-lead {
      font: 400 1.0625rem/1.65 Manrope, sans-serif;
      color: var(--on-surface-variant);
      margin: 0;
      max-width: 60ch;
    }

    /* Roles */
    .careers-roles {
      display: flex;
      flex-direction: column;
    }

    .careers-role {
      padding: 1.5rem 0;
      border-bottom: 1px solid var(--outline-variant);
    }
    .careers-role:first-child { padding-top: 0; }
    .careers-role:last-child { border-bottom: none; }

    .careers-role__top {
      display: flex;
      justify-content: space-between;
      align-items: flex-start;
      gap: 1rem;
      margin-bottom: 0.625rem;
    }

    .careers-role__title {
      font: 600 1rem/1.33 Sora, sans-serif;
      color: var(--on-surface);
      margin: 0;
    }

    .careers-apply {
      flex-shrink: 0;
      padding: 0.375rem 0.875rem;
      border-radius: 8px;
      border: 1px solid var(--primary);
      background: transparent;
      color: var(--primary);
      font: 600 0.75rem/1 Manrope, sans-serif;
      cursor: pointer;
      transition: background 0.15s, color 0.15s;
    }
    .careers-apply:hover {
      background: var(--primary);
      color: var(--on-primary);
    }

    .careers-role__meta {
      display: flex;
      align-items: center;
      gap: 0.75rem;
      margin-bottom: 0.75rem;
      flex-wrap: wrap;
    }

    .careers-badge {
      display: inline-block;
      padding: 0.1875rem 0.5rem;
      border-radius: 6px;
      background: var(--primary-container);
      color: var(--on-primary-container);
      font: 600 0.6875rem/1 Manrope, sans-serif;
    }

    .careers-detail {
      display: inline-flex;
      align-items: center;
      gap: 0.1875rem;
      font: 500 0.75rem/1 Manrope, sans-serif;
      color: var(--on-surface-variant);
    }

    .careers-role__desc {
      font: 400 0.875rem/1.6 Manrope, sans-serif;
      color: var(--on-surface-variant);
      margin: 0;
    }

    /* Benefits */
    .careers-perks {
      display: grid;
      grid-template-columns: repeat(3, 1fr);
      gap: 1px;
      background: var(--outline-variant);
      border-radius: 12px;
      overflow: hidden;
    }

    .careers-perk {
      display: flex;
      align-items: center;
      gap: 0.75rem;
      padding: 1.25rem;
      background: var(--surface-container-lowest);
    }

    .careers-perk__icon {
      font-size: 20px;
      color: var(--primary);
      flex-shrink: 0;
    }

    .careers-perk__label {
      font: 500 0.8125rem/1.35 Manrope, sans-serif;
      color: var(--on-surface);
    }

    /* CTA */
    .careers-cta {
      margin-top: 3rem;
      padding: 1.5rem;
      border-radius: 12px;
      background: var(--surface-container-lowest);
      border: 1px solid var(--outline-variant);
    }

    .careers-cta__text {
      font: 400 0.875rem/1.5 Manrope, sans-serif;
      color: var(--on-surface-variant);
      margin: 0;
    }

    .careers-cta__email {
      font-weight: 600;
      color: var(--primary);
    }

    @media (max-width: 640px) {
      .careers-perks { grid-template-columns: 1fr; }
    }
  `]
})
export class CareersComponent {
  roles = [
    {
      title: 'Senior Full-Stack Engineer',
      department: 'Engineering',
      location: 'Remote',
      type: 'Full-time',
      description: 'Build and scale our AI-powered travel platform. Work with Angular, .NET, and modern cloud infrastructure to deliver features used by thousands of travelers.'
    },
    {
      title: 'AI/ML Engineer',
      department: 'Engineering',
      location: 'Remote',
      type: 'Full-time',
      description: 'Develop and improve our AI concierge system. Train models that understand travel preferences and generate personalized itineraries across 190+ destinations.'
    },
    {
      title: 'Product Designer',
      department: 'Design',
      location: 'Remote',
      type: 'Full-time',
      description: 'Shape the user experience of travel planning. Create intuitive interfaces that make complex itineraries feel simple and enjoyable to build.'
    },
    {
      title: 'Growth Marketing Manager',
      department: 'Marketing',
      location: 'Remote',
      type: 'Full-time',
      description: 'Drive user acquisition and engagement. Build campaigns that connect with travelers and showcase the power of AI-assisted trip planning.'
    }
  ];

  benefits = [
    { icon: 'flight_takeoff', label: 'Travel allowance' },
    { icon: 'home', label: 'Remote-first culture' },
    { icon: 'health_and_safety', label: 'Health coverage' },
    { icon: 'school', label: 'Learning budget' },
    { icon: 'groups', label: 'Team retreats' },
    { icon: 'token', label: 'Equity options' }
  ];
}
