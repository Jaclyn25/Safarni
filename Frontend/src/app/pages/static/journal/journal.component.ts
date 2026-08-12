import { Component } from '@angular/core';
import { StaticPageLayoutComponent } from '../shared/static-page.layout';

@Component({
  selector: 'app-journal',
  standalone: true,
  imports: [StaticPageLayoutComponent],
  template: `
    <app-static-page-layout title="Journal" subtitle="Stories, guides, and travel thinking from the Safarni team.">

      <div class="journal">

        <!-- Featured -->
        <article class="journal-featured">
          <div class="journal-featured__img">
            <img src="https://images.unsplash.com/photo-1507525428034-b723cf961d3e?w=1200&q=80&auto=format" alt="Coastal sunset" loading="lazy" />
          </div>
          <div class="journal-featured__body">
            <span class="journal-chip">Travel Tips</span>
            <h2 class="journal-featured__title">10 ways to stretch your travel budget without sacrificing experiences</h2>
            <p class="journal-featured__excerpt">From timing your bookings to finding the spots locals actually use, these strategies change how you plan trips.</p>
            <div class="journal-meta">
              <time>Aug 8, 2026</time>
              <span class="journal-meta__sep">&middot;</span>
              <span>5 min read</span>
            </div>
          </div>
        </article>

        <!-- Grid -->
        <section class="journal-grid">
          @for (post of posts; track post.title) {
            <article class="journal-card">
              <div class="journal-card__img">
                <img [src]="post.image" [alt]="post.title" loading="lazy" />
              </div>
              <div class="journal-card__body">
                <span class="journal-chip">{{ post.category }}</span>
                <h3 class="journal-card__title">{{ post.title }}</h3>
                <p class="journal-card__excerpt">{{ post.excerpt }}</p>
                <div class="journal-meta">
                  <time>{{ post.date }}</time>
                  <span class="journal-meta__sep">&middot;</span>
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
          <div class="journal-newsletter__form">
            <input type="email" placeholder="your@email.com" class="journal-input" />
            <button class="journal-subscribe">Subscribe</button>
          </div>
        </section>

      </div>
    </app-static-page-layout>
  `,
  styles: [`
    .journal {}

    .journal-chip {
      display: inline-block;
      padding: 0.1875rem 0.5rem;
      border-radius: 6px;
      background: var(--primary-container);
      color: var(--on-primary-container);
      font: 600 0.6875rem/1 Manrope, sans-serif;
      letter-spacing: 0.01em;
    }

    .journal-meta {
      display: flex;
      align-items: center;
      gap: 0.375rem;
      font: 500 0.75rem/1 Manrope, sans-serif;
      color: var(--on-surface-variant);
    }

    .journal-meta__sep { opacity: 0.5; }

    /* Featured */
    .journal-featured {
      display: grid;
      grid-template-columns: 1fr 1fr;
      gap: 0;
      border-radius: 14px;
      overflow: hidden;
      border: 1px solid var(--outline-variant);
      background: var(--surface-container-lowest);
      margin-bottom: 2.5rem;
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
      gap: 0.625rem;
    }

    .journal-featured__title {
      font: 600 1.25rem/1.35 Sora, sans-serif;
      color: var(--on-surface);
      margin: 0;
    }

    .journal-featured__excerpt {
      font: 400 0.875rem/1.6 Manrope, sans-serif;
      color: var(--on-surface-variant);
      margin: 0;
    }

    /* Card grid */
    .journal-grid {
      display: grid;
      grid-template-columns: repeat(2, 1fr);
      gap: 1rem;
      margin-bottom: 2.5rem;
    }

    .journal-card {
      border-radius: 12px;
      overflow: hidden;
      border: 1px solid var(--outline-variant);
      background: var(--surface-container-lowest);
      transition: box-shadow 0.15s;
    }
    .journal-card:hover {
      box-shadow: 0 6px 20px rgba(15, 23, 42, 0.05);
    }

    .journal-card__img {
      height: 160px;
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
      gap: 0.5rem;
      padding: 1.25rem;
    }

    .journal-card__title {
      font: 600 1rem/1.35 Sora, sans-serif;
      color: var(--on-surface);
      margin: 0;
    }

    .journal-card__excerpt {
      font: 400 0.8125rem/1.55 Manrope, sans-serif;
      color: var(--on-surface-variant);
      margin: 0;
      display: -webkit-box;
      -webkit-line-clamp: 2;
      -webkit-box-orient: vertical;
      overflow: hidden;
    }

    /* Newsletter */
    .journal-newsletter {
      display: flex;
      align-items: center;
      justify-content: space-between;
      gap: 2rem;
      padding: 1.75rem 2rem;
      border-radius: 14px;
      background: var(--surface-container-lowest);
      border: 1px solid var(--outline-variant);
    }

    .journal-newsletter__title {
      font: 600 1rem/1.33 Sora, sans-serif;
      color: var(--on-surface);
      margin: 0 0 0.125rem;
    }

    .journal-newsletter__text {
      font: 400 0.8125rem/1.5 Manrope, sans-serif;
      color: var(--on-surface-variant);
      margin: 0;
    }

    .journal-newsletter__form {
      display: flex;
      gap: 0.5rem;
      flex-shrink: 0;
    }

    .journal-input {
      width: 220px;
      padding: 0.5rem 0.875rem;
      border-radius: 8px;
      border: 1px solid var(--outline-variant);
      background: var(--surface);
      color: var(--on-surface);
      font: 400 0.8125rem/1 Manrope, sans-serif;
      outline: none;
      transition: border-color 0.15s;
    }
    .journal-input:focus { border-color: var(--primary); }
    .journal-input::placeholder { color: var(--on-surface-variant); }

    .journal-subscribe {
      padding: 0.5rem 1rem;
      border-radius: 8px;
      border: none;
      background: var(--primary);
      color: var(--on-primary);
      font: 600 0.8125rem/1 Manrope, sans-serif;
      cursor: pointer;
      transition: background 0.15s;
    }
    .journal-subscribe:hover { background: color-mix(in srgb, var(--primary) 88%, black); }

    @media (max-width: 768px) {
      .journal-featured { grid-template-columns: 1fr; }
      .journal-featured__img { min-height: 200px; }
    }

    @media (max-width: 640px) {
      .journal-grid { grid-template-columns: 1fr; }
      .journal-newsletter { flex-direction: column; align-items: flex-start; gap: 1rem; }
      .journal-newsletter__form { width: 100%; }
      .journal-input { flex: 1; width: auto; }
    }
  `]
})
export class JournalComponent {
  posts = [
    {
      title: 'Why AI trip planning changes the game',
      excerpt: 'Traditional planning is fragmented and slow. AI tools are making it possible to go from idea to complete itinerary in minutes.',
      category: 'Technology',
      date: 'Aug 5, 2026',
      readTime: '4 min',
      image: 'https://images.unsplash.com/photo-1488646953014-85cb44e25828?w=600&q=80&auto=format'
    },
    {
      title: 'Hidden gems of Southeast Asia',
      excerpt: 'Beyond Bangkok and Bali: the towns, temples, and coastlines that most travelers miss on their first trip.',
      category: 'Destinations',
      date: 'Aug 1, 2026',
      readTime: '6 min',
      image: 'https://images.unsplash.com/photo-1552733407-5d5c46c3bb3b?w=600&q=80&auto=format'
    },
    {
      title: 'Traveling responsibly in 2026',
      excerpt: 'Practical steps for reducing your footprint without turning every trip into a logistics project.',
      category: 'Sustainability',
      date: 'Jul 28, 2026',
      readTime: '5 min',
      image: 'https://images.unsplash.com/photo-1501785888041-af3ef285b470?w=600&q=80&auto=format'
    },
    {
      title: 'What to know about travel insurance',
      excerpt: 'Medical coverage, trip cancellation, and the fine print that actually matters when something goes wrong.',
      category: 'Travel Tips',
      date: 'Jul 22, 2026',
      readTime: '7 min',
      image: 'https://images.unsplash.com/photo-1436491865332-7a61a109db05?w=600&q=80&auto=format'
    }
  ];
}
