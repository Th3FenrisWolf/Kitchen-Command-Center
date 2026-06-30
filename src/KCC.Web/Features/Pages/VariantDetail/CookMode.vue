<script setup lang="ts">
  import { computed, nextTick, onBeforeUnmount, ref, watch } from 'vue'
  import type { Ingredient, Instruction } from '~/Types/Recipe'
  import { ResourceString, provideResourceStrings } from '~/Components/ResourceStrings'
  import { useWakeLock } from './useWakeLock'
  import CookModeStep from './CookModeStep.vue'

  const props = defineProps<{
    open: boolean
    instructions: Instruction[]
    ingredients: Ingredient[]
    servings?: number
    resourceStrings?: Record<string, string>
  }>()

  const emit = defineEmits<{ close: [] }>()

  const t = provideResourceStrings(props.resourceStrings, 'VariantDetail')
  const wakeLock = useWakeLock()

  const index = ref(0)
  const checked = ref<Record<number, boolean>>({})
  const currentServings = ref(props.servings && props.servings > 0 ? props.servings : 1)
  const panel = ref<HTMLElement | null>(null)

  const total = computed(() => props.instructions.length)
  const current = computed(() => props.instructions[index.value])
  const isFirst = computed(() => index.value === 0)
  const isLast = computed(() => index.value >= total.value - 1)
  const progress = computed(() => `${t('Step')} ${index.value + 1} ${t('Of')} ${total.value}`)
  const hasScaler = computed(() => (props.servings ?? 0) > 0)

  const next = () => {
    if (!isLast.value) index.value += 1
  }
  const prev = () => {
    if (!isFirst.value) index.value -= 1
  }
  const toggleChecked = () => {
    checked.value = { ...checked.value, [index.value]: !checked.value[index.value] }
  }
  const decServings = () => {
    currentServings.value = Math.max(1, currentServings.value - 1)
  }
  const incServings = () => {
    currentServings.value += 1
  }

  const close = () => emit('close')

  const onKeydown = (event: KeyboardEvent) => {
    if (event.key === 'Escape') {
      event.preventDefault()
      close()
      return
    }
    if (event.key === 'Tab') {
      trapFocus(event)
    }
  }

  // Simple focus trap: keep Tab/Shift+Tab cycling within the panel's focusables.
  const trapFocus = (event: KeyboardEvent) => {
    const root = panel.value
    if (!root) return
    const focusables = root.querySelectorAll<HTMLElement>(
      'button:not([disabled]), [href], input, [tabindex]:not([tabindex="-1"])',
    )
    if (focusables.length === 0) return
    const first = focusables[0]!
    const last = focusables[focusables.length - 1]!
    const active = document.activeElement
    if (event.shiftKey && (active === first || active === root)) {
      event.preventDefault()
      last.focus()
    } else if (!event.shiftKey && active === last) {
      event.preventDefault()
      first.focus()
    }
  }

  const activate = async () => {
    index.value = 0
    checked.value = {}
    currentServings.value = props.servings && props.servings > 0 ? props.servings : 1
    document.addEventListener('keydown', onKeydown)
    void wakeLock.request()
    await nextTick()
    panel.value?.focus()
  }

  const deactivate = () => {
    document.removeEventListener('keydown', onKeydown)
    void wakeLock.release()
  }

  watch(
    () => props.open,
    (isOpen) => {
      if (isOpen) void activate()
      else deactivate()
    },
    { immediate: true },
  )

  onBeforeUnmount(deactivate)
</script>

<template>
  <Teleport to="body">
    <div
      v-if="open"
      class="fixed inset-0 z-50 flex flex-col bg-bone"
      role="dialog"
      aria-modal="true"
      :aria-label="t('CookMode')"
    >
      <div ref="panel" tabindex="-1" class="mx-auto flex h-full w-full max-w-3xl flex-col px-5 py-6 outline-none">
        <header class="flex items-center justify-between gap-4">
          <p class="font-bold text-onyx" aria-live="polite" data-test="cook-progress">{{ progress }}</p>
          <div v-if="hasScaler" class="flex items-center gap-2">
            <span class="text-sm font-bold text-onyx-light"><ResourceString for="Servings" /></span>
            <button
              type="button"
              data-test="cook-servings-dec"
              :aria-label="t('Fewer')"
              class="grid h-8 w-8 cursor-pointer place-items-center rounded-full border-none bg-surface-500 text-bone"
              @click="decServings"
            >
              <i class="fa-solid fa-minus text-xs" aria-hidden="true"></i>
            </button>
            <span class="min-w-[40px] text-center font-casual text-lg">{{ currentServings }}</span>
            <button
              type="button"
              data-test="cook-servings-inc"
              :aria-label="t('More')"
              class="grid h-8 w-8 cursor-pointer place-items-center rounded-full border-none bg-surface-500 text-bone"
              @click="incServings"
            >
              <i class="fa-solid fa-plus text-xs" aria-hidden="true"></i>
            </button>
          </div>
          <button
            type="button"
            data-test="cook-close"
            :aria-label="t('Close')"
            class="grid h-10 w-10 cursor-pointer place-items-center rounded-full border-none bg-surface-500 text-bone transition-colors hover:bg-surface-400"
            @click="close"
          >
            <i class="fa-solid fa-xmark" aria-hidden="true"></i>
          </button>
        </header>

        <main class="flex flex-1 items-center overflow-y-auto py-8">
          <CookModeStep
            v-if="current"
            :instruction="current"
            :step-number="current.step ?? index + 1"
            :ingredients="ingredients"
            :base-servings="servings"
            :current-servings="currentServings"
          />
        </main>

        <footer class="flex items-center justify-between gap-4">
          <button
            type="button"
            data-test="cook-prev"
            :disabled="isFirst"
            class="flex items-center gap-2 rounded-2xl bg-surface-500 px-5 py-3 font-bold text-bone transition-opacity disabled:cursor-not-allowed disabled:opacity-40"
            @click="prev"
          >
            <i class="fa-solid fa-arrow-left text-sm" aria-hidden="true"></i> <ResourceString for="Previous" />
          </button>

          <button
            type="button"
            data-test="cook-check"
            :aria-pressed="!!checked[index]"
            class="flex items-center gap-2 rounded-2xl px-5 py-3 font-bold transition-colors"
            :class="checked[index] ? 'bg-green text-onyx' : 'bg-bone-dark text-onyx'"
            @click="toggleChecked"
          >
            <i :class="checked[index] ? 'fa-solid fa-check' : 'fa-regular fa-circle'" class="text-sm" aria-hidden="true"></i>
            <ResourceString :for="checked[index] ? 'Done' : 'MarkDone'" />
          </button>

          <button
            type="button"
            data-test="cook-next"
            :disabled="isLast"
            class="flex items-center gap-2 rounded-2xl bg-surface-500 px-5 py-3 font-bold text-bone transition-opacity disabled:cursor-not-allowed disabled:opacity-40"
            @click="next"
          >
            <ResourceString for="Next" /> <i class="fa-solid fa-arrow-right text-sm" aria-hidden="true"></i>
          </button>
        </footer>
      </div>
    </div>
  </Teleport>
</template>
