<script setup lang="ts">
  import { computed, nextTick, onBeforeUnmount, onMounted, reactive, ref, watch } from 'vue'
  import logoOnDark from '~/Components/Header/Assets/logo-on-dark.webp'
  import logoOnLight from '~/Components/Header/Assets/logo-on-light.webp'
  import type { NavModel } from '~/Types/Nav'
  import KccWashes from '~/Components/Sheet/KccWashes.Component.vue'
  import KitchenCard from './KitchenCard.vue'
  import MenuCard from './MenuCard.vue'
  import PadCard from './PadCard.vue'
  import RecipesCard from './RecipesCard.vue'
  import SearchCard from './SearchCard.vue'
  import SearchForm from './SearchForm.vue'
  import { navLabels } from './navLabels'
  import { usePad, type CardId } from './usePad'
  import { useNavSearch } from './useNavSearch'
  import { usePressToPeek } from './usePressToPeek'
  import { useRecentlyViewed } from './useRecentlyViewed'
  import { useTuckOnScroll } from './useTuckOnScroll'

  export interface AppHeaderProps {
    logoAlt: string
    nav: NavModel
  }

  const { logoAlt, nav } = defineProps<AppHeaderProps>()

  const t = computed(() => navLabels(nav.labels))
  const searchLabel = computed(() => t.value('SearchPlaceholder', nav.recipeTotal))
  const waiting = computed(() => nav.member?.kitchen?.waiting ?? 0)

  const header = ref<HTMLElement>()
  const byId = (id: string) => header.value?.querySelector<HTMLElement>(`#${id}`) ?? null
  const shown = (element: HTMLElement | null) => (element && element.getClientRects().length > 0 ? element : null)

  const pad = usePad({
    header: () => header.value ?? null,
    row: () => shown(byId('pad-bar-desk')) ?? byId('pad-bar-phone'),
    menu: (id) => byId(id === 'search' ? 'pad-search-input' : `pad-menu-${id}`),
    card: (id) => byId(`pad-card-${id}`),
    searchField: () => shown(byId('pad-search-input')),
  })
  const { open, swapping, stateOf } = pad

  const tuck = useTuckOnScroll(pad.close)
  const { tucked } = tuck

  const { query, results, total, failed, searched } = useNavSearch()
  const { recent, refresh: refreshRecent } = useRecentlyViewed()

  // Each desktop card hangs from the control that opens it, kept 8px inside the pad: the clip box runs 40px wider.
  const left = reactive<Partial<Record<CardId, number>>>({})
  const CLAMP = 48

  function place() {
    const box = byId('pad-cards')
    if (!box || !shown(byId('pad-bar-desk'))) {
      return
    }

    const bounds = box.getBoundingClientRect()
    const hang = (id: CardId, from: HTMLElement | null | undefined, x: (anchor: DOMRect, width: number) => number) => {
      const card = byId(`pad-card-${id}`)
      if (card && from) {
        const width = card.offsetWidth
        left[id] = Math.max(CLAMP, Math.min(x(from.getBoundingClientRect(), width), bounds.width - CLAMP - width))
      }
    }

    hang('recipes', byId('pad-menu-recipes'), (menu) => menu.left - bounds.left - 16)
    hang('kitchen', byId('pad-menu-kitchen'), (menu) => menu.left - bounds.left - 16)
    hang(
      'search',
      byId('pad-search-input')?.closest<HTMLElement>('.pad-search-field'),
      (field, width) => field.right - bounds.left + 24 - width,
    )
  }

  const peeks = {
    peek: (id: CardId) => {
      place()
      pad.peek(id)
    },
    unpeek: pad.unpeek,
  }
  const press = usePressToPeek(peeks)

  async function focusFind() {
    await nextTick()
    byId('pad-find-input')?.focus({ preventScroll: true })
  }

  function choose(id: CardId) {
    place()
    pad.toggle(id)
    if (id === 'find' && open.value === 'find') {
      void focusFind()
    }
  }

  function reachSearch() {
    if (open.value !== 'search') {
      place()
      pad.show('search')
    }
  }

  function suggest(phrase: string) {
    query.value = phrase
    ;(shown(byId('pad-search-input')) ?? byId('pad-find-input'))?.focus({ preventScroll: true })
  }

  const menuEvents = (id: CardId) => ({
    click: () => choose(id),
    keydown: (event: KeyboardEvent) => pad.onMenuKeydown(id, event),
    focus: (event: FocusEvent) => {
      if ((event.target as HTMLElement).matches(':focus-visible')) {
        peeks.peek(id)
      }
    },
    blur: () => pad.unpeek(id),
    pointerenter: (event: PointerEvent) => press.enter(id, event),
    pointerleave: (event: PointerEvent) => press.leave(id, event),
    pointerdown: (event: PointerEvent) => press.down(id, event),
    pointermove: press.move,
    pointerup: press.up,
    pointercancel: press.cancel,
    contextmenu: press.cancel,
  })

  watch(open, (id) => {
    if (id === 'search' || id === 'find') {
      refreshRecent()
    }
  })

  let resizes: ResizeObserver | undefined

  onMounted(() => {
    tuck.start()
    resizes = new ResizeObserver(() => {
      place()
      tuck.measure(header.value?.offsetHeight ?? 0)
    })
    if (header.value) {
      resizes.observe(header.value)
    }
    window.addEventListener('scroll', tuck.onScroll, { passive: true })
    document.addEventListener('click', pad.onDocumentClick)
    document.addEventListener('keydown', pad.onDocumentKeydown)
  })

  onBeforeUnmount(() => {
    resizes?.disconnect()
    window.removeEventListener('scroll', tuck.onScroll)
    document.removeEventListener('click', pad.onDocumentClick)
    document.removeEventListener('keydown', pad.onDocumentKeydown)
  })
</script>

<template>
  <header
    ref="header"
    class="pad-header content-grid"
    :class="{ 'is-tucked': tucked }"
    @focusin="tuck.show"
    @focusout="pad.onFocusOut"
  >
    <div class="pad breakout" :class="{ 'is-swapping': swapping }">
      <div class="pad-top kcc-slip kcc-tear-hero" style="--r: -0.12">
        <div class="kcc-torn">
          <div class="kcc-sheet" style="--pad: 12px">
            <KccWashes wash-key="@pad" size="page" :wash-slots="[0, 4, 8, 12]" />
            <div id="pad-bar-desk" class="pad-bar pad-bar--desk">
              <a class="pad-logo" :href="nav.urls.home">
                <img data-ramp="light" :src="logoOnLight" :alt="logoAlt" width="68" height="48" loading="eager" />
                <img data-ramp="dark" :src="logoOnDark" :alt="logoAlt" width="68" height="48" loading="eager" />
              </a>

              <nav class="pad-menus">
                <button
                  id="pad-menu-recipes"
                  type="button"
                  class="pad-menu kcc-control"
                  :class="{ 'kcc-pill': open === 'recipes' }"
                  :aria-expanded="open === 'recipes'"
                  aria-controls="pad-card-recipes"
                  :aria-current="nav.currentSection === 'recipes' ? 'true' : undefined"
                  v-on="menuEvents('recipes')"
                >
                  <span class="pad-menu-label">{{ t('Recipes') }}</span>
                  <i class="fa-solid fa-chevron-down" aria-hidden="true"></i>
                </button>
                <button
                  v-if="nav.member"
                  id="pad-menu-kitchen"
                  type="button"
                  class="pad-menu kcc-control"
                  :class="{ 'kcc-pill': open === 'kitchen' }"
                  :aria-expanded="open === 'kitchen'"
                  aria-controls="pad-card-kitchen"
                  :aria-current="nav.currentSection === 'kitchen' ? 'true' : undefined"
                  v-on="menuEvents('kitchen')"
                >
                  <span class="pad-menu-label">{{ t('MyKitchen') }}</span>
                  <span v-if="waiting" class="pad-count kcc-num"
                    >{{ waiting }}<span class="sr-only">&nbsp;{{ t('WaitingForReview') }}</span></span
                  >
                  <i class="fa-solid fa-chevron-down" aria-hidden="true"></i>
                </button>
              </nav>

              <span class="pad-grow"></span>

              <SearchForm
                v-if="nav.urls.library"
                v-model="query"
                input-id="pad-search-input"
                :action="nav.urls.library"
                :label="searchLabel"
                controls="pad-card-search"
                key-hint
                @reach="reachSearch"
                @keydown="(event: KeyboardEvent) => pad.onMenuKeydown('search', event)"
                @pointerenter="(event: PointerEvent) => press.enter('search', event)"
                @pointerleave="(event: PointerEvent) => press.leave('search', event)"
              />

              <a
                v-if="nav.urls.newRecipe"
                class="kcc-btn max-[820px]:w-(--control-md) max-[820px]:px-0"
                :href="nav.urls.newRecipe"
                :title="t('NewRecipe')"
              >
                <i class="fa-duotone fa-plus" aria-hidden="true"></i
                ><span class="max-[820px]:sr-only">{{ t('NewRecipe') }}</span>
              </a>
              <a v-else-if="nav.urls.signIn" class="kcc-btn kcc-btn--ghost" :href="nav.urls.signIn">
                <i class="fa-duotone fa-right-to-bracket" aria-hidden="true"></i>{{ t('SignIn') }}
              </a>
            </div>

            <div id="pad-bar-phone" class="pad-bar pad-bar--phone">
              <a class="pad-logo" :href="nav.urls.home">
                <img data-ramp="light" :src="logoOnLight" :alt="logoAlt" width="56" height="40" loading="eager" />
                <img data-ramp="dark" :src="logoOnDark" :alt="logoAlt" width="56" height="40" loading="eager" />
              </a>

              <span class="pad-grow"></span>

              <nav class="pad-menus">
                <button
                  v-if="nav.urls.library"
                  id="pad-menu-find"
                  type="button"
                  class="pad-glyph kcc-control"
                  :class="{ 'kcc-pill': open === 'find' }"
                  :aria-label="searchLabel"
                  :aria-expanded="open === 'find'"
                  aria-controls="pad-card-find"
                  v-on="menuEvents('find')"
                >
                  <i class="fa-duotone fa-magnifying-glass" aria-hidden="true"></i>
                </button>
                <button
                  id="pad-menu-menu"
                  type="button"
                  class="pad-menu kcc-control"
                  :class="{ 'kcc-pill': open === 'menu' }"
                  :aria-expanded="open === 'menu'"
                  aria-controls="pad-card-menu"
                  v-on="menuEvents('menu')"
                >
                  <i class="fa-solid fa-bars" aria-hidden="true"></i>
                  <span class="pad-menu-label">{{ t('Menu') }}</span>
                  <span v-if="waiting" class="pad-count kcc-num"
                    >{{ waiting }}<span class="sr-only">&nbsp;{{ t('WaitingForReview') }}</span></span
                  >
                </button>
              </nav>
            </div>
          </div>
        </div>
      </div>

      <div class="pad-page kcc-slip kcc-slip--fill kcc-slip--wide kcc-tear-hero-2" aria-hidden="true">
        <div class="kcc-torn"><div class="kcc-sheet"></div></div>
      </div>

      <div id="pad-cards" class="pad-cards">
        <PadCard
          id="pad-card-recipes"
          :wash-slots="[2, 10]"
          class="pad-card--desk"
          :state="stateOf('recipes')"
          :tear="4"
          :tilt="-0.35"
          width="min(640px, calc(100% - 140px))"
          :left="left.recipes"
          wash="peach"
          labelled-by="pad-menu-recipes"
          @keydown="(event: KeyboardEvent) => pad.onCardKeydown('recipes', event)"
        >
          <RecipesCard
            :recipes="nav.recipes"
            :total="nav.recipeTotal"
            :library="nav.urls.library"
            :current-page="nav.urls.currentPage"
            :t
          />
        </PadCard>

        <PadCard
          v-if="nav.member"
          id="pad-card-kitchen"
          :wash-slots="[6, 14]"
          class="pad-card--desk"
          :state="stateOf('kitchen')"
          :tear="3"
          :tilt="0.3"
          width="min(580px, calc(100% - 140px))"
          :left="left.kitchen"
          wash="lavender"
          labelled-by="pad-menu-kitchen"
          @keydown="(event: KeyboardEvent) => pad.onCardKeydown('kitchen', event)"
        >
          <KitchenCard :member="nav.member" :urls="nav.urls" :t />
        </PadCard>

        <template v-if="nav.urls.library">
          <PadCard
            id="pad-card-search"
            :wash-slots="[3, 11]"
            class="pad-card--desk pad-card--mirror"
            :state="stateOf('search')"
            :tear="1"
            :tilt="0.25"
            width="440px"
            :left="left.search"
            wash="sky"
            :label="searchLabel"
            @keydown="(event: KeyboardEvent) => pad.onCardKeydown('search', event)"
          >
            <SearchCard
              :query
              :searched
              :results
              :total
              :failed
              :recent
              :suggestions="nav.suggestions"
              :library="nav.urls.library"
              :recipe-total="nav.recipeTotal"
              :t
              @suggest="suggest"
            />
          </PadCard>

          <PadCard
            id="pad-card-find"
            :wash-slots="[5, 13]"
            class="pad-card--phone"
            scroll
            :state="stateOf('find')"
            :tear="1"
            :tilt="0.25"
            labelled-by="pad-menu-find"
            @keydown="(event: KeyboardEvent) => pad.onCardKeydown('find', event)"
          >
            <div class="min-h-rule-2" data-row style="--i: 0">
              <SearchForm v-model="query" input-id="pad-find-input" :action="nav.urls.library" :label="searchLabel" />
            </div>
            <div data-row style="--i: 1">
              <SearchCard
                :query
                :searched
                :results
                :total
                :failed
                :recent
                :suggestions="nav.suggestions"
                :library="nav.urls.library"
                :recipe-total="nav.recipeTotal"
                :t
                @suggest="suggest"
              />
            </div>
          </PadCard>
        </template>

        <PadCard
          id="pad-card-menu"
          :wash-slots="[1, 9]"
          class="pad-card--phone"
          scroll
          :state="stateOf('menu')"
          :tear="4"
          :tilt="-0.3"
          labelled-by="pad-menu-menu"
          @keydown="(event: KeyboardEvent) => pad.onCardKeydown('menu', event)"
        >
          <MenuCard :recipes="nav.recipes" :urls="nav.urls" :member="nav.member" :t />
        </PadCard>
      </div>
    </div>
  </header>
</template>

<style>
  /* So a focused element, or the target of a link, never comes to rest under the pad. */
  :root {
    scroll-padding-top: var(--pad-offset, 0px);
  }

  .pad-header {
    overflow-x: clip;
    padding: 16px 0 14px;
    position: sticky;
    top: 0;
    transition: transform 300ms cubic-bezier(0.2, 0.8, 0.2, 1);
    z-index: 30;
  }

  .pad-header.is-tucked {
    transform: translateY(calc(-100% - 28px));
  }

  .pad {
    position: relative;
  }

  .pad-top {
    z-index: 5;
  }

  /* The controls shift up onto the bar's first line, and the hero tear bites up to 8px into the top edge, so the
     bar starts a whole rule down or the tear would cut their focus rings. */
  .pad-top > .kcc-torn > .kcc-sheet {
    padding-top: var(--bl);
  }

  .pad-page {
    inset: 0;
    pointer-events: none;
    position: absolute;
    transform: rotate(calc(-0.12deg + var(--blank-turn))) scale(var(--blank-scale));
    z-index: 3;
  }

  .pad-page .kcc-sheet {
    background-image: none;
  }

  /* The cards hang between the top sheet and the page under it. The box starts inside the top sheet, so its straight
     edge never shows, and runs past the pad's sides so a fanned card keeps its shadow. */
  .pad-cards {
    height: calc(100dvh - 96px);
    left: -40px;
    overflow: hidden;
    pointer-events: none;
    position: absolute;
    right: -40px;
    top: calc(100% - 14px);
    z-index: 4;
  }

  .pad-bar {
    align-items: flex-start;
    display: flex;
    gap: 18px;
    min-height: 48px;
    padding: 0 6px;
  }

  /* The hero tear cuts its top-right corner away diagonally, and the menu's small radius brings its ring's corner
     into that cut unless it ends a little further in. */
  .pad-bar--phone {
    display: none;
    padding-inline-end: 12px;
  }

  .pad-grow {
    flex: 1 1 auto;
  }

  .pad-logo {
    display: inline-flex;
    flex: none;
  }

  .pad-logo img {
    display: block;
    height: 48px;
    width: auto;
  }

  .pad-bar--phone .pad-logo img {
    height: 40px;
  }

  .pad-menus {
    align-items: flex-start;
    display: flex;
    gap: 4px;
  }

  /* The kit centres a control on a Sono line; a menu's label is Hazelnut at the body size, so the Sono size
     that shares its centre stands in for it. */
  .pad-menu,
  .pad-glyph {
    --centre-size: calc(var(--k-hazelnut) / var(--k-sono) * var(--text-body));
  }

  .pad-menu {
    align-items: center;
    border-radius: var(--rd);
    cursor: pointer;
    display: inline-flex;
    font-family: var(--font-hazelnut);
    font-size: var(--text-body);
    gap: 8px;
    line-height: var(--bl);
    min-height: var(--control-md);
    padding: 0 12px;
    transition:
      background-color 300ms,
      color 300ms,
      outline-color 0.2s,
      outline-offset 0.2s;
    white-space: nowrap;
  }

  .pad-menu:not(.kcc-pill) {
    color: var(--color-ink);
  }

  .pad-menu .fa-chevron-down {
    transition: transform 300ms;
  }

  .pad-menu[aria-expanded='true'] .fa-chevron-down {
    transform: rotate(180deg);
  }

  .pad-menu[aria-current='true']:not(.kcc-pill) .pad-menu-label {
    background-image: linear-gradient(
      to top,
      color-mix(in oklab, var(--color-marker) 90%, transparent) 0 6px,
      transparent 6px
    );
    background-position: 0 1px;
    background-repeat: no-repeat;
    margin: 0 -2px;
    padding: 0 2px;
  }

  .pad-count {
    background: var(--color-marker);
    border-radius: 999px;
    color: var(--color-marker-ink);
    display: inline-grid;
    font-family: var(--font-sono);
    font-size: var(--text-caps);
    height: var(--pill-sm);
    line-height: var(--pill-sm);
    min-width: var(--pill-sm);
    padding: 0 7px;
    place-items: center;
  }

  .pad-menu.kcc-pill .pad-count {
    box-shadow: inset 0 0 0 1px var(--color-marker-ink);
  }

  .pad-glyph {
    --h: var(--control-sm);
    border-radius: 999px;
    cursor: pointer;
    display: grid;
    flex: none;
    font-size: var(--icon-sm);
    height: var(--control-sm);
    place-items: center;
    transition:
      background-color 300ms,
      color 300ms,
      outline-color 0.2s,
      outline-offset 0.2s;
    width: var(--control-sm);
  }

  .pad-glyph:not(.kcc-pill) {
    color: var(--color-ink);
  }

  .pad-bar--desk .pad-search {
    min-width: 0;
  }

  .pad-bar--desk .pad-search-field {
    max-width: 100%;
    transition: width 300ms cubic-bezier(0.2, 0.8, 0.2, 1);
    width: 250px;
  }

  .pad-bar--desk .pad-search-field:focus-within {
    width: 330px;
  }

  @media (min-width: 768px) and (max-width: 1023.98px) {
    .pad-bar--desk .pad-search-field {
      grid-template-columns: auto 1fr;
      width: 200px;
    }

    .pad-bar--desk .pad-search-field:focus-within {
      width: 240px;
    }

    .pad-bar--desk .pad-key {
      display: none;
    }
  }

  @media (max-width: 767.98px) {
    .pad-bar--desk {
      display: none;
    }

    .pad-bar--phone {
      display: flex;
    }
  }
</style>
