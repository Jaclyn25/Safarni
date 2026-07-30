import { Component, inject } from '@angular/core';
import { CommonModule } from '@angular/common';
import { FormsModule } from '@angular/forms';
import { Router, RouterLink, RouterLinkActive } from '@angular/router';

export interface FeaturedDestination {
  id: string;
  country: string;
  title: string;
  description: string;
  price: number;
  image: string;
  tag?: string;
  tagType?: 'hot' | 'luxury' | 'popular' | 'budget';
  gridSpanClass?: string;
}

export interface TrendingDestination {
  id: string;
  city: string;
  country: string;
  duration: string;
  avgCost: number;
  trendReason: string;
  category: 'Beach' | 'Culture' | 'Budget' | 'Visa-friendly' | 'Weekend';
  image: string;
}

export interface AiRecommendation {
  id: string;
  title: string;
  subtitle: string;
  description: string;
  durationDays: number;
  styleTags: string[];
  estimatedBudget: number;
  image: string;
  highlights: string[];
}

@Component({
  selector: 'app-explore',
  standalone: true,
  imports: [CommonModule, FormsModule, RouterLink, RouterLinkActive],
  templateUrl: './explore.component.html'
})
export class ExploreComponent {
  private router = inject(Router);

  searchQuery: string = '';
  selectedFilter: string = 'All';

  quickPrompts: string[] = [
    'Romantic weekend in Paris',
    'Beach holiday in Maldives',
    'Best street food in Bangkok',
    'Cultural exploration in Kyoto'
  ];

  filterCategories: string[] = ['All', 'Beach', 'Culture', 'Budget', 'Visa-friendly', 'Weekend'];

  featuredDestinations: FeaturedDestination[] = [
    {
      id: 'maldives',
      country: 'Maldives',
      title: 'Tropical Paradise',
      description: 'Crystal clear waters, overwater villas & unforgettable sunsets',
      price: 1850,
      image: 'assets/countries/maldives.png',
      tag: 'Hot',
      tagType: 'hot',
      gridSpanClass: 'h-72 md:h-80'
    },
    {
      id: 'thailand',
      country: 'Thailand',
      title: 'Land of Smiles',
      description: 'Ancient temples, vibrant street food & pristine islands',
      price: 980,
      image: 'assets/countries/thailand.png',
      gridSpanClass: 'h-72 md:h-80'
    },
    {
      id: 'turkey',
      country: 'Turkey',
      title: 'Where East Meets West',
      description: 'Majestic mosques, hot air balloons & the Bosphorus',
      price: 1120,
      image: 'assets/countries/turkey.png',
      gridSpanClass: 'h-72 md:h-80'
    },
    {
      id: 'dubai',
      country: 'Dubai, UAE',
      title: 'City of Gold',
      description: 'Futuristic skyline, desert safaris & world-class luxury',
      price: 2200,
      image: 'assets/countries/dubai.png',
      tag: 'Luxury',
      tagType: 'luxury',
      gridSpanClass: 'h-64 md:h-72'
    },
    {
      id: 'japan',
      country: 'Japan',
      title: 'Ancient & Modern',
      description: 'Cherry blossoms, neon lights & centuries of tradition',
      price: 1650,
      image: 'https://images.unsplash.com/photo-1493976040374-85c8e12f0c0e?w=800&q=80&auto=format',
      gridSpanClass: 'h-64 md:h-72'
    },
    {
      id: 'france',
      country: 'France',
      title: 'City of Love',
      description: 'Eiffel Tower, Louvre Museum & French countryside charm',
      price: 1340,
      image: 'https://images.unsplash.com/photo-1502602898657-3e91760cbb34?w=800&q=80&auto=format',
      tag: 'Popular',
      tagType: 'popular',
      gridSpanClass: 'h-64 md:h-72'
    },
    {
      id: 'poland',
      country: 'Poland',
      title: "Europe's Hidden Gem",
      description: 'Medieval castles, vibrant culture & incredible value — Kraków, Warsaw & beyond',
      price: 720,
      image: 'https://images.unsplash.com/photo-1519197924294-4ba991a11128?w=1200&q=80&auto=format',
      tag: 'Budget Friendly',
      tagType: 'budget',
      gridSpanClass: 'h-64 md:h-72 md:col-span-3'
    }
  ];

  trendingDestinations: TrendingDestination[] = [
    {
      id: 'amalfi',
      city: 'Amalfi Coast',
      country: 'Italy',
      duration: '5 Days',
      avgCost: 1450,
      trendReason: 'Trending for romantic summer getaways',
      category: 'Weekend',
      image: 'https://lh3.googleusercontent.com/aida-public/AB6AXuAaWkyby5OElt9D0zwD8mQkEIF-vUIcDQjqZyKFAQCIMMDjMKgNLgbuwxareniZ1HSKiFV1Uq02IBw2mdPj8BZb0Zwf0B2qaTyttnl398vIR_L5Mync_YQ1MUaG7M6MyncUDI54Eph5tTmO66vZO7545KHCBnikbC0QNsxE46s8IvEBG9B8god24vC7RxLne2W4eD4TRNLrgIPlETJzSFXVd1HFg8WenuQq8qkNBBTaqUTBWh8vef5svQw'
    },
    {
      id: 'tokyo',
      city: 'Tokyo',
      country: 'Japan',
      duration: '7 Days',
      avgCost: 1650,
      trendReason: 'Top choice for urban culture & culinary tours',
      category: 'Culture',
      image: 'https://lh3.googleusercontent.com/aida-public/AB6AXuDECYyjvqzEQlav3dRYNAAfx1pGt8BcoWc5zwwIw-Qns-bBtlc08fM577hRJCmfIrLtUZMMz84_QpPvptGbFxoq9cTybqSYk8eAPyJPlFQvfqvRNAf66uRxgv38ISyNMjkVpUTQhHR0IQuZQRCfQb-1gM62iQuZO6KDwpcDEvwCtOrbfdbs6P3rua6GBSd7nZjCMgX7dR8hOeDJ14E6FIaF4jro_hFGHDJUsVzHGGcftpaXdmJRuin_-g'
    },
    {
      id: 'iceland',
      city: 'Blue Lagoon',
      country: 'Iceland',
      duration: '4 Days',
      avgCost: 1200,
      trendReason: 'Popular visa-friendly stopover destination',
      category: 'Visa-friendly',
      image: 'https://lh3.googleusercontent.com/aida-public/AB6AXuC8h2B3kLKVl5yCEMGPeuwdG-S3HWjeRkxEeVb386op0JODP_DtRJt7hkLCF8HEfVPft0fwBileEWcXQk1f5oMB-2UAvI62Eb2dTHoiP6fYYy4MZNT9vrCGdcAI3z5D5RqMOHM42rhajD9HAvPjKegvgl5M8xBQEIKrMACTg6bSP4NIR33KvbRzFMDydYv1cK7mvua-p0II0eqPKGq5ETwniaQqIDeAiRttbOqTs1j9iF4oLv9jg5qeNg'
    },
    {
      id: 'zermatt',
      city: 'Zermatt',
      country: 'Switzerland',
      duration: '6 Days',
      avgCost: 2100,
      trendReason: 'High demand for luxury mountain retreats',
      category: 'Beach', // Nature/Weekend
      image: 'https://lh3.googleusercontent.com/aida-public/AB6AXuChIhItiwOVpkGXc4hdCP3Vhk341Z3LTdYmPdzis3Qip8ZNVh-L4QNGyLelEtA1Cwj0nfMqVjCA-sb4McdsuZwoV8EuLHaH0lZnwsgw2u2MPK01B2VyJ3TJpPnH7IBmZl_o_snlMPQ7B1BMvsnA4CYbTZv7XIZ92RV9Ukx3TSt5bqQroPmzWZJtDvW8HDMNatKRdQ57_lGXnKGv4E84rLEs0J_vFOZvGO_8UKJgniuKJiwoA7NqfmGrDQ'
    }
  ];

  aiRecommendations: AiRecommendation[] = [
    {
      id: 'kyoto-modernism',
      title: 'Kyoto Modernism',
      subtitle: 'Top Match for You',
      description: "Discover the intersection of ancient tradition and contemporary architecture in Japan's cultural heart.",
      durationDays: 6,
      styleTags: ['Culture', 'Architecture', 'Foodie'],
      estimatedBudget: 1850,
      image: 'https://lh3.googleusercontent.com/aida-public/AB6AXuAwzJeouqB-QGW8xaF3oCU6ggXGNk6FTBcbPHTm7x8kw9EWzM2IMkrq7gELWCzQwqdRWv0szO_l7Kcys-9vgtx3DpSIR2h3C-Hqrmo47dqz59m0nD0WApCUw5P86bsb2CR_VumsRgR-7IM3Bz3gRSKc0sHqlkm1aa3g2pwVdReAeRMCE18DuxzL-YeinmCKxBHd7igjPK7ndTUu7Um_nN81MNEVoeI77gIL0Y-eMG65I19gAv1_c_SNoA',
      highlights: ['Curated Museum Pass', 'Secret Temple Access']
    },
    {
      id: 'lisbon-textures',
      title: 'Lisbon Textures',
      subtitle: 'Coastal Architecture & Dining',
      description: 'Perfect for a 4-day deep dive into tile art and coastal dining.',
      durationDays: 4,
      styleTags: ['Art & Tile', 'Coastal Dining'],
      estimatedBudget: 1240,
      image: 'https://lh3.googleusercontent.com/aida-public/AB6AXuBqU7pwfRPO5mu8-6wDyuH7u4ijRV2qyHzDcGh7spwDUVlITEk4AyEEDj5UvkC09H1RHke9zLV6s8gWdnhmm5r5Y_X45-Vbc_bWE4sQh5CPFVjmxvvAP4DPVkb2g_wO-Az29GggTTe-kCHLi0jZF7I8fyuJqAFQqSCFd4pF8QB_HS9HRAkN77ls3oX1-bbryDljr4G-ftK3k2KxJcQ6orG6uCv-1V0fi-BLn56jy_vF6BwWRfop33OJtw',
      highlights: ['Historic Tram Route', 'Seafood Tasting']
    },
    {
      id: 'santorini-escape',
      title: 'Santorini Escape',
      subtitle: 'Luxury Caldera Villas',
      description: 'A curated selection of private villas with caldera views and sunset cruises.',
      durationDays: 5,
      styleTags: ['Relaxation', 'Luxury', 'Views'],
      estimatedBudget: 2800,
      image: 'https://lh3.googleusercontent.com/aida-public/AB6AXuAMMyb6UfeIlaXccBIYhVWU2pdy2QXcTFucYI-nJSmIamPXVMBDotTZ2csOa446mC8smElnzlmkJQyQ1cMw9XMdpeL3XB-hEeWBhBQf7_O1_MsKs6egD_rXLwSxXOWmUhTFOclZIc7LI4p-jJC3PXkhJVdEK8DnbqSPTIVI8ESaDWvAGv9GikJLQUC6NMISWN5lg2OsB-F2_vuSa4szJMl5fni7Sjwr57s8gZlDtpwmLwn51YHJuY1i0A',
      highlights: ['Private Villa Pool', 'Catamaran Sunset Cruise']
    }
  ];

  get filteredTrendingDestinations(): TrendingDestination[] {
    if (this.selectedFilter === 'All') {
      return this.trendingDestinations;
    }
    return this.trendingDestinations.filter(d => d.category === this.selectedFilter);
  }

  onSearch(): void {
    const query = this.searchQuery.trim();
    if (query) {
      this.router.navigate(['/concierge'], { queryParams: { q: query } });
    } else {
      this.router.navigate(['/concierge']);
    }
  }

  onSelectPrompt(promptText: string): void {
    this.searchQuery = promptText;
    this.onSearch();
  }

  setFilter(category: string): void {
    this.selectedFilter = category;
  }

  navigateToDestination(destId: string): void {
    const slug = destId.toLowerCase().split(',')[0].trim().replace(/[^a-z0-9]/g, '');
    this.router.navigate(['/destination', slug]);
  }

  buildRecommendation(title: string): void {
    this.router.navigate(['/planner'], { queryParams: { trip: title } });
  }
}

