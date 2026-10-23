<script setup lang="ts">
  import { nextTick, onBeforeUnmount, onMounted, ref, useId, watch } from 'vue'
  import Button from '~/Components/Button/Button.vue'
  import { ResourceString } from '~/Components/ResourceStrings'
  import KccSheet from '~/Components/Sheet/KccSheet.vue'

  export interface LeaveConfirmProps {
    open: boolean
  }

  const { open } = defineProps<LeaveConfirmProps>()

  const emit = defineEmits<{ stay: []; leave: [] }>()

  const panel = ref<HTMLElement | null>(null)
  // The server has no <body> to teleport to; rendering after mount keeps hydration in step.
  const isMounted = ref(false)
  const warningId = useId()

  const focusables = () => [...(panel.value?.querySelectorAll<HTMLElement>('button:not([disabled])') ?? [])]

  function onKeydown(event: KeyboardEvent) {
    if (event.key === 'Escape') {
      event.preventDefault()
      emit('stay')
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
    () => open,
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
      v-if="open"
      class="fixed inset-0 z-50 grid place-items-center overflow-y-auto bg-desk p-6"
      role="alertdialog"
      aria-modal="true"
      :aria-labelledby="warningId"
      data-testid="leave-dialog"
    >
      <div ref="panel" class="w-full max-w-md">
        <KccSheet wash-key="leave-confirm" crisp>
          <ResourceString :id="warningId" for="LeaveWarning" as="p" class="kcc-well kcc-well--warning kcc-kick" />

          <div class="mt-rule-1 flex flex-wrap gap-x-3 gap-y-rule-1">
            <Button size="lg" data-testid="leave-stay" @click="emit('stay')">
              <ResourceString for="LeaveStay" />
            </Button>
            <Button variant="ghost" size="lg" data-testid="leave-confirm" @click="emit('leave')">
              <ResourceString for="LeavePage" />
            </Button>
          </div>
        </KccSheet>
      </div>
    </div>
  </Teleport>
</template>
