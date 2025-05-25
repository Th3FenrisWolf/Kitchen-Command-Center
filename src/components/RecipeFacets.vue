<script setup lang="ts">
import { ref, onMounted, onBeforeUnmount } from 'vue'
import { parseQueryString } from '~/utilities/query-functions'
import { IgnoreCase } from '~/utilities/string.extensions'

// const inputFilters = parseQueryString<RecipeSearchParams>()

const initialCalculationDone = ref(false)
const initialWindowHeight = ref(0)
const baseHeight = ref(0)

const mainHeight = ref(0)
const navHeight = ref(0)
const heroInitialHeight = ref(0)
const filterHeight = ref(0)
const footerHeight = ref(0)

const asideRef = ref<HTMLElement | null>(null)

let isResizing = false
let resizeTimer: NodeJS.Timeout | null = null
let scrollTimer: NodeJS.Timeout | null = null
let rafId: number | null = null

const filters = [
  {
    title: 'Type',
    options: ['Drinks', 'Breakfast', 'Lunch', 'Dinner', 'Dessert'],
  },
  {
    title: 'Protein',
    options: ['Beef', 'Chicken', 'Pork', 'Fish'],
  },
  {
    title: 'Dietary',
    options: ['Vegan', 'Vegetarian', 'Keto', 'Gluten-Free', 'Dairy-Free'],
  },
  {
    title: 'Cuisine',
    options: ['Italian', 'Mexican', 'Japanese', 'American', 'French'],
  },
]

const updateHeight = () => {
  // Get the main element, hero element, nav element, and footer
  const mainRef = document.querySelector('main')
  mainHeight.value = mainRef?.clientHeight ?? 0

  const navRef = document.querySelector('nav')
  navHeight.value = navRef?.clientHeight ?? 0

  const heroRef = document.getElementById('small-hero')
  heroInitialHeight.value = heroRef?.clientHeight ?? 0

  const footerRef = document.querySelector('footer')
  footerHeight.value = footerRef?.clientHeight ?? 0

  // Get the parent element's computed margins
  if (asideRef.value) {
    const parentStyle = getComputedStyle(asideRef.value)
    const parentMargins = parseFloat(parentStyle.marginBlock)

    baseHeight.value = mainHeight.value + footerHeight.value - parentMargins

    initialCalculationDone.value = true
    updateHeightOnScroll()
  }
}

const handleResizeStart = () => {
  if (isResizing) return

  isResizing = true
  initialWindowHeight.value = window.innerHeight
}

const handleResize = () => {
  if (!isResizing) handleResizeStart()

  if (resizeTimer) clearTimeout(resizeTimer)

  resizeTimer = setTimeout(() => {
    const delta = window.innerHeight - initialWindowHeight.value
    filterHeight.value += delta
    isResizing = false
  }, 250)
}

const updateHeightOnScroll = () => {
  if (!initialCalculationDone.value || !heroRef.value) return

  const heroRect = heroRef.value.getBoundingClientRect()
  const scrolledOffAmount = Math.min(Math.max(0, -heroRect.top), heroInitialHeight.value)

  // Account for nav in scroll calculation if it exists
  let navAdjustment = 0
  if (navRef.value) {
    // Check if nav is fixed position or also scrolling with content
    const navStyle = window.getComputedStyle(navRef.value)
    const navPosition = navStyle.position

    if (navPosition === 'fixed') {
      // If nav is fixed, its height is always a factor
      navAdjustment = navHeight.value
    } else {
      // If nav scrolls with content, check if it's still visible
      const navRect = navRef.value.getBoundingClientRect()
      navAdjustment = Math.max(0, navRect.bottom)
    }
  }

  // Calculate visible hero height (will be 0 when fully scrolled off)
  const visibleHeroHeight = Math.max(0, heroInitialHeight.value - scrolledOffAmount)

  // Calculate the height using base height minus visible elements
  filterHeight.value = Math.min(
    baseHeight.value - visibleHeroHeight - navAdjustment,
    window.innerHeight,
  )

  // Continue animation loop while scrolling
  rafId = requestAnimationFrame(updateHeightOnScroll)
}

const handleScroll = () => {
  if (rafId) cancelAnimationFrame(rafId)
  rafId = requestAnimationFrame(updateHeightOnScroll)

  if (scrollTimer) clearTimeout(scrollTimer)
  scrollTimer = setTimeout(() => {
    if (!rafId) return

    cancelAnimationFrame(rafId)
    rafId = null
  }, 150)
}

onMounted(() => {
  updateHeight()
  window.addEventListener('resize', handleResize)
  window.addEventListener('scroll', handleScroll)
})

onBeforeUnmount(() => {
  if (resizeTimer) clearTimeout(resizeTimer)
  if (scrollTimer) clearTimeout(scrollTimer)
  if (rafId) cancelAnimationFrame(rafId)

  window.removeEventListener('resize', handleResize)
  window.removeEventListener('scroll', handleScroll)
})
</script>

<template>
  <aside class="w-full" ref="asideRef">
    <div
      class="bg-base text-bone sticky top-4 max-h-screen overflow-y-auto rounded-3xl p-4"
      :style="{ height: filterHeight + 'px' }"
    >
      <div v-for="filter in filters" :key="filter.title">
        <h3>{{ filter.title }}</h3>
        <ul>
          <li v-for="option in filter.options" :key="option">
            {{ option }}
          </li>
        </ul>
      </div>
    </div>
  </aside>
</template>
