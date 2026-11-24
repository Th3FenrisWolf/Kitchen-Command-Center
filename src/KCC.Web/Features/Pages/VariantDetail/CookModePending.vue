<script setup lang="ts">
  import { nextTick, onBeforeUnmount, ref, watch } from 'vue'
  import Button from '~/Components/Button/Button.vue'
  import { ResourceString, provideResourceStrings } from '~/Components/ResourceStrings'
  import KccSheet from '~/Components/Sheet/KccSheet.vue'
  import { useLaterPartRetry } from '~/Islands/laterParts'
  import { containFocus } from '~/Utilities/containFocus'

  // Cook mode's own props arrive here too; none of them may land on the overlay as attributes.
  defineOptions({ inheritAttrs: false })

  const props = defineProps<{ open: boolean; resourceStrings?: Record<string, string>; error?: Error }>()

  const emit = defineEmits<{ close: [] }>()

  const t = provideResourceStrings(props.resourceStrings, 'VariantDetail')
  const retry = useLaterPartRetry()

  const panel = ref<HTMLElement | null>(null)
  const closeButton = ref<InstanceType<typeof Button> | null>(null)
  const focusClose = () => (closeButton.value?.$el as HTMLElement | undefined)?.focus()

  const close = () => emit('close')

  function onKeydown(event: KeyboardEvent) {
    if (event.key === 'Escape') {
      event.preventDefault()
      close()
    } else if (event.key === 'Tab') {
      containFocus(event, panel.value)
    }
  }

  const stopListening = () => document.removeEventListener('keydown', onKeydown)

  watch(
    () => props.open,
    async (open) => {
      stopListening()
      if (!open) return

      document.addEventListener('keydown', onKeydown)
      await nextTick()
      focusClose()
    },
    { immediate: true },
  )

  onBeforeUnmount(stopListening)
</script>

<template>
  <Teleport to="body">
    <div
      v-if="open"
      class="fixed inset-0 z-50 bg-desk"
      role="dialog"
      aria-modal="true"
      :aria-label="t('CookMode')"
      :aria-busy="error ? undefined : 'true'"
    >
      <div ref="panel" tabindex="-1" class="h-full">
        <KccSheet
          wash-key="cook-mode"
          margin
          crisp
          wide
          :tear="3"
          pad="clamp(24px, 6vw, 48px)"
          class="kcc-slip--fill mx-auto max-w-3xl"
          :data-later-pending="error ? undefined : ''"
        >
          <div class="flex min-h-rule-2 items-start justify-end">
            <Button
              variant="ghost"
              size="lg"
              class="kcc-btn--icon shrink-0"
              ref="closeButton"
              data-test="cook-close"
              :aria-label="t('Close')"
              @click="close"
            >
              <i class="fa-duotone fa-xmark" aria-hidden="true"></i>
            </Button>
          </div>

          <template v-if="error">
            <ResourceString for="UnexpectedError" as="p" class="kcc-well kcc-well--danger kcc-kick" role="alert" />
            <div class="kcc-row mt-rule-1 justify-end">
              <Button data-test="later-part-retry" @click="retry">
                <ResourceString for="TryAgain" /><i class="fa-duotone fa-rotate-right" aria-hidden="true"></i>
              </Button>
            </div>
          </template>
        </KccSheet>
      </div>
    </div>
  </Teleport>
</template>
