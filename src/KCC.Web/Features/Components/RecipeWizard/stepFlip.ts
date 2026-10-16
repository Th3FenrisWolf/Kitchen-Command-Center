import { listTearFor } from '~/Utilities/BrandColor'

export function stepFlips(step: number): boolean {
  return listTearFor(step - 1) % 2 === 1
}
