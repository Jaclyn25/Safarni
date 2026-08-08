export type AgencyVerificationStatus =
  | 'Pending'
  | 'ChangesRequested'
  | 'Verified'
  | 'Rejected'
  | 'Suspended';

export type AgencyVerificationDocumentType =
  | 'CommercialRegistration'
  | 'TaxCard'
  | 'TourismCompanyLicense';

export type AgencyVerificationDecision =
  | 'Approve'
  | 'RequestChanges'
  | 'Reject'
  | 'Suspend';

export type AgencyDocumentReviewStatus = 'Pending' | 'Approved' | 'Declined';
export type AgencyDocumentDecision = 'Approve' | 'Decline';

export interface AgencyVerificationDocument {
  id: string | null;
  documentType: AgencyVerificationDocumentType;
  displayName: string;
  isRequired: boolean;
  isUploaded: boolean;
  reviewStatus: AgencyDocumentReviewStatus;
  reviewReason: string | null;
  reviewedAt: string | null;
  reviewedByUserId: string | null;
  documentNumber: string | null;
  originalFileName: string | null;
  contentType: string | null;
  fileSizeBytes: number | null;
  expiryDate: string | null;
  uploadedAt: string | null;
  updatedAt: string | null;
}

export interface AgencyVerificationOverview {
  agencyId: string;
  companyName: string;
  verificationStatus: AgencyVerificationStatus;
  decisionReason: string | null;
  reviewedAt: string | null;
  canPublishPackages: boolean;
  uploadedDocumentCount: number;
  requiredDocumentCount: number;
  documents: AgencyVerificationDocument[];
}

export interface AgencyProfile {
  agencyId: string;
  userId: string;
  representativeName: string;
  email: string;
  representativePhone: string | null;
  companyName: string;
  description: string;
  address: string;
  city: string;
  country: string;
  companyPhone: string;
  logoUrl: string | null;
  websiteUrl: string | null;
  verificationStatus: AgencyVerificationStatus;
  verificationDecisionReason: string | null;
  createdAt: string;
  updatedAt: string | null;
}

export interface UpdateAgencyProfileRequest {
  companyName: string;
  description: string;
  address: string;
  city: string;
  country: string;
  companyPhone: string;
  logoUrl: string | null;
  websiteUrl: string | null;
}

export interface AgencyOnboardingStatus {
  isEmailVerified: boolean;
  isCompanyProfileComplete: boolean;
  agencyId: string | null;
}

export interface AdminAgencySummary {
  agencyId: string;
  companyName: string;
  representativeName: string;
  email: string;
  city: string;
  country: string;
  verificationStatus: AgencyVerificationStatus;
  uploadedDocumentCount: number;
  requiredDocumentCount: number;
  createdAt: string;
  updatedAt: string | null;
}

export interface AdminAgencyDetail {
  profile: AgencyProfile;
  verification: AgencyVerificationOverview;
  reviewedByUserId: string | null;
}

export interface PagedResult<T> {
  items: T[];
  page: number;
  pageSize: number;
  totalCount: number;
  totalPages: number;
}
