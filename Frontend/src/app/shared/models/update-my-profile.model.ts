/**
 * Mirrors Safarni.Application.Features.Profile.UpdateMyProfile.UpdateMyProfileCommand
 * Keep this in sync with UpdateMyProfileCommandValidator on the backend:
 *  - FullName: required, max 100
 *  - PhoneNumber: optional, max 20, pattern ^[0-9+\-() ]+$
 *  - DateOfBirth: optional, cannot be in the future
 *  - Country: max 100
 *  - City: max 100
 *  - Bio: max 500
 */
export interface UpdateMyProfileCommand {
  userId: string;
  fullName: string;
  phoneNumber?: string | null;
  dateOfBirth?: string | null; // ISO date string (yyyy-MM-dd)
  country?: string | null;
  city?: string | null;
  bio?: string | null;
}

export interface MyProfile extends UpdateMyProfileCommand {
  email: string;
  avatarUrl?: string | null;
  membershipTier?: string;
}
