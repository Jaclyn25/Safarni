import { Injectable, signal } from '@angular/core';

export const CURRENCY_STORAGE_KEY = 'safarni_currency';

export type SupportedCurrency = 'USD' | 'EUR' | 'EGP' | 'GBP' | 'AED' | 'SAR' | 'TRY' | 'JPY';

export const CURRENCY_OPTIONS: { code: SupportedCurrency; symbol: string; name: string }[] = [
  { code: 'USD', symbol: '$', name: 'US Dollar' },
  { code: 'EUR', symbol: '€', name: 'Euro' },
  { code: 'EGP', symbol: 'E£', name: 'Egyptian Pound' },
  { code: 'GBP', symbol: '£', name: 'British Pound' },
  { code: 'AED', symbol: 'د.إ', name: 'UAE Dirham' },
  { code: 'SAR', symbol: '﷼', name: 'Saudi Riyal' },
  { code: 'TRY', symbol: '₺', name: 'Turkish Lira' },
  { code: 'JPY', symbol: '¥', name: 'Japanese Yen' }
];

export function readStoredCurrency(): SupportedCurrency {
  const stored = localStorage.getItem(CURRENCY_STORAGE_KEY);
  return CURRENCY_OPTIONS.some((option) => option.code === stored)
    ? (stored as SupportedCurrency)
    : 'USD';
}

/**
 * Central money formatting. Prices keep their source currency (no client-side
 * conversion), but the user's display preference is exposed for future
 * server-side conversion and locale-aware grouping.
 */
@Injectable({ providedIn: 'root' })
export class CurrencyService {
  readonly displayCurrency = signal<SupportedCurrency>(readStoredCurrency());

  setDisplayCurrency(code: SupportedCurrency): void {
    localStorage.setItem(CURRENCY_STORAGE_KEY, code);
    this.displayCurrency.set(code);
  }

  format(value: number, sourceCurrency: string, display: 'symbol' | 'narrowSymbol' = 'symbol'): string {
    try {
      return new Intl.NumberFormat('en-US', {
        style: 'currency',
        currency: sourceCurrency,
        currencyDisplay: display,
        maximumFractionDigits: value % 1 === 0 ? 0 : 2
      }).format(value);
    } catch {
      return `${sourceCurrency} ${value}`;
    }
  }
}