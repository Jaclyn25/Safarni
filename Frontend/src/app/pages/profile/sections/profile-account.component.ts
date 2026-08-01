import { Component, OnInit, signal } from '@angular/core';
import { CommonModule } from '@angular/common';
import { FormBuilder, FormGroup, FormsModule, ReactiveFormsModule, Validators } from '@angular/forms';
import { ProfileService, getProfileErrorMessage } from '../../../services/profile.service';
import { MyProfile, UpdateMyProfileCommand } from '../../../shared/models/update-my-profile.model';

@Component({
  selector: 'app-profile-account',
  standalone: true,
  imports: [CommonModule, FormsModule, ReactiveFormsModule],
  templateUrl: './profile-account.component.html',
  styleUrls: ['../profile-shared.scss'],
})
export class ProfileAccountComponent implements OnInit {
  profile: MyProfile | null = null;
  profileForm: FormGroup;

  readonly isSaving = signal(false);
  readonly isLoading = signal(true);
  readonly saveError = signal<string | null>(null);
  readonly saveSuccess = signal(false);

  constructor(
    private fb: FormBuilder,
    private profileService: ProfileService
  ) {
    // Mirrors UpdateMyProfileCommandValidator on the backend exactly,
    // so client-side errors match server-side ones field for field.
    this.profileForm = this.fb.group({
      fullName: ['', [Validators.required, Validators.maxLength(100)]],
      email: ['', [Validators.required, Validators.email]],
      phoneNumber: ['', [Validators.maxLength(20), Validators.pattern(/^[0-9+\-() ]+$/)]],
      dateOfBirth: ['', [this.notInFutureValidator]],
      country: ['', [Validators.maxLength(100)]],
      city: ['', [Validators.maxLength(100)]],
      bio: ['', [Validators.maxLength(500)]],
    });
  }

  ngOnInit(): void {
    this.profileService.getMyProfile().subscribe({
      next: (profile) => {
        this.profile = profile;
        this.profileForm.patchValue({
          fullName: profile.fullName,
          email: profile.email,
          phoneNumber: profile.phoneNumber ?? '',
          dateOfBirth: profile.dateOfBirth ?? '',
          country: profile.country ?? '',
          city: profile.city ?? '',
          bio: profile.bio ?? '',
        });
        this.isLoading.set(false);
      },
      error: (error: unknown) => {
        this.isLoading.set(false);
        this.saveError.set(
          getProfileErrorMessage(error, 'Could not load your profile. Please refresh the page.')
        );
      },
    });
  }

  private notInFutureValidator(control: { value: string }) {
    if (!control.value) return null;
    const date = new Date(control.value);
    return date.getTime() > Date.now() ? { dateInFuture: true } : null;
  }

  onAvatarSelected(event: Event): void {
    const input = event.target as HTMLInputElement;
    const file = input.files?.[0];
    if (!file) return;

    this.profileService.uploadAvatar(file).subscribe({
      next: (avatarUrl) => {
        if (this.profile) {
          this.profile.avatarUrl = avatarUrl;
          this.saveSuccess.set(true);
        }
      },
      error: (error: unknown) => {
        this.saveError.set(
          getProfileErrorMessage(error, 'Could not upload your photo. Please try again.')
        );
      },
    });
  }

  discardChanges(): void {
    if (!this.profile) return;
    this.profileForm.patchValue({
      fullName: this.profile.fullName,
      email: this.profile.email,
      phoneNumber: this.profile.phoneNumber ?? '',
      dateOfBirth: this.profile.dateOfBirth ?? '',
      country: this.profile.country ?? '',
      city: this.profile.city ?? '',
      bio: this.profile.bio ?? '',
    });
    this.saveError.set(null);
    this.saveSuccess.set(false);
  }

  saveChanges(): void {
    if (this.profileForm.invalid || !this.profile) {
      this.profileForm.markAllAsTouched();
      return;
    }

    this.isSaving.set(true);
    this.saveError.set(null);
    this.saveSuccess.set(false);

    const command: UpdateMyProfileCommand = {
      userId: this.profile.userId,
      fullName: this.profileForm.value.fullName,
      phoneNumber: this.profileForm.value.phoneNumber || null,
      dateOfBirth: this.profileForm.value.dateOfBirth || null,
      country: this.profileForm.value.country || null,
      city: this.profileForm.value.city || null,
      bio: this.profileForm.value.bio || null,
    };

    this.profileService.updateMyProfile(command).subscribe({
      next: (updated) => {
        this.profile = updated;
        this.isSaving.set(false);
        this.saveSuccess.set(true);
      },
      error: (error: unknown) => {
        this.isSaving.set(false);
        this.saveError.set(
          getProfileErrorMessage(error, 'Could not save your changes. Please check the form and try again.')
        );
      },
    });
  }

  get f() {
    return this.profileForm.controls;
  }
}
