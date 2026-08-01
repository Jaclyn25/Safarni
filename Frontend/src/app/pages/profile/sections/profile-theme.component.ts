import { Component, OnDestroy, OnInit } from '@angular/core';
import { CommonModule } from '@angular/common';
import {
  applyThemeClass,
  readStoredTheme,
  ThemeChoice
} from '../profile-utils';

const THEME_STORAGE_KEY = 'safarni_theme';

@Component({
  selector: 'app-profile-theme',
  standalone: true,
  imports: [CommonModule],
  templateUrl: './profile-theme.component.html',
  styleUrls: ['../profile-shared.scss'],
})
export class ProfileThemeComponent implements OnInit, OnDestroy {
  themeChoice: ThemeChoice = 'system';

  private readonly systemThemeQuery =
    window.matchMedia('(prefers-color-scheme: dark)');

  private readonly onSystemThemeChange = (
    event: MediaQueryListEvent
  ): void => {
    if (this.themeChoice === 'system') {
      applyThemeClass('system');
      void event;
    }
  };

  ngOnInit(): void {
    this.themeChoice = readStoredTheme();
    applyThemeClass(this.themeChoice);
    this.systemThemeQuery.addEventListener(
      'change',
      this.onSystemThemeChange
    );
  }

  ngOnDestroy(): void {
    this.systemThemeQuery.removeEventListener(
      'change',
      this.onSystemThemeChange
    );
  }

  selectTheme(theme: ThemeChoice): void {
    this.themeChoice = theme;
    localStorage.setItem(THEME_STORAGE_KEY, theme);
    applyThemeClass(theme);
  }
}
