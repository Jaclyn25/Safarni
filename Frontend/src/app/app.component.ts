import { Component } from '@angular/core';
import { RouterLink, RouterOutlet } from '@angular/router';
import { NavbarComponent } from './shared/navbar/navbar';

@Component({
  selector: 'app-root',
  standalone: true,
  styleUrls: ['./app.component.css'],
  imports: [
    RouterOutlet,
    RouterLink,
    NavbarComponent
  ],
  template: `
    <a href="#main-content" class="skip-link">Skip to main content</a>
    <app-navbar></app-navbar>

    <router-outlet></router-outlet>

    <a
      routerLink="/concierge"
      class="concierge-fab"
      aria-label="Open Safarni Concierge"
      title="Ask Safarni">
      <span class="material-symbols-outlined concierge-fab__icon" aria-hidden="true">auto_awesome</span>
      <span class="concierge-fab__label" aria-hidden="true">Ask Safarni</span>
    </a>
  `
})
export class AppComponent {}
