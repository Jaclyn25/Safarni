import { Component, Input } from '@angular/core';
import { CommonModule } from '@angular/common';
import { RouterLink } from '@angular/router';

@Component({
  selector: 'app-static-page-layout',
  standalone: true,
  imports: [CommonModule, RouterLink],
  template: `
    <main class="static-page">
      <section class="static-hero">
        <div class="static-hero__inner">
          <a routerLink="/" class="static-hero__back">
            <span class="material-symbols-outlined" style="font-size:16px">arrow_back</span>
            Home
          </a>
          <h1 class="static-hero__title">{{ title }}</h1>
          @if (subtitle) {
            <p class="static-hero__subtitle">{{ subtitle }}</p>
          }
        </div>
      </section>

      <section class="static-body">
        <div class="static-body__inner">
          <ng-content></ng-content>
        </div>
      </section>

      <footer class="static-footer">
        <div class="static-footer__inner">
          <p class="static-footer__text">
            <a routerLink="/" class="static-footer__link">Safarni</a> &middot; Thoughtful travel planning that keeps your choices at the center of every journey.
          </p>
        </div>
      </footer>
    </main>
  `,
  styles: [`
    .static-page {
      min-height: 100dvh;
      background: var(--surface);
      color: var(--on-surface);
    }

    .static-hero {
      background: var(--surface-container-lowest);
      border-bottom: 1px solid var(--outline-variant);
      padding: clamp(4.5rem, 10vh, 7rem) 0 clamp(2rem, 4vh, 3rem);
    }

    .static-hero__inner {
      max-width: 720px;
      margin: 0 auto;
      padding: 0 clamp(1rem, 5vw, 2.5rem);
    }

    .static-hero__back {
      display: inline-flex;
      align-items: center;
      gap: 0.25rem;
      font: 500 0.8125rem/1 Manrope, sans-serif;
      color: var(--on-surface-variant);
      text-decoration: none;
      margin-bottom: 1.75rem;
      transition: color 0.15s;
    }
    .static-hero__back:hover { color: var(--primary); }

    .static-hero__title {
      font: 700 clamp(1.75rem, 4vw, 2.75rem)/1.12 Sora, sans-serif;
      letter-spacing: -0.025em;
      color: var(--on-surface);
      margin: 0 0 0.625rem;
    }

    .static-hero__subtitle {
      font: 400 clamp(0.9375rem, 1.8vw, 1.125rem)/1.6 Manrope, sans-serif;
      color: var(--on-surface-variant);
      margin: 0;
      max-width: 52ch;
    }

    .static-body {
      padding: clamp(2.5rem, 5vh, 4rem) 0 clamp(3rem, 6vh, 5rem);
    }

    .static-body__inner {
      max-width: 720px;
      margin: 0 auto;
      padding: 0 clamp(1rem, 5vw, 2.5rem);
    }

    .static-footer {
      border-top: 1px solid var(--outline-variant);
      padding: clamp(1.5rem, 3vh, 2rem) 0;
    }

    .static-footer__inner {
      max-width: 720px;
      margin: 0 auto;
      padding: 0 clamp(1rem, 5vw, 2.5rem);
    }

    .static-footer__text {
      font: 400 0.8125rem/1.5 Manrope, sans-serif;
      color: var(--on-surface-variant);
      margin: 0;
    }

    .static-footer__link {
      color: var(--on-surface);
      text-decoration: none;
      font-weight: 600;
    }
    .static-footer__link:hover { color: var(--primary); }
  `]
})
export class StaticPageLayoutComponent {
  @Input() title = '';
  @Input() subtitle = '';
}
