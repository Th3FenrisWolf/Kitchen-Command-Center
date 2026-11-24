import { formatRating } from '~/Components/StarRating/starDisplay'
import type { SiblingVariant } from '~/Types/Recipe'
import type { RecipeCardModel } from './recipeCardModel'

/**
 * Sibling variant → grid card. A sibling arrives without a review count, so its rating can only ever be
 * the corner stat: there is no "· N" to print beside it and no empty-rating note to fall back to. The unit
 * is the literal the variant page already prints on its stat tiles; that page carries no `Min` string.
 */
export function siblingToCard(sibling: SiblingVariant): RecipeCardModel {
  const time = `${sibling.totalTime} min`
  return {
    href: sibling.slug,
    name: sibling.name,
    seed: sibling.name,
    icon: sibling.icon,
    notch: sibling.rating > 0 ? { stat: 'rating', text: formatRating(sibling.rating) } : { stat: 'time', text: time },
    meta: [{ key: 'time', icon: 'fa-solid fa-clock', text: time }],
    tags: [],
  }
}
