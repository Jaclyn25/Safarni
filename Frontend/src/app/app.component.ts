import { Component } from '@angular/core';
import { RouterOutlet } from '@angular/router';
import { NavbarComponent } from './shared/navbar/navbar';
@Component({
  selector: 'app-root',
  standalone: true,
  imports: [
    RouterOutlet,
    NavbarComponent
  ],
  template: `
    <a href="#main-content" class="skip-link">Skip to main content</a>
    <app-navbar></app-navbar>

    <router-outlet></router-outlet>
  `
})
export class AppComponent {}