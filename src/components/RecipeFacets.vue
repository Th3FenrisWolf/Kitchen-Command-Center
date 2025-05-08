<script setup lang="ts">
import { ref, onMounted, onBeforeUnmount, watch } from 'vue'
import { parseQueryString } from '~/utilities/query-functions'
import { IgnoreCase } from '~/utilities/string.extensions'

// const inputFilters = parseQueryString<RecipeSearchParams>()

const height = ref(0)
const initialHeight = ref(0)
const filterSection = ref<HTMLElement | null>(null)
let resizeTimer: number | null = null
let isResizing = false
let scrollTimer: number | null = null
let rafId: number | null = null
const heroElement = ref<HTMLElement | null>(null)
const heroInitialHeight = ref(0)
const baseHeight = ref(0)
const navElement = ref<HTMLElement | null>(null)
const navHeight = ref(0)
const initialCalculationDone = ref(false)

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
  const mainEl = document.querySelector('main')
  const heroEl = document.getElementById('small-hero')
  const footerEl = document.querySelector('footer')
  const navEl = document.querySelector('nav')

  if (mainEl && heroEl && footerEl) {
    // Store the hero element and its initial height
    heroElement.value = heroEl
    heroInitialHeight.value = heroEl.clientHeight

    // Store the nav element and its height
    if (navEl) {
      navElement.value = navEl
      navHeight.value = navEl.clientHeight
    }

    // Calculate available height (main height minus hero height and nav height)
    const mainHeight = mainEl.clientHeight
    const heroHeight = heroEl.clientHeight
    const footerHeight = footerEl.clientHeight
    const navigationHeight = navHeight.value

    // Get the parent element's computed margins
    if (filterSection.value && filterSection.value.parentElement) {
      const parentStyle = window.getComputedStyle(filterSection.value.parentElement)
      const parentMargins = parseFloat(parentStyle.marginTop) + parseFloat(parentStyle.marginBottom)

      // Calculate base height (will be adjusted by scroll position)
      baseHeight.value = mainHeight - parentMargins + footerHeight

      // Initial calculation - use current hero and nav position
      // We'll call updateHeightOnScroll immediately to sync with scroll position
      initialCalculationDone.value = true
      updateHeightOnScroll()
    }
  }
}

const updateHeightWithDelta = (delta: number) => {
  height.value += delta
}

const handleResizeStart = () => {
  if (!isResizing) {
    isResizing = true
    initialHeight.value = window.innerHeight
  }
}

const handleResize = () => {
  if (!isResizing) {
    handleResizeStart()
  }

  // Clear any existing timer
  if (resizeTimer) {
    window.clearTimeout(resizeTimer)
  }

  // Set a new timer
  resizeTimer = window.setTimeout(() => {
    console.log('updating height')
    const delta = window.innerHeight - initialHeight.value
    console.log('delta', delta)
    updateHeightWithDelta(delta)
    isResizing = false
  }, 250) // Wait 250ms after the last resize event before updating
}

const updateHeightOnScroll = () => {
  if (!initialCalculationDone.value) return

  if (heroElement.value) {
    const heroRect = heroElement.value.getBoundingClientRect()
    const scrolledOffAmount = Math.min(Math.max(0, -heroRect.top), heroInitialHeight.value)

    // Account for nav in scroll calculation if it exists
    let navAdjustment = 0
    if (navElement.value) {
      // Check if nav is fixed position or also scrolling with content
      const navStyle = window.getComputedStyle(navElement.value)
      const navPosition = navStyle.position

      if (navPosition === 'fixed') {
        // If nav is fixed, its height is always a factor
        navAdjustment = navHeight.value
      } else {
        // If nav scrolls with content, check if it's still visible
        const navRect = navElement.value.getBoundingClientRect()
        navAdjustment = Math.max(0, navRect.bottom)
      }
    }

    // Calculate visible hero height (will be 0 when fully scrolled off)
    const visibleHeroHeight = Math.max(0, heroInitialHeight.value - scrolledOffAmount)

    // Calculate the height using base height minus visible elements
    height.value = Math.min(
      baseHeight.value - visibleHeroHeight - navAdjustment,
      window.innerHeight,
    )

    // Continue animation loop while scrolling
    rafId = requestAnimationFrame(updateHeightOnScroll)
  }
}

const handleScroll = () => {
  // Cancel any existing animation frame to avoid duplicates
  if (rafId !== null) {
    cancelAnimationFrame(rafId)
  }

  // Start a new animation frame loop for smooth updates
  rafId = requestAnimationFrame(updateHeightOnScroll)

  // Also set up a timeout to eventually stop the animation loop
  // after scrolling stops
  if (scrollTimer) {
    clearTimeout(scrollTimer)
  }

  scrollTimer = window.setTimeout(() => {
    if (rafId !== null) {
      cancelAnimationFrame(rafId)
      rafId = null
    }
  }, 150) // Stop checking after 150ms of no scrolling
}

onMounted(() => {
  updateHeight()
  window.addEventListener('resize', handleResize)
  window.addEventListener('scroll', handleScroll)
})

onBeforeUnmount(() => {
  if (resizeTimer) {
    window.clearTimeout(resizeTimer)
  }
  if (scrollTimer) {
    window.clearTimeout(scrollTimer)
  }
  if (rafId !== null) {
    cancelAnimationFrame(rafId)
  }
  window.removeEventListener('resize', handleResize)
  window.removeEventListener('scroll', handleScroll)
})
</script>

<template>
  <aside
    ref="filterSection"
    class="bg-base text-bone sticky top-4 max-h-screen overflow-y-auto rounded-3xl p-4"
    :style="{ height: height + 'px' }"
  >
    <div v-for="filter in filters" :key="filter.title">
      <h3>{{ filter.title }}</h3>
      <ul>
        <li v-for="option in filter.options" :key="option">
          {{ option }}
        </li>
      </ul>
    </div>
  </aside>
</template>
