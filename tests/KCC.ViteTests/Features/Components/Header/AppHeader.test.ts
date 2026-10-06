import { describe, expect, it } from 'vitest'
import AppHeader from '~/Components/Header/AppHeader.Component.vue'
import type { NavModel } from '~/Types/Nav'
import { renderSsr } from '../../../support/ssr'

const nav: NavModel = {
  recipeTotal: 25,
  recipes: { meals: [], diets: [], quickPicks: [] },
  suggestions: [],
  urls: { home: '/', surpriseMe: '/surprise-me', currentPage: '/' },
  labels: {},
}

const props = {
  homeUrl: '/',
  logoAlt: 'Kitchen Command Center',
  switchToLightLabel: 'Switch to light',
  switchToDarkLabel: 'Switch to dark',
  mainNavItems: [],
  utilityNavItems: [],
  nav,
}

describe('AppHeader', () => {
  it('renders one mark per ramp with the same alt text', async () => {
    const html = await renderSsr(AppHeader, props)

    expect(html).toMatch(/<img[^>]*data-ramp="dark"[^>]*alt="Kitchen Command Center"/)
    expect(html).toMatch(/<img[^>]*data-ramp="light"[^>]*alt="Kitchen Command Center"/)
  })

  it('links the mark to the home page', async () => {
    expect(await renderSsr(AppHeader, props)).toContain('href="/"')
  })

  it('declares the nav model as a prop, so it never falls through onto the markup', () => {
    expect(AppHeader).toHaveProperty('props.nav')
  })
})
