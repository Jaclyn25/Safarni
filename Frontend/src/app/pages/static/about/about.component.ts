import { Component } from '@angular/core';
import { RouterLink } from '@angular/router';
import { StaticPageLayoutComponent } from '../shared/static-page.layout';

@Component({
  selector: 'app-about',
  standalone: true,
  imports: [RouterLink, StaticPageLayoutComponent],
  template: `
    <app-static-page-layout title="About Safarni" subtitle="Travel planning that keeps your choices at the center.">

      <div class="about">

        <!-- Mission -->
        <section class="about-section">
          <p class="about-lead">
            Safarni started with a frustration: planning a trip should be part of the adventure, not a chore. We're building an AI-powered travel companion that turns scattered ideas into coherent, personalized itineraries.
          </p>
        </section>

        <!-- What we do -->
        <section class="about-section">
          <h2 class="about-heading">What we do</h2>
          <div class="about-two-col">
            <div>
              <p class="about-text">
                Our platform combines intelligent itinerary generation with a curated marketplace of verified travel agencies. You choose the destination, the cities, and how many days to spend in each one. Safarni fills in the rest: day-by-day plans, budget estimates, and local recommendations tailored to your style.
              </p>
            </div>
            <div>
              <p class="about-text">
                When you're ready to book, our marketplace connects you directly with agencies that have been vetted for quality and reliability. You get the convenience of AI planning with the assurance of human expertise.
              </p>
            </div>
          </div>
        </section>

        <!-- Principles -->
        <section class="about-section">
          <h2 class="about-heading">How we work</h2>
          <div class="about-principles">
            <div class="about-principle">
              <span class="about-principle__num">01</span>
              <div>
                <h3 class="about-principle__title">Your choices first</h3>
                <p class="about-text">Every itinerary starts with your preferences, your budget, and your travel style. We never add destinations you didn't ask for.</p>
              </div>
            </div>
            <div class="about-principle">
              <span class="about-principle__num">02</span>
              <div>
                <h3 class="about-principle__title">AI that understands travel</h3>
                <p class="about-text">Our concierge draws on local insights, seasonal patterns, and real traveler preferences. Not generic advice you could find on any blog.</p>
              </div>
            </div>
            <div class="about-principle">
              <span class="about-principle__num">03</span>
              <div>
                <h3 class="about-principle__title">Verified partners</h3>
                <p class="about-text">Every agency on our marketplace goes through a verification process. We ensure quality so you can book with confidence.</p>
              </div>
            </div>
            <div class="about-principle">
              <span class="about-principle__num">04</span>
              <div>
                <h3 class="about-principle__title">Honest scope</h3>
                <p class="about-text">We cover 190+ destinations, but we won't pretend to know everything. When we're uncertain, we say so and help you find the right source.</p>
              </div>
            </div>
          </div>
        </section>

        <!-- Stats -->
        <section class="about-section">
          <div class="about-stats">
            <div class="about-stat">
              <span class="about-stat__value">190+</span>
              <span class="about-stat__label">Destinations covered</span>
            </div>
            <div class="about-stat">
              <span class="about-stat__value">AI</span>
              <span class="about-stat__label">Powered concierge</span>
            </div>
            <div class="about-stat">
              <span class="about-stat__value">24/7</span>
              <span class="about-stat__label">Trip planning access</span>
            </div>
          </div>
        </section>

        <!-- CTA -->
        <section class="about-cta">
          <h2 class="about-cta__title">Start planning</h2>
          <p class="about-cta__text">Try the concierge and see how quickly your next trip takes shape.</p>
          <div class="about-cta__actions">
            <a routerLink="/concierge" class="about-btn about-btn--primary">
              <span class="material-symbols-outlined" style="font-size:18px">auto_awesome</span>
              Open Concierge
            </a>
            <a routerLink="/" class="about-btn about-btn--ghost">
              Browse destinations
            </a>
          </div>
        </section>

      </div>
    </app-static-page-layout>
  `,
  styles: [`
    .about {}

    .about-section { margin-bottom: 3rem; }
    .about-section:last-of-type { margin-bottom: 0; }

    .about-heading {
      font: 600 1.125rem/1.33 Sora, sans-serif;
      color: var(--on-surface);
      margin: 0 0 1rem;
    }

    .about-lead {
      font: 400 1.125rem/1.7 Manrope, sans-serif;
      color: var(--on-surface-variant);
      margin: 0;
      max-width: 62ch;
    }

    .about-text {
      font: 400 0.9375rem/1.65 Manrope, sans-serif;
      color: var(--on-surface-variant);
      margin: 0;
    }

    .about-two-col {
      display: grid;
      grid-template-columns: 1fr 1fr;
      gap: 2rem;
    }

    /* Numbered principles */
    .about-principles {
      display: flex;
      flex-direction: column;
      gap: 0;
    }

    .about-principle {
      display: grid;
      grid-template-columns: 2.5rem 1fr;
      gap: 1rem;
      padding: 1.25rem 0;
      border-bottom: 1px solid var(--outline-variant);
    }
    .about-principle:last-child { border-bottom: none; }

    .about-principle__num {
      font: 600 0.8125rem/1.75 Manrope, sans-serif;
      color: var(--primary);
      font-variant-numeric: tabular-nums;
    }

    .about-principle__title {
      font: 600 0.9375rem/1.4 Manrope, sans-serif;
      color: var(--on-surface);
      margin: 0 0 0.375rem;
    }

    /* Stats */
    .about-stats {
      display: grid;
      grid-template-columns: repeat(3, 1fr);
      gap: 1px;
      background: var(--outline-variant);
      border-radius: 12px;
      overflow: hidden;
    }

    .about-stat {
      display: flex;
      flex-direction: column;
      align-items: center;
      gap: 0.25rem;
      padding: 1.75rem 1rem;
      background: var(--surface-container-lowest);
    }

    .about-stat__value {
      font: 700 1.5rem/1 Sora, sans-serif;
      color: var(--on-surface);
      letter-spacing: -0.02em;
    }

    .about-stat__label {
      font: 500 0.75rem/1 Manrope, sans-serif;
      color: var(--on-surface-variant);
      text-transform: uppercase;
      letter-spacing: 0.06em;
    }

    /* CTA */
    .about-cta {
      margin-top: 3rem;
      padding: 2rem;
      border-radius: 16px;
      background: var(--surface-container-lowest);
      border: 1px solid var(--outline-variant);
    }

    .about-cta__title {
      font: 600 1.25rem/1.33 Sora, sans-serif;
      color: var(--on-surface);
      margin: 0 0 0.25rem;
    }

    .about-cta__text {
      font: 400 0.9375rem/1.5 Manrope, sans-serif;
      color: var(--on-surface-variant);
      margin: 0 0 1.25rem;
    }

    .about-cta__actions {
      display: flex;
      gap: 0.625rem;
      flex-wrap: wrap;
    }

    .about-btn {
      display: inline-flex;
      align-items: center;
      gap: 0.375rem;
      padding: 0.625rem 1.25rem;
      border-radius: 10px;
      font: 600 0.8125rem/1 Manrope, sans-serif;
      text-decoration: none;
      transition: background 0.15s, color 0.15s;
    }

    .about-btn--primary {
      background: var(--primary);
      color: var(--on-primary);
    }
    .about-btn--primary:hover {
      background: color-mix(in srgb, var(--primary) 88%, black);
    }

    .about-btn--ghost {
      background: transparent;
      color: var(--on-surface-variant);
      border: 1px solid var(--outline-variant);
    }
    .about-btn--ghost:hover {
      background: var(--surface-container-high);
      color: var(--on-surface);
    }

    @media (max-width: 640px) {
      .about-two-col { grid-template-columns: 1fr; gap: 1rem; }
      .about-stats { grid-template-columns: 1fr; }
    }
  `]
})
export class AboutComponent {}
