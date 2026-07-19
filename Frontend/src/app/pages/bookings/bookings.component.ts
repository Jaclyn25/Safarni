import { Component } from '@angular/core';
import { RouterLink, RouterLinkActive } from '@angular/router';

@Component({
  selector: 'app-bookings',
  standalone: true,
  imports: [RouterLink, RouterLinkActive],
  templateUrl: './bookings.component.html'
})
export class BookingsComponent {}
