import type { NavMember, NavRow, NavUrls } from '~/Types/Nav'
import type { NavLabel } from './navLabels'

export function kitchenLines(member: NavMember, urls: NavUrls, t: NavLabel): NavRow[] {
  const kitchen = member.kitchen
  const lines: NavRow[] = []
  if (urls.account) {
    lines.push({
      label: t('YourRecipesAndVariants'),
      url: urls.account,
      count: kitchen ? kitchen.recipes + kitchen.variants : undefined,
      icon: 'fa-duotone fa-book-open',
    })
    if (kitchen?.waiting) {
      lines.push({
        label: t('WaitingForReview'),
        url: urls.account,
        count: kitchen.waiting,
        icon: 'fa-duotone fa-hourglass',
      })
    }
  }

  if (urls.settings) {
    lines.push({ label: t('Settings'), url: urls.settings, icon: 'fa-duotone fa-gear' })
  }

  return lines
}
