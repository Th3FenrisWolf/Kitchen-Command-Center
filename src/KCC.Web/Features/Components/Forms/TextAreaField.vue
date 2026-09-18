<!-- #region TextAreaField Component Properties -->
<script lang="ts">
  import { ref, onUnmounted } from 'vue'
  import type { TextareaHTMLAttributes } from 'vue'

  /**
   * Multi-line text input with a custom drag handle for vertical resizing.
   */
  export default {
    name: 'TextAreaField',
  }

  export interface TextAreaFieldProps {
    required?: TextareaHTMLAttributes['required']
    readonly?: TextareaHTMLAttributes['readonly']
    placeholder?: TextareaHTMLAttributes['placeholder']
  }
</script>
<!-- #endregion -->

<script setup lang="ts">
  const { required, readonly, placeholder } = defineProps<TextAreaFieldProps>()

  const model = defineModel<string>({
    required: true,
  })

  const textareaRef = ref<HTMLTextAreaElement | null>(null)
  const isResizing = ref(false)
  const height = ref(75)
  const delta = ref(0)

  const startResize = (event: MouseEvent) => {
    isResizing.value = true
    delta.value = event.clientY
    document.body.classList.add('cursor-ns-resize', '[&_*]:cursor-ns-resize', 'select-none')
    document.addEventListener('mousemove', onResize)
    document.addEventListener('mouseup', stopResize)
  }

  const onResize = (event: MouseEvent) => {
    if (!isResizing.value || !textareaRef.value) return

    const deltaY = event.clientY - delta.value

    height.value = Math.max(48, height.value + deltaY)

    delta.value = event.clientY
  }

  const stopResize = () => {
    isResizing.value = false
    document.body.classList.remove('cursor-ns-resize', '[&_*]:cursor-ns-resize', 'select-none')
    document.removeEventListener('mousemove', onResize)
    document.removeEventListener('mouseup', stopResize)
  }

  onUnmounted(stopResize)
</script>

<template>
  <label class="kcc-field kcc-field--area relative w-full" :style="{ height: `${height}px` }">
    <!-- `resize-none`: the drag handle below is this component's resize affordance, and the kit's
         `resize: vertical` default would put a second, competing grip in the same corner. -->
    <textarea ref="textareaRef" v-model="model" :required :readonly :placeholder class="h-full resize-none"></textarea>
    <span class="absolute right-1 bottom-0">
      <i class="fa-duotone fa-grip-lines rotate-135 cursor-ns-resize text-[15px] text-ink-soft" @mousedown="startResize"></i>
    </span>
  </label>
</template>
