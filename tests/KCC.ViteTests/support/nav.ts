import type { NavModel } from '~/Types/Nav'

export const LABELS: Record<string, string> = {
  'Nav.Recipes': 'Recipes',
  'Nav.MyKitchen': 'My kitchen',
  'Nav.Menu': 'Menu',
  'Nav.SignIn': 'Sign in',
  'Nav.SignOut': 'Sign out',
  'Nav.NewRecipe': 'New recipe',
  'Nav.AskForAnAccount': 'Ask for an account',
  'Nav.Meals': 'Meals',
  'Nav.Diets': 'Diets',
  'Nav.QuickPicks': 'Quick picks',
  'Nav.UnderThirtyMinutes': 'Under 30 minutes',
  'Nav.TopRated': 'Top rated',
  'Nav.MostVariants': 'Most variants',
  'Nav.Newest': 'Newest',
  'Nav.SurpriseMe': 'Surprise me',
  'Nav.AllRecipes': 'All {0} recipes',
  'Nav.SearchPlaceholder': 'Search {0} recipes',
  'Nav.RecentlyViewed': 'Recently viewed',
  'Nav.Try': 'Try',
  'Nav.NothingMatches': 'Nothing matches “{0}” yet.',
  'Nav.AllResults': 'All {0} results',
  'Nav.SearchUnavailable': 'Search is unavailable',
  'Nav.SignedInAs': 'Signed in as',
  'Nav.MemberSince': 'Member since',
  'Nav.YourRecipesAndVariants': 'Your recipes and variants',
  'Nav.WaitingForReview': 'Waiting for review',
  'Nav.Settings': 'Settings',
  'Nav.Variants': 'Variants',
  'Nav.KitchenOf': '{0}’s kitchen',
}

export const visitor = (): NavModel => ({
  currentSection: 'recipes',
  recipeTotal: 25,
  recipes: {
    meals: [
      { label: 'Breakfast', url: '/recipes/?category=Breakfast', count: 4, icon: 'fa-duotone fa-egg' },
      { label: 'Dinner', url: '/recipes/?category=Dinner', count: 5 },
    ],
    diets: [{ label: 'Vegan', url: '/recipes/?diet=Vegan', count: 12 }],
    quickPicks: [
      { label: 'Top rated', url: '/recipes/?sort=rated', icon: 'fa-duotone fa-star' },
      { label: 'Surprise me', url: '/surprise-me', icon: 'fa-duotone fa-dice' },
    ],
  },
  suggestions: ['Dinner', 'Breakfast', 'Lunch'],
  urls: {
    home: '/',
    library: '/recipes/',
    surpriseMe: '/surprise-me',
    currentPage: '/recipes/?category=Dinner',
    signIn: '/account/login/?returnUrl=%2Frecipes%2F%3Fcategory%3DDinner',
    register: '/account/login/?mode=register',
  },
  labels: LABELS,
})

export const member = (): NavModel => ({
  ...visitor(),
  currentSection: 'kitchen',
  member: { firstName: 'Grace', memberSince: 'October 2026', kitchen: { recipes: 3, variants: 5, waiting: 1 } },
  urls: {
    home: '/',
    library: '/recipes/',
    surpriseMe: '/surprise-me',
    currentPage: '/account/',
    newRecipe: '/recipes/create-recipe/',
    account: '/account/',
    settings: '/account/settings/',
    signOut: '/account/logout?returnUrl=%2F',
  },
})
