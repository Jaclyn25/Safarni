import {
  AfterViewInit,
  Component,
  Inject,
  Input,
  OnDestroy,
  OnInit
} from '@angular/core';
import { DOCUMENT } from '@angular/common';
import { ActivatedRoute, RouterLink, RouterLinkActive } from '@angular/router';
import { PublicFooterComponent } from '../../../shared/public-footer/public-footer.component';
import { applyPublicMeta } from './public-meta';

export interface LegalTocItem {
  id: string;
  label: string;
}

@Component({
  selector: 'app-legal-page',
  standalone: true,
  imports: [RouterLink, RouterLinkActive, PublicFooterComponent],
  template: `
    <div class="legal-page">
      <header class="pub-hero pub-hero--plain">
        <div class="pub-hero__inner pub-hero__inner--narrow">
          <a routerLink="/" class="pub-hero__back">
            <span class="material-symbols-outlined pub-icon-arrow" style="font-size:16px">arrow_back</span>
            Home
          </a>
          <p class="pub-kicker pub-kicker--plain">Legal</p>
          <h1 class="pub-hero__title">{{ title }}</h1>
          @if (intro) {
            <p class="pub-hero__intro">{{ intro }}</p>
          }
          @if (updated) {
            <p class="legal-updated">
              <span class="material-symbols-outlined" style="font-size:14px">update</span>
              Last updated
              <time [attr.datetime]="updatedIso">{{ updated }}</time>
            </p>
          }
        </div>
      </header>

      <main id="main-content" tabindex="-1" class="legal-body">
        <div class="legal-body__inner">

          <aside class="legal-toc" aria-label="On this page">
            <nav class="legal-toc__nav">
              <p class="legal-toc__label">On this page</p>
              <ul style="list-style:none; margin:0; padding:0; display:flex; flex-direction:column; gap:0.125rem;">
                @for (item of toc; track item.id) {
                  <li>
                    <a [href]="'#' + item.id" (click)="onTocClick($event, item.id)"
                      class="legal-toc__link" [class.is-active]="activeId === item.id">
                      {{ item.label }}
                    </a>
                  </li>
                }
              </ul>
            </nav>
            <nav class="legal-toc__cross" aria-label="Related legal documents">
              <a routerLink="/privacy" routerLinkActive="is-active" class="legal-toc__cross-link">Privacy Policy</a>
              <a routerLink="/terms" routerLinkActive="is-active" class="legal-toc__cross-link">Terms of Service</a>
              <a routerLink="/cookies" routerLinkActive="is-active" class="legal-toc__cross-link">Cookie Policy</a>
            </nav>
          </aside>

          <nav class="legal-toc--mobile" aria-label="On this page">
            <p class="legal-toc__label">On this page</p>
            <ul style="list-style:none; margin:0; padding:0; display:flex; flex-direction:column; gap:0.125rem;">
              @for (item of toc; track item.id) {
                <li>
                  <a [href]="'#' + item.id" (click)="onTocClick($event, item.id)"
                    class="legal-toc__link" [class.is-active]="activeId === item.id">
                    {{ item.label }}
                  </a>
                </li>
              }
            </ul>
          </nav>

          <article class="legal-doc">
            <ng-content></ng-content>
          </article>

        </div>
      </main>

      <app-public-footer></app-public-footer>
    </div>
  `
})
export class LegalPageLayoutComponent implements OnInit, AfterViewInit, OnDestroy {
  @Input() title = '';
  @Input() intro = '';
  @Input() updated = '';
  @Input() updatedIso = '';
  @Input() toc: LegalTocItem[] = [];

  activeId = '';
  private observer?: IntersectionObserver;

  constructor(
    @Inject(DOCUMENT) private doc: Document,
    private route: ActivatedRoute
  ) {}

  ngOnInit(): void {
    applyPublicMeta(this.doc, this.route);
  }

  ngAfterViewInit(): void {
    const root = this.doc.getElementById('main-content');
    if (!root || !('IntersectionObserver' in window)) return;

    const sections = this.toc
      .map((item) => this.doc.getElementById(item.id))
      .filter((el): el is HTMLElement => !!el);

    this.observer = new IntersectionObserver(
      (entries) => {
        for (const entry of entries) {
          if (entry.isIntersecting) {
            this.activeId = entry.target.id;
          }
        }
      },
      { root, rootMargin: '0px 0px -70% 0px', threshold: 0 }
    );
    sections.forEach((section) => this.observer?.observe(section));

    if (sections.length > 0) {
      this.activeId = sections[0].id;
    }
  }

  onTocClick(event: Event, id: string): void {
    event.preventDefault();
    const reduceMotion = window.matchMedia('(prefers-reduced-motion: reduce)').matches;
    this.doc.getElementById(id)?.scrollIntoView({
      behavior: reduceMotion ? 'auto' : 'smooth',
      block: 'start'
    });
    this.activeId = id;
  }

  ngOnDestroy(): void {
    this.observer?.disconnect();
  }
}
