export interface TravelPackageAgency {
  agencyId: string;
  companyName: string;
  logoUrl: string | null;
  isVerified: boolean;
}

export interface TravelPackageDay {
  id: string;
  dayNumber: number;
  title: string;
  description: string;
  activities: string | null;
}

export interface TravelPackage {
  id: string;
  travelAgencyId: string;
  title: string;
  description: string;
  destination: string;
  startDate: string;
  endDate: string;
  durationDays: number;
  price: number;
  currencyCode: string;
  availableSeats: number;
  coverImageUrl: string | null;
  discountPercentage: number | null;
  offerLabel: string | null;
  isPublished: boolean;
  isActive: boolean;
  createdAt: string;
  updatedAt: string | null;
  agency: TravelPackageAgency;
  days: TravelPackageDay[];
}

export interface MarketplacePage {
  items: TravelPackage[];
  page: number;
  pageSize: number;
  totalCount: number;
  totalPages: number;
}

export interface MarketplaceFilters {
  destination?: string;
  startDate?: string;
  durationDays?: number | null;
  minPrice?: number | null;
  maxPrice?: number | null;
  page?: number;
  pageSize?: number;
}

export interface PackageBookingRequest {
  id: string;
  packageId: string;
  packageTitle: string;
  destination: string;
  travelAgencyId: string;
  companyName: string;
  travelerUserId: string;
  travelerName: string;
  travelerEmail: string;
  travelerCount: number;
  message: string | null;
  status: 'Pending' | 'Accepted' | 'Rejected' | 'Cancelled';
  agencyResponse: string | null;
  createdAt: string;
  updatedAt: string | null;
  respondedAt: string | null;
}

export interface TravelPackageRequest {
  title: string;
  description: string;
  destination: string;
  startDate: string;
  endDate: string;
  durationDays: number;
  price: number;
  currencyCode: string;
  availableSeats: number;
  coverImageUrl: string | null;
  discountPercentage: number | null;
  offerLabel: string | null;
}

export interface TravelPackageDayRequest {
  dayNumber: number;
  title: string;
  description: string;
  activities: string | null;
}
