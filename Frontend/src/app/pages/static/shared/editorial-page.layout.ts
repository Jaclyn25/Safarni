import { Component, Inject, Input, OnInit } from '@angular/core';
import { DOCUMENT } from '@angular/common';
import { ActivatedRoute, RouterLink } from '@angular/router';
import { PublicFooterComponent } from '../../../shared/public-footer/public-footer.component';
import { applyPublicMeta } from './public-meta';

@Component({
  selector: 'app-editorial-page',
  standalone: true,
  imports: [RouterLink, PublicFooterComponent],
  template: `
    <div class="pub-page">
      <header class="pub-hero" [class.pub-hero--media]="heroImage">
        @if (heroImage) {
          <img class="pub-hero__media" [src]="heroImage" [alt]="heroImageAlt" fetchpriority="high" />
          <div class="pub-hero__scrim" aria-hidden="true"></div>
        }
        <div class="pub-hero__inner">
          <a routerLink="/" class="pub-hero__back">
            <span class="material-symbols-outlined pub-icon-arrow" style="font-size:16px">arrow_back</span>
            Home
          </a>
          @if (kicker) {
            <p class="pub-kicker pub-kicker--on-hero">{{ kicker }}</p>
          }
          <h1 class="pub-hero__title">{{ title }}</h1>
          @if (intro) {
            <p class="pub-hero__intro">{{ intro }}</p>
          }
          <ng-content select="[hero-actions]"></ng-content>
        </div>
      </header>

      <main id="main-content" tabindex="-1" class="pub-body">
        <div class="pub-body__inner">
          <ng-content></ng-content>
        </div>
      </main>

      <app-public-footer></app-public-footer>
    </div>
  `
})
export class EditorialPageLayoutComponent implements OnInit {
  @Input() title = '';
  @Input() intro = '';
  @Input() kicker = '';
  @Input() heroImage = '';
  @Input() heroImageAlt = '';

  constructor(
    @Inject(DOCUMENT) private doc: Document,
    private route: ActivatedRoute
  ) {}

  ngOnInit(): void {
    applyPublicMeta(this.doc, this.route);
  }
}
