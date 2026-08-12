import { Component } from '@angular/core';
import { RouterLink } from '@angular/router';

@Component({
  selector: 'app-public-footer',
  standalone: true,
  imports: [RouterLink],
  template: `
    <footer class="pub-footer">
      <div class="pub-footer__inner">
        <div class="pub-footer__grid">

          <div class="pub-footer__brand">
            <a routerLink="/" class="pub-footer__logo" aria-label="Safarni home">
              <img src="assets/logo.svg" alt="Safarni" class="pub-footer__logo-img" />
            </a>
            <p class="pub-footer__tagline">
              Thoughtful travel planning that keeps your choices at the center of every journey.
            </p>
          </div>

          <nav class="pub-footer__col" aria-label="Company">
            <h2 class="pub-footer__heading">Company</h2>
            <a routerLink="/about" class="pub-footer__link">About Us</a>
            <a routerLink="/contact" class="pub-footer__link">Contact</a>
          </nav>

          <nav class="pub-footer__col" aria-label="Resources">
            <h2 class="pub-footer__heading">Resources</h2>
            <a routerLink="/" class="pub-footer__link">Explore</a>
            <a routerLink="/journal" class="pub-footer__link">Journal</a>
            <a routerLink="/concierge" class="pub-footer__link">Concierge</a>
            <a routerLink="/marketplace" class="pub-footer__link">Marketplace</a>
            <a routerLink="/pricing" class="pub-footer__link">Pricing</a>
          </nav>

          <nav class="pub-footer__col" aria-label="Legal">
            <h2 class="pub-footer__heading">Legal</h2>
            <a routerLink="/privacy" class="pub-footer__link">Privacy Policy</a>
            <a routerLink="/terms" class="pub-footer__link">Terms of Service</a>
            <a routerLink="/cookies" class="pub-footer__link">Cookie Policy</a>
          </nav>

          <div class="pub-footer__col">
            <h2 class="pub-footer__heading">Get in touch</h2>
            <a href="mailto:hello&#64;safarni.com" class="pub-footer__link">hello&#64;safarni.com</a>
            <a href="mailto:support&#64;safarni.com" class="pub-footer__link">support&#64;safarni.com</a>
            <a href="mailto:partners&#64;safarni.com" class="pub-footer__link">partners&#64;safarni.com</a>
            <p class="pub-footer__note">We reply within one business day.</p>
          </div>

        </div>

        <div class="pub-footer__bottom">
          <p class="pub-footer__copyright">
            &copy; {{ year }} Safarni. All rights reserved.
          </p>
          <p class="pub-footer__lang">
            <span class="material-symbols-outlined" style="font-size:16px" aria-hidden="true">language</span>
            English
          </p>
        </div>
      </div>
    </footer>
  `,
  styles: [`
    :host { display: block; }

    .pub-footer {
      border-top: 1px solid var(--outline-variant);
      background: var(--surface-container-lowest);
      padding: clamp(2.5rem, 6vh, 4rem) 0 1.5rem;
    }

    .pub-footer__inner {
      max-width: 76rem;
      margin-inline: auto;
      padding-inline: clamp(1rem, 5vw, 2.5rem);
    }

    .pub-footer__grid {
      display: grid;
      grid-template-columns: minmax(14rem, 2fr) repeat(4, minmax(9rem, 1fr));
      gap: 2.5rem;
      padding-block-end: 2.5rem;
    }

    .pub-footer__logo-img { height: 2rem; width: auto; }

    .pub-footer__tagline {
      font: 400 0.8125rem/1.6 Manrope, sans-serif;
      color: var(--on-surface-variant);
      margin: 1rem 0 0;
      max-width: 28ch;
    }

    .pub-footer__heading {
      font: 700 0.6875rem/1 Manrope, sans-serif;
      letter-spacing: 0.14em;
      text-transform: uppercase;
      color: var(--on-surface);
      margin: 0 0 0.75rem;
    }

    .pub-footer__col {
      display: flex;
      flex-direction: column;
      align-items: flex-start;
      gap: 0.125rem;
    }

    .pub-footer__link {
      display: inline-flex;
      align-items: center;
      min-height: 44px;
      font: 500 0.875rem/1.2 Manrope, sans-serif;
      color: var(--on-surface-variant);
      text-decoration: none;
      transition: color 0.15s;
    }
    .pub-footer__link:hover {
      color: var(--primary);
      text-decoration: underline;
      text-underline-offset: 4px;
    }

    .pub-footer__note {
      font: 400 0.75rem/1.5 Manrope, sans-serif;
      color: var(--on-surface-variant);
      margin: 0.625rem 0 0;
    }

    .pub-footer__bottom {
      display: flex;
      align-items: center;
      justify-content: space-between;
      gap: 1rem;
      flex-wrap: wrap;
      padding-block-start: 1.5rem;
      border-top: 1px solid var(--outline-variant);
    }

    .pub-footer__copyright {
      font: 400 0.75rem/1.5 Manrope, sans-serif;
      color: var(--on-surface-variant);
      margin: 0;
    }

    .pub-footer__lang {
      display: inline-flex;
      align-items: center;
      gap: 0.375rem;
      font: 600 0.75rem/1 Manrope, sans-serif;
      color: var(--on-surface-variant);
      margin: 0;
    }

    @media (max-width: 1023px) {
      .pub-footer__grid { grid-template-columns: repeat(2, 1fr); }
    }

    @media (max-width: 640px) {
      .pub-footer__grid { grid-template-columns: 1fr; gap: 1.75rem; }
      .pub-footer__bottom { flex-direction: column; align-items: flex-start; }
    }
  `]
})
export class PublicFooterComponent {
  year = new Date().getFullYear();
}
