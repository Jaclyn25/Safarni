import { Component, OnInit } from '@angular/core';
import { CommonModule } from '@angular/common';
import { readStoredCurrency } from '../profile-utils';

const CURRENCY_STORAGE_KEY = 'safarni_currency';

interface CurrencyOption {
  code: string;
  symbol: string;
  name: string;
}

@Component({
  selector: 'app-profile-currency',
  standalone: true,
  imports: [CommonModule],
  templateUrl: './profile-currency.component.html',
  styleUrls: ['../profile-shared.scss'],
})
export class ProfileCurrencyComponent implements OnInit {
  readonly currencyOptions: CurrencyOption[] = [
    { code: 'USD', symbol: '$', name: 'US Dollar' },
    { code: 'EUR', symbol: '€', name: 'Euro' },
    { code: 'EGP', symbol: 'E£', name: 'Egyptian Pound' },
    { code: 'GBP', symbol: '£', name: 'British Pound' },
    { code: 'AED', symbol: 'د.إ', name: 'UAE Dirham' },
    { code: 'SAR', symbol: '﷼', name: 'Saudi Riyal' },
    { code: 'TRY', symbol: '₺', name: 'Turkish Lira' },
    { code: 'JPY', symbol: '¥', name: 'Japanese Yen' },
  ];

  currencyCode = 'USD';

  ngOnInit(): void {
    this.currencyCode = readStoredCurrency();
  }

  selectCurrency(code: string): void {
    this.currencyCode = code;
    localStorage.setItem(CURRENCY_STORAGE_KEY, code);
  }
}
