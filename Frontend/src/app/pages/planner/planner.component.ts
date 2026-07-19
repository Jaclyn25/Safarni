import { Component, HostListener } from '@angular/core';
import { RouterLink, RouterLinkActive } from '@angular/router';

@Component({
  selector: 'app-planner',
  standalone: true,
  imports: [RouterLink, RouterLinkActive],
  templateUrl: './planner.component.html'
})
export class PlannerComponent {
  scrolled = false;

  @HostListener('window:scroll')
  onWindowScroll(): void {
    this.scrolled = window.scrollY > 20;
  }
}
