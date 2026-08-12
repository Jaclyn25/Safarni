import { Component } from '@angular/core';
import { RouterLink } from '@angular/router';
import { EditorialPageLayoutComponent } from '../shared/editorial-page.layout';

@Component({
  selector: 'app-about',
  standalone: true,
  imports: [RouterLink, EditorialPageLayoutComponent],
  template: `
    <app-editorial-page
      title="About Safarni"
      intro="Travel planning that keeps your choices at the center."
      kicker="Our story"
      heroImage="assets/public/about-hero.jpg"
      heroImageAlt="A winding road through dramatic mountains, symbolizing the start of a journey">

      <div hero-actions class="pub-hero__actions">
        <a routerLink="/concierge" class="pub-btn pub-btn--on-hero">
          <span class="material-symbols-outlined" style="font-size:18px">auto_awesome</span>
          Open Concierge
        </a>
        <a routerLink="/" class="pub-btn pub-btn--on-hero-ghost">
          Browse destinations
        </a>
      </div>

      <div class="about">

        <!-- Mission -->
        <section class="pub-section">
          <p class="pub-lead">
            Safarni started with a frustration: planning a trip should be part of the adventure, not a chore. We're building an AI-powered travel companion that turns scattered ideas into coherent, personalized itineraries.
          </p>
        </section>

        <!-- What we do -->
        <section class="pub-section">
          <h2 class="pub-heading">What we do</h2>
          <div class="pub-two-col">
            <p class="pub-text">
              Our platform combines intelligent itinerary generation with a curated marketplace of verified travel agencies. You choose the destination, the cities, and how many days to spend in each one. Safarni fills in the rest: day-by-day plans, budget estimates, and local recommendations tailored to your style.
            </p>
            <p class="pub-text">
              When you're ready to book, our marketplace connects you directly with agencies that have been vetted for quality and reliability. You get the convenience of AI planning with the assurance of human expertise.
            </p>
          </div>
        </section>

        <!-- Principles -->
        <section class="pub-section">
          <h2 class="pub-heading">How we work</h2>
          <div class="about-principles">
            <div class="about-principle">
              <span class="about-principle__num">01</span>
              <div>
                <h3 class="about-principle__title">Your choices first</h3>
                <p class="pub-text">Every itinerary starts with your preferences, your budget, and your travel style. We never add destinations you didn't ask for.</p>
              </div>
            </div>
            <div class="about-principle">
              <span class="about-principle__num">02</span>
              <div>
                <h3 class="about-principle__title">AI that understands travel</h3>
                <p class="pub-text">Our concierge draws on local insights, seasonal patterns, and real traveler preferences. Not generic advice you could find on any blog.</p>
              </div>
            </div>
            <div class="about-principle">
              <span class="about-principle__num">03</span>
              <div>
                <h3 class="about-principle__title">Verified partners</h3>
                <p class="pub-text">Every agency on our marketplace goes through a verification process. We ensure quality so you can book with confidence.</p>
              </div>
            </div>
            <div class="about-principle">
              <span class="about-principle__num">04</span>
              <div>
                <h3 class="about-principle__title">Honest scope</h3>
                <p class="pub-text">We cover 190+ destinations, but we won't pretend to know everything. When we're uncertain, we say so and help you find the right source.</p>
              </div>
            </div>
          </div>
        </section>

        <!-- Stats -->
        <section class="pub-section">
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
        <section class="pub-card about-cta">
          <h2 class="about-cta__title">Start planning</h2>
          <p class="pub-text">Try the concierge and see how quickly your next trip takes shape.</p>
          <div class="about-cta__actions">
            <a routerLink="/concierge" class="pub-btn pub-btn--primary">
              <span class="material-symbols-outlined" style="font-size:18px">auto_awesome</span>
              Open Concierge
            </a>
            <a routerLink="/" class="pub-btn pub-btn--ghost">
              Browse destinations
            </a>
          </div>
        </section>

      </div>
    </app-editorial-page>
  `,
  styles: [`
    /* Numbered principles */
    .about-principles {
      display: flex;
      flex-direction: column;
    }

    .about-principle {
      display: grid;
      grid-template-columns: 2.5rem 1fr;
      gap: 1rem;
      padding: 1.25rem 0;
      border-block-end: 1px solid var(--outline-variant);
    }
    .about-principle:last-child { border-block-end: none; }

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
      padding: 2rem;
    }

    .about-cta__title {
      font: 700 1.375rem/1.3 Sora, sans-serif;
      letter-spacing: -0.015em;
      color: var(--on-surface);
      margin: 0 0 0.375rem;
    }

    .about-cta__actions {
      display: flex;
      gap: 0.75rem;
      flex-wrap: wrap;
      margin-block-start: 1.25rem;
    }

    @media (max-width: 640px) {
      .about-stats { grid-template-columns: 1fr; }
    }
  `]
})
export class AboutComponent {}
