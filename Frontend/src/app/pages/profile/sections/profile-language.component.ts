import { Component, OnInit } from '@angular/core';
import { CommonModule } from '@angular/common';
import {
  applyLanguage,
  LanguageChoice,
  readStoredLanguage
} from '../profile-utils';

const LANGUAGE_STORAGE_KEY = 'safarni_lang';

@Component({
  selector: 'app-profile-language',
  standalone: true,
  imports: [CommonModule],
  templateUrl: './profile-language.component.html',
  styleUrls: ['../profile-shared.scss'],
})
export class ProfileLanguageComponent implements OnInit {
  languageChoice: LanguageChoice = 'en';

  ngOnInit(): void {
    this.languageChoice = readStoredLanguage();
    applyLanguage(this.languageChoice);
  }

  selectLanguage(lang: LanguageChoice): void {
    this.languageChoice = lang;
    localStorage.setItem(LANGUAGE_STORAGE_KEY, lang);
    applyLanguage(lang);
  }
}
