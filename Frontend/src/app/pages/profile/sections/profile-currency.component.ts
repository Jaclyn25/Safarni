import { Component, OnInit } from '@angular/core';
import { CommonModule } from '@angular/common';
import {
  CURRENCY_OPTIONS,
  CurrencyService,
  readStoredCurrency
} from '../../../services/currency.service';

@Component({
  selector: 'app-profile-currency',
  standalone: true,
  imports: [CommonModule],
  templateUrl: './profile-currency.component.html',
  styleUrls: ['../profile-shared.scss'],
})
export class ProfileCurrencyComponent implements OnInit {
  readonly currencyOptions = CURRENCY_OPTIONS;

  currencyCode = 'USD';

  constructor(private readonly currencyService: CurrencyService) {}

  ngOnInit(): void {
    this.currencyCode = readStoredCurrency();
  }

  selectCurrency(code: typeof CURRENCY_OPTIONS[number]['code']): void {
    this.currencyCode = code;
    this.currencyService.setDisplayCurrency(code);
  }
}