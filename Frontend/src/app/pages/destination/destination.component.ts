import { CommonModule } from '@angular/common';
import { Component, OnInit, inject } from '@angular/core';
import { ActivatedRoute, Router, RouterLink } from '@angular/router';
import { TravelPackage } from '../../models/marketplace.models';
import { CurrencyService } from '../../services/currency.service';
import { MarketplaceService } from '../../services/marketplace.service';
import {
  PublicTripSearchResponse,
  PublicTripService
} from '../../services/public-trip.service';
import { packageCoverUrl } from '../../shared/constants/marketplace.constants';

export interface QuickInfo {
  bestTimeToVisit: string;
  suggestedLength: string;
  currency: string;
  language: string;
  weatherVibe: string;
}

export interface TopPlace {
  name: string;
  description: string;
  suitableFor: string[];
  image: string;
}

export interface ExperienceIdea {
  id: string;
  icon: string;
  title: string;
  description: string;
  tripName: string;
}

export interface Activity {
  id: string;
  title: string;
  category: string;
  description: string;
  image: string;
}

export interface PracticalTip {
  icon: string;
  title: string;
  content: string;
}

export interface DestinationData {
  id: string;
  country: string;
  planningDestination: string;
  marketplaceQuery: string;
  communityQuery: string;
  tagline: string;
  description: string;
  heroImage: string;
  tags: string[];
  quickInfo: QuickInfo;
  topPlaces: TopPlace[];
  experienceIdeas: ExperienceIdea[];
  thingsToDo: Activity[];
  travelTips: PracticalTip[];
}

@Component({
  selector: 'app-destination',
  standalone: true,
  imports: [CommonModule, RouterLink],
  templateUrl: './destination.component.html'
})
export class DestinationComponent implements OnInit {
  private readonly route = inject(ActivatedRoute);
  private readonly router = inject(Router);
  private readonly marketplaceService = inject(MarketplaceService);
  private readonly publicTripService = inject(PublicTripService);
  private readonly currencyService = inject(CurrencyService);

  destinationId = 'maldives';
  destination!: DestinationData;

  agencyPackages: TravelPackage[] = [];
  communityTrips: PublicTripSearchResponse[] = [];

  isLoadingPackages = false;
  isLoadingCommunityTrips = false;
  packageError = '';
  communityError = '';

  private readonly destinationDatabase: Record<string, DestinationData> = {
    maldives: {
      id: 'maldives',
      country: 'Maldives',
      planningDestination: 'Maldives',
      marketplaceQuery: 'Maldives',
      communityQuery: 'Maldives',
      tagline: 'Clear-water islands, coral reefs and slow tropical days',
      description:
        'Use this guide for inspiration, then choose how you want to continue: build a personal trip with Safarni, discover trips shared by travelers, or browse real packages from verified travel agencies.',
      heroImage: 'assets/countries/maldives.webp',
      tags: ['Beach', 'Islands', 'Diving'],
      quickInfo: {
        bestTimeToVisit: 'November – April',
        suggestedLength: '5 – 7 days',
        currency: 'Maldivian Rufiyaa (MVR)',
        language: 'Dhivehi · English widely used in tourism',
        weatherVibe: 'Tropical and warm year-round'
      },
      topPlaces: [
        {
          name: 'Malé',
          description:
            'The compact capital is a useful stop for local markets, mosques, cafés and a glimpse of everyday Maldivian life.',
          suitableFor: ['Culture', 'Local food', 'Short stop'],
          image:
            'https://images.unsplash.com/photo-1573843981267-be1999ff37cd?w=900&q=80&auto=format'
        },
        {
          name: 'Maafushi',
          description:
            'A lively local island known for guesthouses, watersports and easy access to boat excursions.',
          suitableFor: ['Watersports', 'Local island', 'Couples'],
          image:
            'https://images.unsplash.com/photo-1514282401047-d79a71a590e8?w=900&q=80&auto=format'
        },
        {
          name: 'Baa Atoll',
          description:
            'A UNESCO Biosphere Reserve and one of the country’s best-known areas for marine life and reef experiences.',
          suitableFor: ['Diving', 'Nature', 'Marine life'],
          image:
            'https://images.unsplash.com/photo-1544551763-46a013bb70d5?w=900&q=80&auto=format'
        },
        {
          name: 'Ari Atoll',
          description:
            'A broad atoll with resort islands, dive sites and a strong reputation for underwater experiences.',
          suitableFor: ['Diving', 'Resorts', 'Relaxation'],
          image:
            'https://images.unsplash.com/photo-1507525428034-b723cf961d3e?w=900&q=80&auto=format'
        }
      ],
      experienceIdeas: [
        {
          id: 'maldives-relaxed',
          icon: 'spa',
          title: 'Relaxed island escape',
          description:
            'Keep the pace light with beach time, spa moments, sunset views and room for unplanned days.',
          tripName: 'Relaxed Maldives Escape'
        },
        {
          id: 'maldives-ocean',
          icon: 'scuba_diving',
          title: 'Ocean & diving adventure',
          description:
            'Shape the trip around snorkeling, dive sites, boat days and the best marine-life areas.',
          tripName: 'Maldives Ocean Adventure'
        },
        {
          id: 'maldives-romantic',
          icon: 'favorite',
          title: 'Romantic getaway',
          description:
            'Plan a slower couples trip with scenic stays, private experiences and memorable evenings.',
          tripName: 'Romantic Maldives Getaway'
        }
      ],
      thingsToDo: [
        {
          id: 'maldives-snorkel',
          title: 'Snorkeling & reef exploration',
          category: 'Ocean',
          description:
            'Explore coral gardens and look for tropical fish, rays and sea turtles.',
          image:
            'https://images.unsplash.com/photo-1544551763-46a013bb70d5?w=700&q=80&auto=format'
        },
        {
          id: 'maldives-cruise',
          title: 'Sunset boat cruise',
          category: 'Scenic',
          description:
            'Spend an evening on the water and build the day around sunset views.',
          image:
            'https://images.unsplash.com/photo-1507525428034-b723cf961d3e?w=700&q=80&auto=format'
        },
        {
          id: 'maldives-sandbank',
          title: 'Sandbank day',
          category: 'Relaxation',
          description:
            'Add a quiet white-sand stop to an island-hopping or boat itinerary.',
          image:
            'https://images.unsplash.com/photo-1514282401047-d79a71a590e8?w=700&q=80&auto=format'
        },
        {
          id: 'maldives-diving',
          title: 'Scuba diving',
          category: 'Adventure',
          description:
            'Choose dive areas according to your certification level and the season.',
          image:
            'https://images.unsplash.com/photo-1544551763-46a013bb70d5?w=700&q=80&auto=format'
        }
      ],
      travelTips: [
        {
          icon: 'directions_boat',
          title: 'Think about transfers early',
          content:
            'Island transfers can shape the whole itinerary. Check whether your route needs a speedboat, domestic flight or seaplane before locking in the order of your stays.'
        },
        {
          icon: 'payments',
          title: 'Separate estimates from bookable prices',
          content:
            'Use Safarni’s planner for personal cost estimates. Prices shown in the agency section below come from real marketplace packages.'
        },
        {
          icon: 'checkroom',
          title: 'Resort and local-island rules differ',
          content:
            'Local islands and resort islands can have different customs and dress expectations, so check the rules for the places on your final route.'
        }
      ]
    },
    thailand: {
      id: 'thailand',
      country: 'Thailand',
      planningDestination: 'Thailand',
      marketplaceQuery: 'Thailand',
      communityQuery: 'Thailand',
      tagline: 'Temples, street food, northern mountains and tropical islands',
      description:
        'Use the destination guide to decide what kind of Thailand trip you want, then continue with Safarni’s planner, traveler community or verified-agency marketplace.',
      heroImage: 'assets/countries/thailand.webp',
      tags: ['Culture', 'Food', 'Islands'],
      quickInfo: {
        bestTimeToVisit: 'November – February',
        suggestedLength: '7 – 10 days',
        currency: 'Thai Baht (THB)',
        language: 'Thai · English common in major visitor areas',
        weatherVibe: 'Tropical, with regional and seasonal variation'
      },
      topPlaces: [
        {
          name: 'Bangkok',
          description:
            'A high-energy capital for temples, neighborhoods, markets, rooftop views and street food.',
          suitableFor: ['Culture', 'Food', 'City life'],
          image:
            'https://images.unsplash.com/photo-1508009603885-50cf7c579365?w=900&q=80&auto=format'
        },
        {
          name: 'Chiang Mai',
          description:
            'A northern base for temples, mountain scenery, markets and slower days.',
          suitableFor: ['Culture', 'Nature', 'Slow travel'],
          image:
            'https://images.unsplash.com/photo-1512553353614-82a7370096dc?w=900&q=80&auto=format'
        },
        {
          name: 'Phuket',
          description:
            'A practical island base with beaches, nightlife and access to boat trips.',
          suitableFor: ['Beach', 'Nightlife', 'Boat trips'],
          image:
            'https://images.unsplash.com/photo-1589394815804-964ed0be2eb5?w=900&q=80&auto=format'
        },
        {
          name: 'Krabi',
          description:
            'Known for limestone scenery, beaches and connections to nearby islands.',
          suitableFor: ['Scenery', 'Adventure', 'Islands'],
          image:
            'https://images.unsplash.com/photo-1537996194471-e657df975ab4?w=900&q=80&auto=format'
        }
      ],
      experienceIdeas: [
        {
          id: 'thailand-first-time',
          icon: 'route',
          title: 'First-time Thailand',
          description:
            'Balance a major city, northern culture and a few beach days without rushing every stop.',
          tripName: 'First-Time Thailand Journey'
        },
        {
          id: 'thailand-food',
          icon: 'restaurant',
          title: 'Food-led journey',
          description:
            'Build around markets, street food, regional dishes and neighborhood exploration.',
          tripName: 'Thailand Food & Culture Trip'
        },
        {
          id: 'thailand-islands',
          icon: 'beach_access',
          title: 'Island-focused escape',
          description:
            'Keep the route coastal with beaches, boat days, snorkeling and relaxed evenings.',
          tripName: 'Thailand Island Escape'
        }
      ],
      thingsToDo: [
        {
          id: 'thailand-food-tour',
          title: 'Street-food exploration',
          category: 'Food',
          description:
            'Plan an evening around markets, small restaurants and local specialties.',
          image:
            'https://images.unsplash.com/photo-1508009603885-50cf7c579365?w=700&q=80&auto=format'
        },
        {
          id: 'thailand-island-hop',
          title: 'Island-hopping day',
          category: 'Ocean',
          description:
            'Compare routes and choose a boat day that fits the region and your pace.',
          image:
            'https://images.unsplash.com/photo-1589394815804-964ed0be2eb5?w=700&q=80&auto=format'
        },
        {
          id: 'thailand-temples',
          title: 'Temple & heritage day',
          category: 'Culture',
          description:
            'Group nearby heritage sites together so the day stays walkable and realistic.',
          image:
            'https://images.unsplash.com/photo-1528181304800-259b08848526?w=700&q=80&auto=format'
        }
      ],
      travelTips: [
        {
          icon: 'map',
          title: 'Do not overpack the route',
          content:
            'Thailand is large. A better trip often means choosing two or three regions instead of trying to cover the whole country in one week.'
        },
        {
          icon: 'train',
          title: 'Compare internal travel time',
          content:
            'Flights, overnight trains, ferries and road transfers can all make sense depending on the route. Include transfer time when you assign days.'
        },
        {
          icon: 'cloud',
          title: 'Check the coast by season',
          content:
            'Weather patterns vary between regions and coasts, so check the exact islands on your route rather than relying on one national forecast.'
        }
      ]
    },
    turkey: {
      id: 'turkey',
      country: 'Türkiye',
      planningDestination: 'Türkiye',
      marketplaceQuery: 'Turkey',
      communityQuery: 'Turkey',
      tagline: 'Historic cities, dramatic landscapes and food-rich neighborhoods',
      description:
        'Explore Türkiye as inspiration first, then move into the real Safarni flow: create your own trip, learn from published traveler trips, or compare verified-agency packages.',
      heroImage: 'assets/countries/turkey.webp',
      tags: ['History', 'Cities', 'Food'],
      quickInfo: {
        bestTimeToVisit: 'April – May · September – October',
        suggestedLength: '7 – 10 days',
        currency: 'Turkish Lira (TRY)',
        language: 'Turkish · English varies by area',
        weatherVibe: 'Varied by coast, city and season'
      },
      topPlaces: [
        {
          name: 'Istanbul',
          description:
            'A city spanning two continents, with landmark architecture, ferry rides, markets and layered neighborhoods.',
          suitableFor: ['History', 'Food', 'City life'],
          image:
            'https://images.unsplash.com/photo-1524231757912-21f4fe3a7200?w=900&q=80&auto=format'
        },
        {
          name: 'Cappadocia',
          description:
            'Known for unusual rock formations, cave stays, valleys and sunrise viewpoints.',
          suitableFor: ['Scenery', 'Photography', 'Adventure'],
          image:
            'https://images.unsplash.com/photo-1528181304800-259b08848526?w=900&q=80&auto=format'
        },
        {
          name: 'Antalya',
          description:
            'A Mediterranean base combining coast, old-town streets and nearby historical sites.',
          suitableFor: ['Coast', 'History', 'Relaxation'],
          image:
            'https://images.unsplash.com/photo-1602002418082-a4443e081dd1?w=900&q=80&auto=format'
        },
        {
          name: 'İzmir & Ephesus',
          description:
            'Use İzmir as a base for Aegean food, coastal atmosphere and major archaeological sites.',
          suitableFor: ['History', 'Coast', 'Culture'],
          image:
            'https://images.unsplash.com/photo-1564507592333-c60657eea523?w=900&q=80&auto=format'
        }
      ],
      experienceIdeas: [
        {
          id: 'turkey-classic',
          icon: 'account_balance',
          title: 'Classic culture route',
          description:
            'Combine Istanbul with one or two major heritage regions and leave enough time to experience each place.',
          tripName: 'Classic Türkiye Journey'
        },
        {
          id: 'turkey-food',
          icon: 'restaurant_menu',
          title: 'Food & neighborhoods',
          description:
            'Build the itinerary around markets, regional dishes, café streets and local neighborhoods.',
          tripName: 'Türkiye Food & Neighborhoods Trip'
        },
        {
          id: 'turkey-coast',
          icon: 'sailing',
          title: 'Aegean & Mediterranean',
          description:
            'Slow the trip down with coastal towns, beaches and historic stops along the shore.',
          tripName: 'Türkiye Coastal Escape'
        }
      ],
      thingsToDo: [
        {
          id: 'turkey-bosphorus',
          title: 'Bosphorus ferry day',
          category: 'Scenic',
          description:
            'Use ferries to connect neighborhoods and see the city from the water.',
          image:
            'https://images.unsplash.com/photo-1524231757912-21f4fe3a7200?w=700&q=80&auto=format'
        },
        {
          id: 'turkey-cappadocia',
          title: 'Cappadocia valley exploration',
          category: 'Adventure',
          description:
            'Plan viewpoints and walks around the weather, distances and your preferred pace.',
          image:
            'https://images.unsplash.com/photo-1528181304800-259b08848526?w=700&q=80&auto=format'
        },
        {
          id: 'turkey-food-markets',
          title: 'Market & food day',
          category: 'Food',
          description:
            'Build a flexible day around bakeries, markets, cafés and regional dishes.',
          image:
            'https://images.unsplash.com/photo-1547592180-85f173990554?w=700&q=80&auto=format'
        }
      ],
      travelTips: [
        {
          icon: 'route',
          title: 'Group regions realistically',
          content:
            'Istanbul, Cappadocia and the Mediterranean coast are not next to each other. Treat domestic transfers as real itinerary time.'
        },
        {
          icon: 'calendar_month',
          title: 'Book high-demand experiences early',
          content:
            'Popular experiences can fill up in busy periods. Once your route is final, confirm anything that has limited capacity directly with its provider or agency.'
        }
      ]
    },
    dubai: {
      id: 'dubai',
      country: 'Dubai, UAE',
      planningDestination: 'United Arab Emirates',
      marketplaceQuery: 'Dubai',
      communityQuery: 'Dubai',
      tagline: 'Modern city energy, desert landscapes and polished short-break experiences',
      description:
        'Use Dubai as a destination starting point, not as a fake package catalogue. Safarni can then hand you into personal planning, community inspiration or real agency offers.',
      heroImage: 'assets/countries/dubai.webp',
      tags: ['City', 'Luxury', 'Desert'],
      quickInfo: {
        bestTimeToVisit: 'November – March',
        suggestedLength: '4 – 6 days',
        currency: 'UAE Dirham (AED)',
        language: 'Arabic · English widely used',
        weatherVibe: 'Hot desert climate; milder in winter'
      },
      topPlaces: [
        {
          name: 'Downtown Dubai',
          description:
            'A central area for skyline views, major attractions, shopping and evening activity.',
          suitableFor: ['First visit', 'City views', 'Shopping'],
          image:
            'https://images.unsplash.com/photo-1512453979798-5ea266f8880c?w=900&q=80&auto=format'
        },
        {
          name: 'Old Dubai',
          description:
            'Explore creek-side neighborhoods, souks, museums and older parts of the city.',
          suitableFor: ['Culture', 'Markets', 'Walking'],
          image:
            'https://images.unsplash.com/photo-1528702748617-c64d49f918af?w=900&q=80&auto=format'
        },
        {
          name: 'Dubai Marina & JBR',
          description:
            'A waterfront area that works well for beaches, dining and evening walks.',
          suitableFor: ['Beach', 'Dining', 'Nightlife'],
          image:
            'https://images.unsplash.com/photo-1518684079-3c830dcef090?w=900&q=80&auto=format'
        },
        {
          name: 'Dubai Desert',
          description:
            'Add a desert experience outside the dense city itinerary for a very different landscape.',
          suitableFor: ['Desert', 'Adventure', 'Sunset'],
          image:
            'https://images.unsplash.com/photo-1509316785289-025f5b846b35?w=900&q=80&auto=format'
        }
      ],
      experienceIdeas: [
        {
          id: 'dubai-city',
          icon: 'location_city',
          title: 'Modern city break',
          description:
            'Focus on architecture, neighborhoods, dining and a few signature attractions.',
          tripName: 'Dubai City Break'
        },
        {
          id: 'dubai-family',
          icon: 'family_restroom',
          title: 'Family-friendly Dubai',
          description:
            'Balance indoor attractions, easy transport, beaches and shorter activity blocks.',
          tripName: 'Family Trip to Dubai'
        },
        {
          id: 'dubai-desert',
          icon: 'landscape',
          title: 'City + desert',
          description:
            'Mix the city with a desert day and a slower waterfront evening.',
          tripName: 'Dubai City & Desert Trip'
        }
      ],
      thingsToDo: [
        {
          id: 'dubai-creek',
          title: 'Explore Dubai Creek',
          category: 'Culture',
          description:
            'Use the creek as a route through souks, older neighborhoods and local transport.',
          image:
            'https://images.unsplash.com/photo-1528702748617-c64d49f918af?w=700&q=80&auto=format'
        },
        {
          id: 'dubai-desert-day',
          title: 'Desert experience',
          category: 'Adventure',
          description:
            'Compare departure times, included activities and transport before choosing a provider.',
          image:
            'https://images.unsplash.com/photo-1509316785289-025f5b846b35?w=700&q=80&auto=format'
        },
        {
          id: 'dubai-waterfront',
          title: 'Waterfront evening',
          category: 'Relaxed',
          description:
            'Keep one evening open for a marina, beach or creek-side walk and dinner.',
          image:
            'https://images.unsplash.com/photo-1518684079-3c830dcef090?w=700&q=80&auto=format'
        }
      ],
      travelTips: [
        {
          icon: 'thermostat',
          title: 'Plan around heat',
          content:
            'In hotter months, schedule outdoor activities for early morning or evening and keep midday plans flexible.'
        },
        {
          icon: 'directions_car',
          title: 'Allow time between districts',
          content:
            'Dubai looks compact on a map, but crossing between major districts can take meaningful time depending on traffic and transport choice.'
        }
      ]
    },
    japan: {
      id: 'japan',
      country: 'Japan',
      planningDestination: 'Japan',
      marketplaceQuery: 'Japan',
      communityQuery: 'Japan',
      tagline: 'Historic districts, fast cities, regional food and precise rail journeys',
      description:
        'Use the guide to choose a manageable route, then let Safarni connect the destination to your planner, community trips and real marketplace packages.',
      heroImage: 'assets/countries/japan.webp',
      tags: ['Culture', 'Food', 'Rail'],
      quickInfo: {
        bestTimeToVisit: 'March – May · October – November',
        suggestedLength: '8 – 12 days',
        currency: 'Japanese Yen (JPY)',
        language: 'Japanese',
        weatherVibe: 'Four distinct seasons; conditions vary by region'
      },
      topPlaces: [
        {
          name: 'Tokyo',
          description:
            'A huge city best planned by neighborhoods, with food, design, shopping and day-trip options.',
          suitableFor: ['City life', 'Food', 'Modern culture'],
          image:
            'https://images.unsplash.com/photo-1540959733332-eab4deabeeaf?w=900&q=80&auto=format'
        },
        {
          name: 'Kyoto',
          description:
            'Historic districts, temples, gardens and traditional streets reward a slower schedule.',
          suitableFor: ['History', 'Temples', 'Walking'],
          image:
            'https://images.unsplash.com/photo-1493976040374-85c8e12f0c0e?w=900&q=80&auto=format'
        },
        {
          name: 'Osaka',
          description:
            'An energetic base for food, nightlife and easy connections around Kansai.',
          suitableFor: ['Food', 'Nightlife', 'Day trips'],
          image:
            'https://images.unsplash.com/photo-1590559899731-a382839e5549?w=900&q=80&auto=format'
        },
        {
          name: 'Hakone & Fuji area',
          description:
            'A useful break from the major cities for mountain scenery, hot springs and slower days.',
          suitableFor: ['Nature', 'Onsen', 'Scenery'],
          image:
            'https://images.unsplash.com/photo-1490806843957-31f4c9a91c65?w=900&q=80&auto=format'
        }
      ],
      experienceIdeas: [
        {
          id: 'japan-classic',
          icon: 'train',
          title: 'Tokyo–Kyoto–Osaka',
          description:
            'A straightforward first-time structure with city variety and manageable rail connections.',
          tripName: 'Classic Japan Journey'
        },
        {
          id: 'japan-food',
          icon: 'ramen_dining',
          title: 'Food-first Japan',
          description:
            'Give each city a culinary theme and protect time for markets, neighborhoods and reservations.',
          tripName: 'Japan Food Journey'
        },
        {
          id: 'japan-slow',
          icon: 'forest',
          title: 'Cities + slower countryside',
          description:
            'Pair major cities with an onsen town, mountain area or smaller regional stop.',
          tripName: 'Japan Cities & Countryside'
        }
      ],
      thingsToDo: [
        {
          id: 'japan-neighborhoods',
          title: 'Neighborhood day',
          category: 'City',
          description:
            'Group nearby neighborhoods together instead of crossing a large city repeatedly.',
          image:
            'https://images.unsplash.com/photo-1540959733332-eab4deabeeaf?w=700&q=80&auto=format'
        },
        {
          id: 'japan-temples',
          title: 'Temple & garden route',
          category: 'Culture',
          description:
            'Choose a compact cluster and leave room for walking rather than collecting landmarks.',
          image:
            'https://images.unsplash.com/photo-1493976040374-85c8e12f0c0e?w=700&q=80&auto=format'
        },
        {
          id: 'japan-onsen',
          title: 'Onsen stay',
          category: 'Relaxation',
          description:
            'Use a hot-spring stay as a slower overnight break between busy city segments.',
          image:
            'https://images.unsplash.com/photo-1490806843957-31f4c9a91c65?w=700&q=80&auto=format'
        }
      ],
      travelTips: [
        {
          icon: 'train',
          title: 'Plan the route before buying transport',
          content:
            'Rail passes and individual tickets are not automatically the same value for every itinerary. Finalize the cities first, then compare the transport options.'
        },
        {
          icon: 'event_seat',
          title: 'Reserve high-demand experiences',
          content:
            'Some attractions and restaurants need advance reservations. Mark those as fixed anchors and build flexible sightseeing around them.'
        }
      ]
    },
    france: {
      id: 'france',
      country: 'France',
      planningDestination: 'France',
      marketplaceQuery: 'France',
      communityQuery: 'France',
      tagline: 'Art-filled cities, regional food, coastlines and countryside',
      description:
        'Use France as a discovery page, then continue through Safarni’s real products rather than a hardcoded package: personal planning, community inspiration and verified agencies.',
      heroImage: 'assets/countries/france.webp',
      tags: ['Art', 'Food', 'Romance'],
      quickInfo: {
        bestTimeToVisit: 'April – June · September – October',
        suggestedLength: '5 – 8 days',
        currency: 'Euro (EUR)',
        language: 'French',
        weatherVibe: 'Temperate overall, with strong regional differences'
      },
      topPlaces: [
        {
          name: 'Paris',
          description:
            'Plan by neighborhood and interest: museums, architecture, food, parks and evening walks.',
          suitableFor: ['Art', 'Food', 'City life'],
          image:
            'https://images.unsplash.com/photo-1502602898657-3e91760cbb34?w=900&q=80&auto=format'
        },
        {
          name: 'Provence',
          description:
            'A slower region for villages, markets, landscapes and road-trip style travel.',
          suitableFor: ['Countryside', 'Food', 'Slow travel'],
          image:
            'https://images.unsplash.com/photo-1533104816931-20fa691ff6ca?w=900&q=80&auto=format'
        },
        {
          name: 'French Riviera',
          description:
            'Mediterranean cities and towns that work well for scenic rail or road connections.',
          suitableFor: ['Coast', 'Scenery', 'Relaxation'],
          image:
            'https://images.unsplash.com/photo-1500530855697-b586d89ba3ee?w=900&q=80&auto=format'
        },
        {
          name: 'Loire Valley',
          description:
            'A region of historic towns, châteaux and countryside that benefits from a dedicated day or overnight stay.',
          suitableFor: ['History', 'Countryside', 'Architecture'],
          image:
            'https://images.unsplash.com/photo-1563298723-dcfebaa392e3?w=900&q=80&auto=format'
        }
      ],
      experienceIdeas: [
        {
          id: 'france-paris',
          icon: 'museum',
          title: 'Paris arts & neighborhoods',
          description:
            'Build a city trip around a few major museums, neighborhood time and food rather than a landmark checklist.',
          tripName: 'Paris Arts & Neighborhoods'
        },
        {
          id: 'france-food',
          icon: 'bakery_dining',
          title: 'Food & regional France',
          description:
            'Pair a major city with one region and give local markets, cafés and regional dishes real time.',
          tripName: 'France Food & Regions Trip'
        },
        {
          id: 'france-romantic',
          icon: 'favorite',
          title: 'Romantic France',
          description:
            'Slow the itinerary down with scenic evenings, smaller towns and fewer hotel changes.',
          tripName: 'Romantic France Escape'
        }
      ],
      thingsToDo: [
        {
          id: 'france-museum',
          title: 'Museum-focused day',
          category: 'Art',
          description:
            'Choose one major museum and pair it with nearby neighborhoods instead of stacking several large museums together.',
          image:
            'https://images.unsplash.com/photo-1502602898657-3e91760cbb34?w=700&q=80&auto=format'
        },
        {
          id: 'france-market',
          title: 'Market & café morning',
          category: 'Food',
          description:
            'Leave a morning flexible for a local market, bakery stops and neighborhood wandering.',
          image:
            'https://images.unsplash.com/photo-1504674900247-0877df9cc836?w=700&q=80&auto=format'
        },
        {
          id: 'france-daytrip',
          title: 'Regional day trip',
          category: 'Explore',
          description:
            'Pick one reachable town or region and check travel time before adding it to a city itinerary.',
          image:
            'https://images.unsplash.com/photo-1533104816931-20fa691ff6ca?w=700&q=80&auto=format'
        }
      ],
      travelTips: [
        {
          icon: 'directions_subway',
          title: 'Plan by neighborhood',
          content:
            'In large cities, grouping nearby sights usually creates a better day than jumping between distant landmarks.'
        },
        {
          icon: 'restaurant',
          title: 'Leave room for meals',
          content:
            'Food can be a major part of the experience. Avoid scheduling every hour so the itinerary still works around reservations and slower meals.'
        }
      ]
    }
  };

  ngOnInit(): void {
    this.route.paramMap.subscribe((params) => {
      const id = params.get('id')?.toLowerCase() || 'maldives';
      this.destinationId = id;
      this.destination = this.destinationDatabase[id] ?? this.createFallbackDestination(id);
      this.loadLiveContent();
    });
  }

  onCreateTrip(suggestedName?: string): void {
    void this.router.navigate(['/'], {
      queryParams: {
        createTrip: '1',
        destination: this.destination.planningDestination,
        trip: suggestedName || `Trip to ${this.destination.country}`
      }
    });
  }

  onAskAi(customPrompt?: string): void {
    const query =
      customPrompt ||
      `Help me plan a trip to ${this.destination.country}. Ask me about my dates, budget, travel style and who I am traveling with before suggesting an itinerary.`;

    void this.router.navigate(['/concierge'], {
      queryParams: { q: query }
    });
  }

  retryPackages(): void {
    this.loadAgencyPackages();
  }

  retryCommunityTrips(): void {
    this.loadCommunityTrips();
  }

  packagePrice(packageItem: TravelPackage): string {
    return this.currencyService.format(packageItem.price, packageItem.currencyCode);
  }

  packageCover(packageItem: TravelPackage): string {
    return packageCoverUrl(packageItem);
  }

  packageDate(value: string): string {
    return new Date(value).toLocaleDateString('en-US', {
      month: 'short',
      day: 'numeric',
      year: 'numeric'
    });
  }

  communityTripDateRange(trip: PublicTripSearchResponse): string {
    const start = new Date(trip.startDate).toLocaleDateString('en-US', {
      month: 'short',
      day: 'numeric'
    });
    const end = new Date(trip.endDate).toLocaleDateString('en-US', {
      month: 'short',
      day: 'numeric'
    });
    return `${start} – ${end}`;
  }

  private loadLiveContent(): void {
    this.loadAgencyPackages();
    this.loadCommunityTrips();
  }

  private loadAgencyPackages(): void {
    this.isLoadingPackages = true;
    this.packageError = '';
    this.agencyPackages = [];

    this.marketplaceService
      .list({
        destination: this.destination.marketplaceQuery,
        page: 1,
        pageSize: 3
      })
      .subscribe({
        next: (result) => {
          this.agencyPackages = result.items;
          this.isLoadingPackages = false;
        },
        error: () => {
          this.packageError = 'We could not load agency packages for this destination right now.';
          this.isLoadingPackages = false;
        }
      });
  }

  private loadCommunityTrips(): void {
    this.isLoadingCommunityTrips = true;
    this.communityError = '';
    this.communityTrips = [];

    this.publicTripService
      .searchPublicTrips({
        destination: this.destination.communityQuery,
        sortOrder: 'relevance'
      })
      .subscribe({
        next: (response) => {
          this.communityTrips = (response.data ?? []).slice(0, 3);
          this.isLoadingCommunityTrips = false;
        },
        error: () => {
          this.communityError = 'We could not load community trips for this destination right now.';
          this.isLoadingCommunityTrips = false;
        }
      });
  }

  private createFallbackDestination(id: string): DestinationData {
    const country = id
      .split('-')
      .filter(Boolean)
      .map((part) => part.charAt(0).toUpperCase() + part.slice(1))
      .join(' ');

    return {
      id,
      country,
      planningDestination: country,
      marketplaceQuery: country,
      communityQuery: country,
      tagline: `Start exploring ${country} and turn inspiration into a real trip`,
      description:
        `This destination page is an inspiration layer. Build your own ${country} trip with Safarni, discover traveler-published trips, or browse real packages from verified agencies when they are available.`,
      heroImage:
        'https://images.unsplash.com/photo-1488646953014-85cb44e25828?w=1600&q=85&auto=format',
      tags: ['Explore', 'Plan', 'Discover'],
      quickInfo: {
        bestTimeToVisit: 'Check by region and season',
        suggestedLength: 'Choose based on your route',
        currency: 'Check local currency before travel',
        language: 'Check the languages used in your destinations',
        weatherVibe: 'Conditions vary by region and season'
      },
      topPlaces: [],
      experienceIdeas: [
        {
          id: `${id}-custom`,
          icon: 'auto_awesome',
          title: 'Build it your way',
          description:
            'Start with your dates, interests and budget, then let Safarni help you shape the route.',
          tripName: `Trip to ${country}`
        }
      ],
      thingsToDo: [],
      travelTips: [
        {
          icon: 'fact_check',
          title: 'Use live sources for final decisions',
          content:
            'Before booking, confirm entry rules, operating hours, transport details and other time-sensitive information with official or provider sources.'
        }
      ]
    };
  }
}
