const DESTINATION_IMAGES: Array<[string[], string]> = [
  [['turkey', 'turkiye', 'istanbul', 'izmir', 'antalya', 'cappadocia'], 'assets/countries/turkey.png'],
  [['thailand', 'bangkok', 'phuket'], 'assets/countries/thailand.png'],
  [['maldives', 'male'], 'assets/countries/maldives.png'],
  [['dubai', 'united arab emirates', 'uae'], 'assets/countries/dubai.png'],
  [['japan', 'tokyo', 'kyoto', 'osaka'], 'https://images.unsplash.com/photo-1493976040374-85c8e12f0c0e?w=1800&q=88&auto=format'],
  [['france', 'paris', 'lyon', 'nice'], 'https://images.unsplash.com/photo-1502602898657-3e91760cbb34?w=1800&q=88&auto=format'],
  [['italy', 'rome', 'florence', 'venice', 'amalfi'], 'https://images.unsplash.com/photo-1533105079780-92b9be482077?w=1800&q=88&auto=format'],
  [['egypt', 'cairo', 'luxor', 'aswan', 'hurghada', 'dahab', 'sharm'], 'https://images.unsplash.com/photo-1503177119275-0aa32b3a9368?w=1800&q=88&auto=format'],
  [['spain', 'barcelona', 'madrid', 'seville'], 'https://images.unsplash.com/photo-1509840841025-9088ba78a826?w=1800&q=88&auto=format'],
  [['morocco', 'marrakech', 'casablanca', 'fes'], 'https://images.unsplash.com/photo-1489493585363-d69421e0edd3?w=1800&q=88&auto=format'],
  [['united kingdom', 'england', 'london'], 'https://images.unsplash.com/photo-1513635269975-59663e0ac1ad?w=1800&q=88&auto=format'],
  [['poland', 'krakow', 'warsaw'], 'https://images.unsplash.com/photo-1519197924294-4ba991a11128?w=1800&q=88&auto=format']
];

export const DEFAULT_DESTINATION_IMAGE =
  'https://images.unsplash.com/photo-1469474968028-56623f02e42e?w=1800&q=88&auto=format';

export function destinationImageUrl(source: string): string {
  const normalized = source
    .toLocaleLowerCase()
    .normalize('NFD')
    .replace(/[\u0300-\u036f]/g, '');

  return DESTINATION_IMAGES.find(([keys]) =>
    keys.some((key) => normalized.includes(key)))?.[1] ?? DEFAULT_DESTINATION_IMAGE;
}
