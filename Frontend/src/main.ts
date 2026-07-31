import { bootstrapApplication } from '@angular/platform-browser';
import { appConfig } from './app/app.config';
import { AppComponent } from './app/app.component';

// Apply the saved theme before the app renders so there is no flash.
// Mirrors the theme picker in the profile page (safarni_theme key).
(() => {
  const stored = localStorage.getItem('safarni_theme');
  const theme = stored === 'dark' || stored === 'light' || stored === 'system'
    ? stored
    : 'system';
  const prefersDark =
    window.matchMedia('(prefers-color-scheme: dark)').matches;

  if (theme === 'dark' || (theme === 'system' && prefersDark)) {
    document.documentElement.classList.add('dark');
  }

  // Apply the saved language (safarni_lang key) and flip to RTL for Arabic.
  const lang = localStorage.getItem('safarni_lang') === 'ar' ? 'ar' : 'en';
  if (lang === 'ar') {
    document.documentElement.lang = 'ar';
    document.documentElement.dir = 'rtl';
  }
})();

bootstrapApplication(AppComponent, appConfig).catch((err) => console.error(err));
