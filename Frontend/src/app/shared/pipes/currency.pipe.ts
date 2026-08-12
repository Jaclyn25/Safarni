import { Pipe, PipeTransform, inject } from '@angular/core';
import { CurrencyService } from '../../services/currency.service';

@Pipe({
  name: 'currency',
  standalone: true,
  pure: false
})
export class CurrencyPipe implements PipeTransform {
  private readonly currencyService = inject(CurrencyService);

  transform(
    value: number | null | undefined,
    sourceCurrency: string = 'USD',
    display: 'symbol' | 'narrowSymbol' = 'symbol'
  ): string {
    if (value === null || value === undefined || Number.isNaN(value)) {
      return '';
    }
    return this.currencyService.format(value, sourceCurrency, display);
  }
}