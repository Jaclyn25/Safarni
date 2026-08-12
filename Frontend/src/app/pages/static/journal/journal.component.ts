import { Component, signal } from '@angular/core';
import { FormsModule } from '@angular/forms';
import { EditorialPageLayoutComponent } from '../shared/editorial-page.layout';

@Component({
  selector: 'app-journal',
  standalone: true,
  imports: [FormsModule, EditorialPageLayoutComponent],
  template: `
    <app-editorial-page
      title="Journal"
      intro="Stories, guides, and travel thinking from the Safarni team."
      kicker="Journal">

      <div class="journal">

        <!-- Featured -->
        <article class="journal-featured">
          <div class="journal-featured__img">
            <img src="assets/public/journal-featured.jpg" alt="Coastal sunset with calm water" loading="eager" />
          </div>
          <div class="journal-featured__body">
            <span class="pub-chip">Travel Tips</span>
            <h2 class="journal-featured__title">10 ways to stretch your travel budget without sacrificing experiences</h2>
            <p class="journal-featured__excerpt">From timing your bookings to finding the spots locals actually use, these strategies change how you plan trips.</p>
            <div class="pub-meta">
              <time datetime="2026-08-08">Aug 8, 2026</time>
              <span class="pub-meta__sep">&middot;</span>
              <span>5 min read</span>
            </div>
          </div>
        </article>

        <!-- Grid -->
        <section class="journal-grid" aria-label="More articles">
          @for (post of posts; track post.title) {
            <article class="journal-card">
              <div class="journal-card__img">
                <img [src]="post.image" [alt]="post.imageAlt" loading="lazy" width="800" height="480" />
              </div>
              <div class="journal-card__body">
                <span class="pub-chip">{{ post.category }}</span>
                <h3 class="journal-card__title">{{ post.title }}</h3>
                <p class="journal-card__excerpt">{{ post.excerpt }}</p>
                <div class="pub-meta">
                  <time>{{ post.date }}</time>
                  <span class="pub-meta__sep">&middot;</span>
                  <span>{{ post.readTime }}</span>
                </div>
              </div>
            </article>
          }
        </section>

        <!-- Newsletter -->
        <section class="journal-newsletter">
          <div class="journal-newsletter__content">
            <h2 class="journal-newsletter__title">Stay updated</h2>
            <p class="journal-newsletter__text">Travel stories and guides, delivered weekly.</p>
          </div>
          <form class="journal-newsletter__form" (ngSubmit)="subscribe()" novalidate>
            <label class="sr-only" for="journal-email">Email address</label>
            <input id="journal-email" name="email" type="email" required placeholder="your@email.com"
              class="journal-input" [(ngModel)]="email" (input)="clearNewsletterStatus()" />
            <button type="submit" class="journal-subscribe" [disabled]="newsletterStatus() === 'loading'">
              @if (newsletterStatus() === 'loading') {
                <span class="material-symbols-outlined" style="font-size:16px">progress_activity</span>
                Subscribing
              } @else {
                Subscribe
              }
            </button>
          </form>
          @if (newsletterStatus() === 'success') {
            <p class="journal-newsletter__status journal-newsletter__status--success" role="status">
              <span class="material-symbols-outlined" style="font-size:16px">check_circle</span>
              You're subscribed. Watch your inbox.
            </p>
          }
          @if (newsletterStatus() === 'error') {
            <p class="journal-newsletter__status journal-newsletter__status--error" role="alert">
              <span class="material-symbols-outlined" style="font-size:16px">error</span>
              That email doesn't look right. Please try again.
            </p>
          }
        </section>

      </div>
    </app-editorial-page>
  `,
  styles: [`
    /* Featured */
    .journal-featured {
      display: grid;
      grid-template-columns: 1fr 1fr;
      gap: 0;
      border-radius: 14px;
      overflow: hidden;
      border: 1px solid var(--outline-variant);
      background: var(--surface-container-lowest);
      margin-block-end: 2.5rem;
    }

    .journal-featured__img {
      height: 100%;
      min-height: 260px;
      overflow: hidden;
    }

    .journal-featured__img img {
      width: 100%;
      height: 100%;
      object-fit: cover;
      display: block;
    }

    .journal-featured__body {
      display: flex;
      flex-direction: column;
      justify-content: center;
      padding: 2rem;
      gap: 0.75rem;
    }

    .journal-featured__title {
      font: 700 1.375rem/1.35 Sora, sans-serif;
      letter-spacing: -0.015em;
      color: var(--on-surface);
      margin: 0;
    }

    .journal-featured__excerpt {
      font: 400 0.9375rem/1.6 Manrope, sans-serif;
      color: var(--on-surface-variant);
      margin: 0;
    }

    /* Card grid */
    .journal-grid {
      display: grid;
      grid-template-columns: repeat(2, 1fr);
      gap: 1.25rem;
      margin-block-end: 2.5rem;
    }

    .journal-card {
      border-radius: 12px;
      overflow: hidden;
      border: 1px solid var(--outline-variant);
      background: var(--surface-container-lowest);
      transition: box-shadow 0.15s, transform 0.15s;
    }
    .journal-card:hover {
      box-shadow: 0 6px 20px rgba(15, 23, 42, 0.05);
    }

    .journal-card__img {
      aspect-ratio: 5 / 3;
      overflow: hidden;
    }

    .journal-card__img img {
      width: 100%;
      height: 100%;
      object-fit: cover;
      display: block;
      transition: transform 0.25s;
    }
    .journal-card:hover .journal-card__img img { transform: scale(1.03); }

    .journal-card__body {
      display: flex;
      flex-direction: column;
      gap: 0.625rem;
      padding: 1.25rem;
    }

    .journal-card__title {
      font: 600 1.0625rem/1.35 Sora, sans-serif;
      color: var(--on-surface);
      margin: 0;
    }

    .journal-card__excerpt {
      font: 400 0.875rem/1.55 Manrope, sans-serif;
      color: var(--on-surface-variant);
      margin: 0;
      display: -webkit-box;
      -webkit-line-clamp: 2;
      -webkit-box-orient: vertical;
      overflow: hidden;
    }

    /* Newsletter */
    .journal-newsletter {
      display: grid;
      gap: 1.25rem;
      padding: 2rem;
      border-radius: 14px;
      background: var(--surface-container-lowest);
      border: 1px solid var(--outline-variant);
    }

    .journal-newsletter__title {
      font: 700 1.25rem/1.33 Sora, sans-serif;
      letter-spacing: -0.015em;
      color: var(--on-surface);
      margin: 0 0 0.25rem;
    }

    .journal-newsletter__text {
      font: 400 0.875rem/1.5 Manrope, sans-serif;
      color: var(--on-surface-variant);
      margin: 0;
    }

    .journal-newsletter__form {
      display: flex;
      gap: 0.625rem;
      flex-wrap: wrap;
    }

    .journal-input {
      flex: 1 1 220px;
      min-height: 46px;
      padding: 0.625rem 0.875rem;
      border-radius: 10px;
      border: 1px solid var(--outline-variant);
      background: var(--surface);
      color: var(--on-surface);
      font: 400 0.9375rem/1 Manrope, sans-serif;
      transition: border-color 0.15s, box-shadow 0.15s;
    }
    .journal-input:focus {
      border-color: var(--primary);
      box-shadow: 0 0 0 3px color-mix(in srgb, var(--primary) 18%, transparent);
      outline: none;
    }
    .journal-input::placeholder { color: var(--on-surface-variant); }

    .journal-subscribe {
      display: inline-flex;
      align-items: center;
      justify-content: center;
      gap: 0.375rem;
      min-height: 46px;
      padding: 0.625rem 1.375rem;
      border-radius: 999px;
      border: none;
      background: var(--primary);
      color: var(--on-primary);
      font: 600 0.875rem/1 Manrope, sans-serif;
      cursor: pointer;
      transition: background 0.15s;
    }
    .journal-subscribe:hover { background: color-mix(in srgb, var(--primary) 86%, black); }
    .journal-subscribe:disabled { opacity: 0.6; cursor: not-allowed; }

    .journal-newsletter__status {
      display: flex;
      align-items: center;
      gap: 0.375rem;
      margin: 0;
      font: 500 0.8125rem/1.5 Manrope, sans-serif;
    }
    .journal-newsletter__status--success { color: #15803d; }
    .journal-newsletter__status--error { color: var(--error); }
    .dark .journal-newsletter__status--success { color: #4ade80; }

    @media (max-width: 768px) {
      .journal-featured { grid-template-columns: 1fr; }
      .journal-featured__img { min-height: 200px; }
    }

    @media (max-width: 640px) {
      .journal-grid { grid-template-columns: 1fr; }
    }
  `]
})
export class JournalComponent {
  email = '';
  newsletterStatus = signal<'idle' | 'loading' | 'success' | 'error'>('idle');

  posts = [
    {
      title: 'Why AI trip planning changes the game',
      excerpt: 'Traditional planning is fragmented and slow. AI tools are making it possible to go from idea to complete itinerary in minutes.',
      category: 'Technology',
      date: 'Aug 5, 2026',
      readTime: '4 min',
      image: 'assets/public/journal-ai.jpg',
      imageAlt: 'Airport terminal with travelers and an airplane ready for departure'
    },
    {
      title: 'Hidden gems of Southeast Asia',
      excerpt: 'Beyond Bangkok and Bali: the towns, temples, and coastlines that most travelers miss on their first trip.',
      category: 'Destinations',
      date: 'Aug 1, 2026',
      readTime: '6 min',
      image: 'assets/public/journal-seasia.jpg',
      imageAlt: 'Airplane wing above the ocean at golden hour'
    },
    {
      title: 'Traveling responsibly in 2026',
      excerpt: 'Practical steps for reducing your footprint without turning every trip into a logistics project.',
      category: 'Sustainability',
      date: 'Jul 28, 2026',
      readTime: '5 min',
      image: 'assets/public/journal-sustainable.jpg',
      imageAlt: 'Mountain lake reflecting snow-capped peaks'
    },
    {
      title: 'What to know about travel insurance',
      excerpt: 'Medical coverage, trip cancellation, and the fine print that actually matters when something goes wrong.',
      category: 'Travel Tips',
      date: 'Jul 22, 2026',
      readTime: '7 min',
      image: 'assets/public/journal-insurance.jpg',
      imageAlt: 'View from an airplane window above the clouds'
    }
  ];

  subscribe(): void {
    const valid = /^[^\s@]+@[^\s@]+\.[^\s@]+$/.test(this.email.trim());
    if (!valid || this.newsletterStatus() === 'loading') {
      this.newsletterStatus.set('error');
      return;
    }
    this.newsletterStatus.set('loading');
    setTimeout(() => {
      this.newsletterStatus.set('success');
      this.email = '';
      setTimeout(() => this.newsletterStatus.set('idle'), 5000);
    }, 900);
  }

  clearNewsletterStatus(): void {
    if (this.newsletterStatus() === 'error') {
      this.newsletterStatus.set('idle');
    }
  }
}
