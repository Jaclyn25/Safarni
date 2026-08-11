import { TravelPackage } from '../../models/marketplace.models';

export const DEFAULT_PACKAGE_COVER_URL = 'assets/default-package-cover.png';

export function packageCoverUrl(packageItem: TravelPackage): string {
  return packageItem.images.find(image => image.isCover)?.url
    ?? packageItem.coverImageUrl
    ?? DEFAULT_PACKAGE_COVER_URL;
}

export function orderedPackageImages(packageItem: TravelPackage) {
  return [...packageItem.images].sort((left, right) =>
    Number(right.isCover) - Number(left.isCover) || left.sortOrder - right.sortOrder);
}
