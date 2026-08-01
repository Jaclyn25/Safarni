import { Component, OnInit, inject } from '@angular/core';
import { CommonModule } from '@angular/common';
import { FormsModule } from '@angular/forms';
import { ActivatedRoute, Router, RouterLink, RouterLinkActive } from '@angular/router';

export interface QuickInfo {
  bestTimeToVisit: string;
  avgBudget: string;
  suggestedLength: string;
  visaRequirement: string;
  currency: string;
  language: string;
  safetyLevel: string;
  weatherVibe: string;
}

export interface TopPlace {
  name: string;
  description: string;
  suitableFor: string[];
  image: string;
}

export interface SuggestedItinerary {
  id: string;
  title: string;
  days: number;
  estimatedCost: number;
  highlights: string[];
  badge?: string;
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
  tagline: string;
  description: string;
  heroImage: string;
  priceFrom: number;
  tags: string[];
  quickInfo: QuickInfo;
  topPlaces: TopPlace[];
  suggestedItineraries: SuggestedItinerary[];
  thingsToDo: Activity[];
  travelTips: PracticalTip[];
  vibeHighlights: string[];
}

@Component({
  selector: 'app-destination',
  standalone: true,
  imports: [CommonModule, FormsModule, RouterLink],
  templateUrl: './destination.component.html'
})
export class DestinationComponent implements OnInit {
  private route = inject(ActivatedRoute);
  private router = inject(Router);

  destinationId: string = 'maldives';
  destination!: DestinationData;

  // Form State for AI Trip Builder Panel
  builderDays: number = 7;
  builderBudget: string = 'Comfortable';
  builderStyle: string = 'Relaxed & Scenic';
  builderGroup: string = 'Couple';

  daysOptions: number[] = [3, 5, 7, 10, 14];
  budgetOptions: string[] = ['Budget', 'Comfortable', 'Luxury'];
  styleOptions: string[] = ['Relaxed & Scenic', 'Adventure & Exploration', 'Cultural & History', 'Foodie & Nightlife'];
  groupOptions: string[] = ['Solo', 'Couple', 'Family', 'Friends'];

  private destinationDatabase: Record<string, DestinationData> = {
    maldives: {
      id: 'maldives',
      country: 'Maldives',
      tagline: 'Clear water escapes, private overwater villas & slow luxury days',
      description: 'The ultimate tropical sanctuary in the Indian Ocean, famous for turquoise lagoons, coral reefs, underwater dining, and world-class overwater resorts.',
      heroImage: 'assets/countries/maldives.png',
      priceFrom: 1850,
      tags: ['Beach', 'Luxury', 'Visa on Arrival', '7 Days'],
      quickInfo: {
        bestTimeToVisit: 'Nov – Apr (Dry Season)',
        avgBudget: '$250 – $600 / day',
        suggestedLength: '5 – 7 Days',
        visaRequirement: 'Free 30-day Visa on Arrival',
        currency: 'Maldivian Rufiyaa (MVR) / USD accepted',
        language: 'Dhivehi (English widely spoken)',
        safetyLevel: 'Extremely Safe (9.8/10)',
        weatherVibe: 'Tropical Sunny 28°C–31°C'
      },
      topPlaces: [
        {
          name: 'Malé City',
          description: 'The vibrant capital with bustling fish markets, historic mosques, and colorful harbor streets.',
          suitableFor: ['Culture', 'Transit', 'Local Food'],
          image: 'https://images.unsplash.com/photo-1573843981267-be1999ff37cd?w=600&q=80&auto=format'
        },
        {
          name: 'Maafushi Island',
          description: 'Popular local island offering budget guest houses, watersports, and island-hopping tours.',
          suitableFor: ['Budget', 'Couples', 'Watersports'],
          image: 'https://images.unsplash.com/photo-1514282401047-d79a71a590e8?w=600&q=80&auto=format'
        },
        {
          name: 'Baa Atoll (UNESCO Biosphere)',
          description: 'World-famous marine reserve for swimming with manta rays and whale sharks in Hanifaru Bay.',
          suitableFor: ['Adventure', 'Diving', 'Couples'],
          image: 'https://images.unsplash.com/photo-1544551763-46a013bb70d5?w=600&q=80&auto=format'
        },
        {
          name: 'Vaadhoo Island',
          description: 'Famous for its mystical bioluminescent "Sea of Stars" glowing beach at night.',
          suitableFor: ['Romance', 'Photography'],
          image: 'https://images.unsplash.com/photo-1507525428034-b723cf961d3e?w=600&q=80&auto=format'
        }
      ],
      suggestedItineraries: [
        {
          id: 'maldives-3day',
          title: '3-Day Quick Escape',
          days: 3,
          estimatedCost: 1100,
          highlights: ['Speedboat transfer', 'Snorkeling safari', 'Sunset dolphin cruise'],
          badge: 'Weekend'
        },
        {
          id: 'maldives-5day',
          title: '5-Day Relaxed Vacation',
          days: 5,
          estimatedCost: 1650,
          highlights: ['Overwater villa stay', 'Sandbank picnic', 'Underwater dining experience'],
          badge: 'Popular'
        },
        {
          id: 'maldives-7day',
          title: '7-Day Full Paradise Experience',
          days: 7,
          estimatedCost: 2400,
          highlights: ['Resort + local island combo', 'Manta ray swim at Baa Atoll', 'Private candlelit beach dinner'],
          badge: 'Best Value'
        },
        {
          id: 'maldives-honeymoon',
          title: 'Luxury Honeymoon Special',
          days: 6,
          estimatedCost: 3200,
          highlights: ['Private villa with plunge pool', 'Floating breakfast', 'Couples spa treatment'],
          badge: 'Luxury'
        }
      ],
      thingsToDo: [
        {
          id: 'act-1',
          title: 'Snorkeling with Sea Turtles',
          category: 'Adventure',
          description: 'Explore coral gardens home to green sea turtles and vibrant reef fish.',
          image: 'https://images.unsplash.com/photo-1544551763-46a013bb70d5?w=600&q=80&auto=format'
        },
        {
          id: 'act-2',
          title: 'Sunset Dolphin Cruise',
          category: 'Romance',
          description: 'Watch wild spinner dolphins leap beside your traditional Dhoni boat at sunset.',
          image: 'https://images.unsplash.com/photo-1507525428034-b723cf961d3e?w=600&q=80&auto=format'
        },
        {
          id: 'act-3',
          title: 'Sandbank Private Picnic',
          category: 'Relaxation',
          description: 'A secluded white sandbank in the middle of the ocean reserved exclusively for you.',
          image: 'https://images.unsplash.com/photo-1514282401047-d79a71a590e8?w=600&q=80&auto=format'
        },
        {
          id: 'act-4',
          title: 'Scuba Diving at Ari Atoll',
          category: 'Diving',
          description: 'Dive deep into hammerhead channels and shipwrecks with certified master divers.',
          image: 'https://images.unsplash.com/photo-1544551763-46a013bb70d5?w=600&q=80&auto=format'
        }
      ],
      travelTips: [
        {
          icon: 'sim_card',
          title: 'SIM & Internet',
          content: 'Buy a Dhiraagu or Ooredoo tourist eSIM at Malé Airport ($35 for 17GB). Resorts have fast Wi-Fi.'
        },
        {
          icon: 'directions_boat',
          title: 'Transportation',
          content: 'Speedboats connect close islands ($25-$80). Remote atolls require seaplanes ($300-$500 return).'
        },
        {
          icon: 'payments',
          title: 'Payment & Money',
          content: 'US Dollars are accepted everywhere in resorts. Credit cards are standard. Carry small MVR for local islands.'
        },
        {
          icon: 'checkroom',
          title: 'Dress Code & Customs',
          content: 'Bikinis allowed on resort islands & designated "Bikini Beaches" on local islands. Modest dress in Malé.'
        }
      ],
      vibeHighlights: ['Best for Honeymoon', 'World Class Diving', 'Overwater Luxury', 'Crystal Clear Waters']
    },
    thailand: {
      id: 'thailand',
      country: 'Thailand',
      tagline: 'Golden temples, tropical island hopping & legendary street food',
      description: 'Experience the magic of Southeast Asia, from Bangkok’s illuminated temples and markets to Phuket and Phi Phi islands.',
      heroImage: 'assets/countries/thailand.png',
      priceFrom: 980,
      tags: ['Culture', 'Budget Friendly', 'Island Hopping', 'Foodie'],
      quickInfo: {
        bestTimeToVisit: 'Nov – Feb (Cool Dry Season)',
        avgBudget: '$60 – $180 / day',
        suggestedLength: '7 – 10 Days',
        visaRequirement: 'Visa Exempt for 60+ Countries',
        currency: 'Thai Baht (THB)',
        language: 'Thai (English in tourist spots)',
        safetyLevel: 'Safe & Welcoming (9.0/10)',
        weatherVibe: 'Warm Tropical 27°C–34°C'
      },
      topPlaces: [
        {
          name: 'Bangkok',
          description: 'Grand Palace, bustling night markets, Tuk-Tuks, and world-renowned street food stalls.',
          suitableFor: ['Culture', 'Nightlife', 'Food'],
          image: 'https://images.unsplash.com/photo-1508009603885-50cf7c579365?w=600&q=80&auto=format'
        },
        {
          name: 'Phuket & Phi Phi Islands',
          description: 'Limestone cliffs, Maya Bay turquoise waters, and vibrant beach parties.',
          suitableFor: ['Beaches', 'Parties', 'Island Hopping'],
          image: 'https://images.unsplash.com/photo-1589394815804-964ed0be2eb5?w=600&q=80&auto=format'
        },
        {
          name: 'Chiang Mai',
          description: 'Misty northern mountains, ethical elephant sanctuaries, and night bazaars.',
          suitableFor: ['Nature', 'Culture', 'Digital Nomads'],
          image: 'https://images.unsplash.com/photo-1512553353614-82a7370096dc?w=600&q=80&auto=format'
        }
      ],
      suggestedItineraries: [
        {
          id: 'thailand-7day',
          title: '7-Day Golden Triangle & Islands',
          days: 7,
          estimatedCost: 980,
          highlights: ['Bangkok temples', 'Phuket island hop', 'Elephant sanctuary visit'],
          badge: 'Popular'
        },
        {
          id: 'thailand-10day',
          title: '10-Day Complete Thailand Exploration',
          days: 10,
          estimatedCost: 1400,
          highlights: ['Bangkok + Chiang Mai + Krabi islands', 'Night market food tours'],
          badge: 'Best Experience'
        }
      ],
      thingsToDo: [
        {
          id: 'th-act-1',
          title: 'Bangkok Street Food Safari',
          category: 'Culinary',
          description: 'Taste Michelin-recognized Pad Thai and Mango Sticky Rice in Yaowarat Chinatown.',
          image: 'https://images.unsplash.com/photo-1508009603885-50cf7c579365?w=600&q=80&auto=format'
        },
        {
          id: 'th-act-2',
          title: 'Island Hopping in Maya Bay',
          category: 'Adventure',
          description: 'Sail through iconic limestone karsts featured in "The Beach".',
          image: 'https://images.unsplash.com/photo-1589394815804-964ed0be2eb5?w=600&q=80&auto=format'
        }
      ],
      travelTips: [
        {
          icon: 'sim_card',
          title: 'SIM & Internet',
          content: 'AIS or TrueMove tourist SIM at Bangkok Airport provides unlimited 5G data for $15.'
        },
        {
          icon: 'payments',
          title: 'Cash is King',
          content: 'Keep Thai Baht cash for street markets and Tuk-Tuks. ATMs charge 220 THB withdrawal fee.'
        }
      ],
      vibeHighlights: ['Incredible Street Food', 'Friendly Hospitality', 'Affordable Luxury', 'Island Hopping']
    }
  };

  ngOnInit(): void {
    this.route.paramMap.subscribe(params => {
      const id = params.get('id')?.toLowerCase() || 'maldives';
      this.destinationId = id;
      this.loadDestination(id);
    });
  }

  private loadDestination(id: string): void {
    if (this.destinationDatabase[id]) {
      this.destination = this.destinationDatabase[id];
    } else {
      // Dynamic fallback generator for any country ID
      const formattedName = id.charAt(0).toUpperCase() + id.slice(1);
      this.destination = {
        id: id,
        country: formattedName,
        tagline: `Discover the breathtaking sights and rich culture of ${formattedName}`,
        description: `Plan an unforgettable journey to ${formattedName} with custom AI itineraries tailored to your dates, preferences, and travel style.`,
        heroImage: 'https://images.unsplash.com/photo-1488646953014-85cb44e25828?w=1200&q=80&auto=format',
        priceFrom: 1200,
        tags: ['Popular', 'Culture', 'Sightseeing', 'AI Recommended'],
        quickInfo: {
          bestTimeToVisit: 'Spring & Autumn',
          avgBudget: '$120 – $300 / day',
          suggestedLength: '5 – 7 Days',
          visaRequirement: 'Check visa guidelines',
          currency: 'Local Currency / USD',
          language: 'Local Language / English',
          safetyLevel: 'High Safety Rating (9.2/10)',
          weatherVibe: 'Pleasant & Seasonal'
        },
        topPlaces: [
          {
            name: `${formattedName} Capital & Historic Center`,
            description: `Explore the vibrant heart of ${formattedName} with historical architecture and dining.`,
            suitableFor: ['Culture', 'History', 'City Tour'],
            image: 'https://images.unsplash.com/photo-1499856871958-5b9627545d1a?w=600&q=80&auto=format'
          },
          {
            name: `${formattedName} Scenic Coast & Countryside`,
            description: `Unwind in beautiful natural landscapes and iconic views.`,
            suitableFor: ['Relaxation', 'Nature', 'Photography'],
            image: 'https://images.unsplash.com/photo-1507525428034-b723cf961d3e?w=600&q=80&auto=format'
          }
        ],
        suggestedItineraries: [
          {
            id: `${id}-5day`,
            title: `5-Day Best of ${formattedName}`,
            days: 5,
            estimatedCost: 1250,
            highlights: ['City tour', 'Local gastronomy experience', 'Guided landmark trip'],
            badge: 'Recommended'
          },
          {
            id: `${id}-7day`,
            title: `7-Day Grand ${formattedName} Expedition`,
            days: 7,
            estimatedCost: 1750,
            highlights: ['Full region exploration', 'Private transfer options', 'Hidden gem discovery'],
            badge: 'Best Value'
          }
        ],
        thingsToDo: [
          {
            id: `${id}-act-1`,
            title: `Guided Heritage Walk in ${formattedName}`,
            category: 'Culture',
            description: 'Discover centuries of history with local expert guides.',
            image: 'https://images.unsplash.com/photo-1499856871958-5b9627545d1a?w=600&q=80&auto=format'
          },
          {
            id: `${id}-act-2`,
            title: `Sunset Culinary & Tasting Tour`,
            category: 'Foodie',
            description: 'Sample local specialties and traditional hospitality.',
            image: 'https://images.unsplash.com/photo-1504674900247-0877df9cc836?w=600&q=80&auto=format'
          }
        ],
        travelTips: [
          {
            icon: 'sim_card',
            title: 'SIM & Connectivity',
            content: 'Get an eSIM before arrival or purchase a local SIM at the arrival terminal.'
          },
          {
            icon: 'payments',
            title: 'Currency & Payment',
            content: 'Major credit cards accepted in cities; carry local cash for small shops.'
          }
        ],
        vibeHighlights: ['Scenic Views', 'Rich Culture', 'Great Local Cuisine', 'Safe for Travelers']
      };
    }
  }

  onCreateTrip(itineraryTitle?: string, days?: number): void {
    const title = itineraryTitle || `Trip to ${this.destination.country}`;
    const countryAliases: Record<string, string> = {
      turkey: 'Türkiye',
      'dubai, uae': 'United Arab Emirates',
      uae: 'United Arab Emirates'
    };
    const destination =
      countryAliases[this.destination.country.toLocaleLowerCase()] ??
      this.destination.country;

    void this.router.navigate(['/'], {
      queryParams: {
        createTrip: '1',
        destination,
        trip: title,
        days: days || this.builderDays
      }
    });
  }

  onAskAi(customPrompt?: string): void {
    const query = customPrompt || `Plan a customized ${this.builderDays}-day trip to ${this.destination.country} for a ${this.builderGroup} with a ${this.builderBudget} budget.`;
    this.router.navigate(['/concierge'], {
      queryParams: { q: query }
    });
  }

  onGenerateCustomTrip(): void {
    const query = `Create a ${this.builderDays}-day ${this.builderStyle} itinerary for ${this.destination.country} (${this.builderGroup}, ${this.builderBudget} budget).`;
    this.router.navigate(['/planner'], {
      queryParams: {
        destination: this.destination.country,
        days: this.builderDays,
        budget: this.builderBudget,
        style: this.builderStyle,
        group: this.builderGroup,
        trip: `${this.builderDays}-Day ${this.destination.country} ${this.builderStyle}`
      }
    });
  }
}
