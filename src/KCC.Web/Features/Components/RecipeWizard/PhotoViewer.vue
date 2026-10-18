<script setup lang="ts">
  import { nextTick, onBeforeUnmount, onMounted, ref, watch } from 'vue'
  import Button from '~/Components/Button/Button.vue'
  import { useResourceStrings } from '~/Components/ResourceStrings'
  import KccSheet from '~/Components/Sheet/KccSheet.vue'
  import { sheetTearFor } from '~/Utilities/BrandColor'
  import type { WizardPhoto } from './recipeWizardFlow'

  export interface PhotoViewerProps {
    photo: WizardPhoto | null
  }

  const { photo } = defineProps<PhotoViewerProps>()

  const emit = defineEmits<{ close: [] }>()

  const rs = useResourceStrings()

  const panel = ref<HTMLElement | null>(null)
  // The server has no <body> to teleport to; rendering after mount keeps hydration in step.
  const isMounted = ref(false)

  const focusables = () => [
    ...(panel.value?.querySelectorAll<HTMLElement>('button:not([disabled]), [href], [tabindex]:not([tabindex="-1"])') ?? []),
  ]

  function onKeydown(event: KeyboardEvent) {
    if (event.key === 'Escape') {
      event.preventDefault()
      emit('close')
    } else if (event.key === 'Tab') {
      keepFocusInside(event)
    }
  }

  function keepFocusInside(event: KeyboardEvent) {
    const inside = focusables()
    const first = inside[0]
    const last = inside[inside.length - 1]
    if (!first || !last) return

    const active = document.activeElement
    const outside = !panel.value?.contains(active)
    if (event.shiftKey && (active === first || outside)) {
      event.preventDefault()
      last.focus()
    } else if (!event.shiftKey && (active === last || outside)) {
      event.preventDefault()
      first.focus()
    }
  }

  watch(
    () => photo,
    async (shown) => {
      document.removeEventListener('keydown', onKeydown)
      if (!shown) return

      document.addEventListener('keydown', onKeydown)
      await nextTick()
      focusables()[0]?.focus()
    },
  )

  onMounted(() => {
    isMounted.value = true
  })

  onBeforeUnmount(() => {
    document.removeEventListener('keydown', onKeydown)
  })
</script>

<template>
  <Teleport v-if="isMounted" to="body">
    <div
      v-if="photo"
      class="fixed inset-0 z-50 bg-desk"
      role="dialog"
      aria-modal="true"
      :aria-label="rs('ViewPhoto')"
      data-testid="photo-viewer"
    >
      <div ref="panel" class="h-full">
        <KccSheet
          crisp
          wide
          :tear="sheetTearFor(photo.id)"
          pad="clamp(24px, 6vw, 48px)"
          class="kcc-slip--fill mx-auto max-w-5xl"
        >
          <div class="flex h-full flex-col gap-6">
            <div class="flex justify-end">
              <Button variant="ghost" size="lg" class="kcc-btn--icon" :aria-label="rs('ClosePhoto')" @click="emit('close')">
                <i class="fa-duotone fa-xmark" aria-hidden="true"></i>
              </Button>
            </div>

            <img :src="photo.url" alt="" class="block min-h-0 w-full flex-1 object-contain" />
          </div>
        </KccSheet>
      </div>
    </div>
  </Teleport>
</template>
