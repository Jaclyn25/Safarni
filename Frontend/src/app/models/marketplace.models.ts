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
  cityId: string | null;
  cityName: string | null;
  items: TravelPackageItem[];
  images: TravelPackageImage[];
}

export type TravelPackageItemType =
  | 'Place' | 'Activity' | 'Restaurant' | 'Accommodation' | 'Transport' | 'WalkingTour' | 'Other';
export type TravelPackageItemSource = 'Manual' | 'Provider';

export interface TravelPackageImage {
  id: string;
  url: string;
  caption: string | null;
  altText: string | null;
  sortOrder: number;
  isCover: boolean;
}

export interface TravelPackageCity {
  id: string;
  name: string;
  orderIndex: number;
  nights: number;
  description: string | null;
}

export interface TravelPackageItem {
  id: string;
  type: TravelPackageItemType;
  source: TravelPackageItemSource;
  orderIndex: number;
  title: string;
  description: string | null;
  locationName: string | null;
  address: string | null;
  latitude: number | null;
  longitude: number | null;
  mapUrl: string | null;
  provider: string | null;
  providerPlaceId: string | null;
  providerPhotoUrl: string | null;
  websiteUrl: string | null;
  cuisine: string | null;
  mealType: string | null;
  reservationRequired: boolean | null;
  accommodationType: string | null;
  starRating: number | null;
  roomType: string | null;
  boardBasis: string | null;
  transportMode: string | null;
  fromLocation: string | null;
  toLocation: string | null;
  meetingPoint: string | null;
  difficultyLevel: string | null;
  additionalDetails: string | null;
  images: TravelPackageImage[];
}

export type TravelPackageItemRequest = Omit<TravelPackageItem, 'id' | 'images'>;

export interface PackagePlaceSearchResult {
  provider: string;
  providerPlaceId: string;
  name: string;
  address: string;
  description: string | null;
  rating: number | null;
  reviewCount: number | null;
  latitude: number | null;
  longitude: number | null;
  mapUrl: string | null;
  photoUrl: string | null;
  websiteUrl: string | null;
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
  remainingSeats: number;
  coverImageUrl: string | null;
  discountPercentage: number | null;
  offerLabel: string | null;
  inclusions: string | null;
  exclusions: string | null;
  importantNotes: string | null;
  cancellationPolicy: string | null;
  isPublished: boolean;
  isActive: boolean;
  createdAt: string;
  updatedAt: string | null;
  agency: TravelPackageAgency;
  cities: TravelPackageCity[];
  days: TravelPackageDay[];
  images: TravelPackageImage[];
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
  packageEndDate: string;
  travelAgencyId: string;
  companyName: string;
  travelerUserId: string;
  travelerName: string;
  travelerEmail: string;
  travelerCount: number;
  adultCount: number;
  childCount: number;
  childrenAges: number[];
  contactPhone: string;
  alternatePhone: string | null;
  travelerCountry: string;
  travelerCity: string;
  message: string | null;
  status: 'Pending' | 'Accepted' | 'Rejected' | 'Cancelled';
  agencyResponse: string | null;
  createdAt: string;
  updatedAt: string | null;
  respondedAt: string | null;
  hasReview: boolean;
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
  inclusions: string | null;
  exclusions: string | null;
  importantNotes: string | null;
  cancellationPolicy: string | null;
}

export interface TravelPackageDayRequest {
  dayNumber: number;
  title: string;
  description: string;
}

export interface TravelPackageCityRequest {
  name: string;
  nights: number;
  description: string | null;
}

export interface PublicAgencyReview {
  id: string;
  travelerName: string;
  packageTitle: string;
  rating: number;
  comment: string;
  agencyResponse: string | null;
  createdAt: string;
  agencyRespondedAt: string | null;
}

export interface PublicAgencyProfile {
  agencyId: string;
  companyName: string;
  description: string;
  city: string;
  country: string;
  companyPhone: string;
  logoUrl: string | null;
  websiteUrl: string | null;
  isVerified: boolean;
  memberSince: string;
  averageRating: number;
  reviewCount: number;
  verifiedBookings: number;
  travelersServed: number;
  packages: TravelPackage[];
  recentReviews: PublicAgencyReview[];
}
