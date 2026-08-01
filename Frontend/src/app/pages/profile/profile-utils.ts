export type ThemeChoice = 'light' | 'dark' | 'system';
export type LanguageChoice = 'en' | 'ar';

export const THEME_STORAGE_KEY = 'safarni_theme';
export const LANGUAGE_STORAGE_KEY = 'safarni_lang';
export const CURRENCY_STORAGE_KEY = 'safarni_currency';

/** Applies (or clears) the `dark` class Tailwind listens to. */
export function applyThemeClass(theme: ThemeChoice): void {
  const prefersDark = window.matchMedia('(prefers-color-scheme: dark)').matches;

  if (theme === 'dark' || (theme === 'system' && prefersDark)) {
    document.documentElement.classList.add('dark');
  } else {
    document.documentElement.classList.remove('dark');
  }
}

export function readStoredTheme(): ThemeChoice {
  const stored = localStorage.getItem(THEME_STORAGE_KEY);
  return stored === 'dark' || stored === 'light' || stored === 'system'
    ? stored
    : 'system';
}

/**
 * Applies the app-wide language: sets <html lang> and flips
 * the document direction so Tailwind `rtl:`/`ltr:` variants work.
 */
export function applyLanguage(lang: LanguageChoice): void {
  document.documentElement.lang = lang;
  document.documentElement.dir = lang === 'ar' ? 'rtl' : 'ltr';
}

export function readStoredLanguage(): LanguageChoice {
  return localStorage.getItem(LANGUAGE_STORAGE_KEY) === 'ar' ? 'ar' : 'en';
}

export function readStoredCurrency(): string {
  return localStorage.getItem(CURRENCY_STORAGE_KEY) ?? 'USD';
}
