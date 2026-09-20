<!-- #region CookMode Component Properties -->
<script lang="ts">
  import { computed, nextTick, onBeforeUnmount, onMounted, ref, watch } from 'vue'
  import type { Ingredient, Instruction } from '~/Types/Recipe'
  import { ResourceString, provideResourceStrings } from '~/Components/ResourceStrings'
  import KccSheet from '~/Components/Sheet/KccSheet.vue'
  import Button from '~/Components/Button/Button.vue'
  import { useWakeLock } from './useWakeLock'
  import CookModeStep from './CookModeStep.vue'

  /**
   * Full-screen step-by-step cooking overlay that holds the screen awake.
   */
  export default {
    name: 'CookMode',
  }

  export interface CookModeProps {
    open: boolean
    instructions: Instruction[]
    /**
     * Repeated in full under every step, so a cook never has to navigate back for it.
     */
    ingredients: Ingredient[]
    /**
     * Servings the stored amounts were written for. Omit or pass 0 to hide the scaler.
     */
    servings?: number
    /**
     * Localized text under the VariantDetail prefix. The overlay teleports to <body>, outside the
     * page's provider, so it re-provides them for its own subtree.
     */
    resourceStrings?: Record<string, string>
  }
</script>
<!-- #endregion -->

<script setup lang="ts">
  const props = defineProps<CookModeProps>()

  const emit = defineEmits<{ close: [] }>()

  const t = provideResourceStrings(props.resourceStrings, 'VariantDetail')
  const wakeLock = useWakeLock()

  const index = ref(0)
  const checked = ref<Record<number, boolean>>({})
  const currentServings = ref(props.servings && props.servings > 0 ? props.servings : 1)
  const panel = ref<HTMLElement | null>(null)
  // Teleport renders to <body>, which only exists client-side. Gating the Teleport on this
  // flag keeps SSR output (a comment placeholder) identical to the client's pre-mount output,
  // avoiding a hydration node mismatch that would discard the server-rendered subtree.
  const isMounted = ref(false)

  const total = computed(() => props.instructions.length)
  const current = computed(() => props.instructions[index.value])
  const isFirst = computed(() => index.value === 0)
  const isLast = computed(() => index.value >= total.value - 1)
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
    if (typeof document !== 'undefined') {
      document.addEventListener('keydown', onKeydown)
    }
    void wakeLock.request()
    await nextTick()
    panel.value?.focus()
  }

  const deactivate = () => {
    if (typeof document !== 'undefined') {
      document.removeEventListener('keydown', onKeydown)
    }
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

  onMounted(() => {
    isMounted.value = true
  })

  onBeforeUnmount(deactivate)
</script>

<template>
  <Teleport v-if="isMounted" to="body">
    <div v-if="open" class="fixed inset-0 z-50 bg-desk" role="dialog" aria-modal="true" :aria-label="t('CookMode')">
      <!-- The trap cycles the focusables of this element, so it stays a plain div wrapping the whole sheet:
           a ref on the sheet component would resolve to its instance, not to a node to query. -->
      <div ref="panel" tabindex="-1" class="h-full outline-none">
        <!-- The sheet is the viewport: height flows down the slip and the torn wrapper into the paper, and
             the step column scrolls inside the padding rather than past the tear. -->
        <KccSheet crisp :tear="3" pad="clamp(24px, 6vw, 48px)" class="kcc-slip--fill mx-auto max-w-3xl">
          <div class="flex h-full flex-col">
            <header class="flex flex-wrap items-center justify-between gap-x-6 gap-y-3">
              <!-- Interpolated rather than two <ResourceString>: a whitespace-only text node between two
                   elements is dropped by the template compiler, and the count would read "STEP 1OF 2". -->
              <p class="kcc-kick" aria-live="polite" data-test="cook-progress">
                {{ t('Step') }} <span class="kcc-num">{{ index + 1 }}</span> {{ t('Of') }}
                <span class="kcc-num">{{ total }}</span>
              </p>

              <!-- Narrow screens give the scaler a rule of its own so the count and the close button keep
                   their 48px targets side by side. -->
              <div v-if="hasScaler" class="order-last flex w-full flex-col items-center sm:order-none sm:w-auto">
                <ResourceString for="Servings" as="span" class="kcc-kick" />
                <div class="flex items-center gap-3">
                  <Button
                    variant="ghost"
                    size="lg"
                    class="kcc-btn--icon shrink-0"
                    data-test="cook-servings-dec"
                    :aria-label="t('Fewer')"
                    @click="decServings"
                  >
                    <i class="fa-duotone fa-minus" aria-hidden="true"></i>
                  </Button>
                  <span class="kcc-num min-w-10 text-center text-[22px] leading-6">{{ currentServings }}</span>
                  <Button
                    variant="ghost"
                    size="lg"
                    class="kcc-btn--icon shrink-0"
                    data-test="cook-servings-inc"
                    :aria-label="t('More')"
                    @click="incServings"
                  >
                    <i class="fa-duotone fa-plus" aria-hidden="true"></i>
                  </Button>
                </div>
              </div>

              <Button
                variant="ghost"
                size="lg"
                class="kcc-btn--icon shrink-0"
                data-test="cook-close"
                :aria-label="t('Close')"
                @click="close"
              >
                <i class="fa-duotone fa-xmark" aria-hidden="true"></i>
              </Button>
            </header>

            <main class="min-h-0 flex-1 overflow-y-auto py-6">
              <CookModeStep
                v-if="current"
                :instruction="current"
                :ingredients="ingredients"
                :base-servings="servings"
                :current-servings="currentServings"
              />
            </main>

            <footer class="flex flex-wrap items-center justify-between gap-x-6 gap-y-3">
              <Button variant="ghost" size="lg" data-test="cook-prev" :disabled="isFirst" @click="prev">
                <i class="fa-duotone fa-arrow-left" aria-hidden="true"></i><ResourceString for="Previous" />
              </Button>

              <!-- Same rule as the header: below sm the three pills do not fit on one line, so the step
                   check takes a rule of its own between the two navigation pills. -->
              <div class="order-last flex w-full justify-center sm:order-none sm:w-auto">
                <Button
                  :variant="checked[index] ? 'ink' : 'ghost'"
                  size="lg"
                  data-test="cook-check"
                  :aria-pressed="!!checked[index]"
                  @click="toggleChecked"
                >
                  <!-- Regular, not duotone, for the unticked ring: duotone fa-circle paints a filled disc,
                       which reads the same as the ticked mark. -->
                  <i :class="checked[index] ? 'fa-duotone fa-check' : 'fa-regular fa-circle'" aria-hidden="true"></i>
                  <ResourceString :for="checked[index] ? 'Done' : 'MarkDone'" />
                </Button>
              </div>

              <Button variant="ghost" size="lg" data-test="cook-next" :disabled="isLast" @click="next">
                <ResourceString for="Next" /><i class="fa-duotone fa-arrow-right" aria-hidden="true"></i>
              </Button>
            </footer>
          </div>
        </KccSheet>
      </div>
    </div>
  </Teleport>
</template>
