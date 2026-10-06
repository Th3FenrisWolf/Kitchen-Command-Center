import { afterEach, beforeEach, describe, expect, it, vi } from 'vitest'
import { nextTick } from 'vue'
import { usePad, type CardId } from '~/Components/Header/usePad'
import { FakeElement, keydown } from '../../../support/elements'

// Both top sheets: logo, Recipes, My kitchen, the search field and New recipe on a desktop, the search glyph and Menu
// on a phone. Whichever has no box at the width under test can take no focus.
const logo = new FakeElement('logo')
const recipesMenu = new FakeElement('recipes menu')
const kitchenMenu = new FakeElement('kitchen menu')
const searchInput = new FakeElement('search field', [], { keepsKeys: true })
const newRecipe = new FakeElement('new recipe')
const desk = new FakeElement('desk row', [logo, recipesMenu, kitchenMenu, searchInput, newRecipe])

const findMenu = new FakeElement('find glyph')
const menuMenu = new FakeElement('menu')
const phone = new FakeElement('phone row', [findMenu, menuMenu])

const breakfast = new FakeElement('breakfast')
const dinner = new FakeElement('dinner')
const allRecipes = new FakeElement('all recipes')
const recipesCard = new FakeElement('recipes card', [breakfast, dinner, allRecipes])
const settings = new FakeElement('settings')
const signOut = new FakeElement('sign out')
const kitchenCard = new FakeElement('kitchen card', [settings, signOut])
const topResult = new FakeElement('top result')
const searchCard = new FakeElement('search card', [topResult])
const findInput = new FakeElement('find field', [], { keepsKeys: true })
const findCard = new FakeElement('find card', [findInput])
const signInLink = new FakeElement('sign in')
const menuCard = new FakeElement('menu card', [signInLink])
const header = new FakeElement('header', [desk, phone, recipesCard, kitchenCard, searchCard, findCard, menuCard])
const page = new FakeElement('page')

const menus: Record<CardId, FakeElement> = {
  recipes: recipesMenu,
  kitchen: kitchenMenu,
  search: searchInput,
  find: findMenu,
  menu: menuMenu,
}
const cards: Partial<Record<CardId, FakeElement>> = {
  recipes: recipesCard,
  kitchen: kitchenCard,
  search: searchCard,
  find: findCard,
  menu: menuCard,
}

const asElement = (element: FakeElement | null | undefined) => (element ?? null) as unknown as HTMLElement | null

function padOn({ phoneLayout = false } = {}) {
  for (const element of [desk, ...desk.querySelectorAll()]) {
    element.options.boxed = !phoneLayout
  }
  for (const element of [phone, ...phone.querySelectorAll()]) {
    element.options.boxed = phoneLayout
  }

  return usePad({
    header: () => asElement(header),
    row: () => asElement(phoneLayout ? phone : desk),
    menu: (id) => asElement(menus[id]),
    card: (id) => asElement(cards[id]),
    searchField: () => asElement(phoneLayout ? null : searchInput),
  })
}

const focusOut = (to: FakeElement | null) => ({ relatedTarget: to }) as unknown as FocusEvent
const clickOn = (target: FakeElement) => ({ target }) as unknown as MouseEvent

beforeEach(() => {
  FakeElement.focused = null
})

afterEach(() => {
  vi.restoreAllMocks()
})

describe('usePad states', () => {
  it('peeks a card while you reach for it, and puts it back when you stop', () => {
    const pad = padOn()

    pad.peek('recipes')
    expect(pad.stateOf('recipes')).toBe('peek')

    pad.unpeek('recipes')
    expect(pad.stateOf('recipes')).toBe('rest')
  })

  it('opens a card from its peek, and closes it again on the second press', () => {
    const pad = padOn()
    pad.peek('recipes')

    pad.toggle('recipes')
    expect(pad.stateOf('recipes')).toBe('open')
    expect(pad.peeking.value).toBeNull()

    pad.toggle('recipes')
    expect(pad.stateOf('recipes')).toBe('rest')
  })

  it('does not peek the card that is already open', () => {
    const pad = padOn()
    pad.show('recipes')

    pad.peek('recipes')

    expect(pad.stateOf('recipes')).toBe('open')
  })

  it('closes the open card when you switch, and holds the next one back a beat', () => {
    const pad = padOn()
    pad.show('recipes')
    expect(pad.swapping.value).toBe(false)

    pad.toggle('kitchen')

    expect(pad.stateOf('recipes')).toBe('rest')
    expect(pad.stateOf('kitchen')).toBe('open')
    expect(pad.swapping.value).toBe(true)

    pad.close()
    expect(pad.swapping.value).toBe(false)
  })
})

describe('usePad keyboard', () => {
  it('opens a card on ↓ and moves into its first link', async () => {
    const pad = padOn()
    const { event, prevented } = keydown('ArrowDown', recipesMenu)

    pad.onMenuKeydown('recipes', event)
    await nextTick()

    expect(prevented()).toBe(true)
    expect(pad.stateOf('recipes')).toBe('open')
    expect(FakeElement.focused).toBe(breakfast)
  })

  it('leaves ↓ with Shift, Ctrl or Meta to the browser, in the field and on a button, and opens on Alt+↓', () => {
    const pad = padOn()

    for (const [id, target] of [
      ['search', searchInput],
      ['recipes', recipesMenu],
    ] as const) {
      for (const modifier of [{ shiftKey: true }, { ctrlKey: true }, { metaKey: true }]) {
        const { event, prevented } = keydown('ArrowDown', target, modifier)

        pad.onMenuKeydown(id, event)

        expect(prevented()).toBe(false)
      }
    }
    expect(pad.open.value).toBeNull()

    pad.onMenuKeydown('recipes', keydown('ArrowDown', recipesMenu, { altKey: true }).event)
    expect(pad.open.value).toBe('recipes')
  })

  it('tabs from an open menu into its card', () => {
    const pad = padOn()
    pad.show('recipes')
    const { event, prevented } = keydown('Tab', recipesMenu)

    pad.onMenuKeydown('recipes', event)

    expect(prevented()).toBe(true)
    expect(FakeElement.focused).toBe(breakfast)
  })

  it('lets Tab move on from a closed menu', () => {
    const pad = padOn()
    const { event, prevented } = keydown('Tab', recipesMenu)

    pad.onMenuKeydown('recipes', event)

    expect(prevented()).toBe(false)
    expect(FakeElement.focused).toBeNull()
  })

  it('returns from the first link to the menu on Shift+Tab', () => {
    const pad = padOn()
    pad.show('recipes')
    const { event, prevented } = keydown('Tab', breakfast, { shiftKey: true })

    pad.onCardKeydown('recipes', event)

    expect(prevented()).toBe(true)
    expect(FakeElement.focused).toBe(recipesMenu)
    expect(pad.stateOf('recipes')).toBe('open')
  })

  it('closes the card past its last link, and carries on along the top sheet', () => {
    const pad = padOn()
    pad.show('recipes')
    const { event, prevented } = keydown('Tab', allRecipes)

    pad.onCardKeydown('recipes', event)

    expect(prevented()).toBe(true)
    expect(FakeElement.focused).toBe(kitchenMenu)
    expect(pad.open.value).toBeNull()
  })

  it('lets Tab leave the pad past the last link when its menu ends the top sheet', () => {
    const pad = padOn({ phoneLayout: true })
    pad.show('menu')
    const { event, prevented } = keydown('Tab', signInLink)

    pad.onCardKeydown('menu', event)

    expect(pad.open.value).toBeNull()
    expect(prevented()).toBe(false)
    expect(FakeElement.focused).toBeNull()
  })

  it("carries on along a phone's top sheet past the search card", () => {
    const pad = padOn({ phoneLayout: true })
    pad.show('find')
    const { event, prevented } = keydown('Tab', findInput)

    pad.onCardKeydown('find', event)

    expect(prevented()).toBe(true)
    expect(FakeElement.focused).toBe(menuMenu)
  })

  it('closes the open card on Escape and returns focus to its menu', () => {
    const pad = padOn()
    pad.show('kitchen')
    settings.focus()

    pad.onDocumentKeydown(keydown('Escape', settings).event)

    expect(pad.open.value).toBeNull()
    expect(FakeElement.focused).toBe(kitchenMenu)
  })

  it('focuses the menu before it closes the card, so a field that opens its card on focus does not reopen it', () => {
    const pad = padOn()
    pad.show('search')
    topResult.focus()
    const openAtFocus: (CardId | null)[] = []
    const focus = searchInput.focus.bind(searchInput)
    vi.spyOn(searchInput, 'focus').mockImplementation(() => {
      openAtFocus.push(pad.open.value)
      focus()
    })

    pad.onDocumentKeydown(keydown('Escape', topResult).event)

    expect(openAtFocus).toEqual(['search'])
    expect(pad.open.value).toBeNull()
    expect(FakeElement.focused).toBe(searchInput)
  })

  it('focuses the search field on /', () => {
    const pad = padOn()
    const { event, prevented } = keydown('/', page)

    pad.onDocumentKeydown(event)

    expect(prevented()).toBe(true)
    expect(FakeElement.focused).toBe(searchInput)
  })

  it('leaves / to a field, a modal dialog and a shortcut', () => {
    const pad = padOn()
    const dialog = new FakeElement('cook mode', [], { keepsKeys: true })

    for (const { event, prevented } of [
      keydown('/', searchInput),
      keydown('/', dialog),
      keydown('/', page, { metaKey: true }),
      keydown('/', page, { ctrlKey: true }),
    ]) {
      pad.onDocumentKeydown(event)
      expect(prevented()).toBe(false)
    }
    expect(FakeElement.focused).toBeNull()
  })

  it('opens the search card on a phone for /, with its field focused', async () => {
    const pad = padOn({ phoneLayout: true })

    pad.onDocumentKeydown(keydown('/', page).event)
    await nextTick()

    expect(pad.open.value).toBe('find')
    expect(FakeElement.focused).toBe(findInput)
  })
})

describe('usePad leaving', () => {
  it('closes the open card on a click outside the pad, and not on one inside it', () => {
    const pad = padOn()
    pad.show('recipes')

    pad.onDocumentClick(clickOn(dinner))
    expect(pad.open.value).toBe('recipes')

    pad.onDocumentClick(clickOn(page))
    expect(pad.open.value).toBeNull()
  })

  it('closes the open card when focus moves past it and its menu', () => {
    const pad = padOn()
    pad.show('recipes')

    pad.onFocusOut(focusOut(logo))

    expect(pad.open.value).toBeNull()
  })

  it('keeps the card open while focus stays in it, or moves to a menu that will take over', () => {
    const pad = padOn()
    pad.show('recipes')

    pad.onFocusOut(focusOut(dinner))
    pad.onFocusOut(focusOut(recipesMenu))
    pad.onFocusOut(focusOut(kitchenMenu))

    expect(pad.open.value).toBe('recipes')
  })

  it('keeps the card open when a click lands on something that takes no focus', () => {
    const pad = padOn()
    pad.show('recipes')

    pad.onFocusOut(focusOut(null))

    expect(pad.open.value).toBe('recipes')
  })
})
