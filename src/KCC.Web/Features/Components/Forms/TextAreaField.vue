<script setup lang="ts">
  import { computed, ref, onUnmounted, useAttrs, type StyleValue, type TextareaHTMLAttributes } from 'vue'
  import { draggedRules } from './draggedRules'
  import { useRaisedRules } from './useRaisedRules'

  export interface TextAreaFieldProps {
    required?: TextareaHTMLAttributes['required']
    readonly?: TextareaHTMLAttributes['readonly']
    placeholder?: TextareaHTMLAttributes['placeholder']
    error?: boolean
    rules?: number
  }

  defineOptions({
    // Attributes land on the <textarea> itself rather than being split across the wrapper — except
    // class/style, which style the field (the label) a caller sees as this component's root.
    inheritAttrs: false,
  })

  const { required, readonly, placeholder, error = false, rules: minRules = 3 } = defineProps<TextAreaFieldProps>()

  const model = defineModel<string>({
    required: true,
  })

  const attrs = useAttrs()
  const textareaAttrs = computed(() => {
    const { class: _class, style: _style, ...rest } = attrs
    return rest
  })

  const textareaRef = ref<HTMLTextAreaElement | null>(null)
  const isResizing = ref(false)
  const rules = useRaisedRules(() => minRules)
  let dragStartY = 0
  let dragStartRules = 0
  let rulePx = 0

  const startResize = (event: MouseEvent) => {
    if (!textareaRef.value) return
    isResizing.value = true
    dragStartY = event.clientY
    dragStartRules = rules.value
    rulePx = parseFloat(getComputedStyle(textareaRef.value).lineHeight)
    document.body.classList.add('cursor-ns-resize', '[&_*]:cursor-ns-resize', 'select-none')
    document.addEventListener('mousemove', onResize)
    document.addEventListener('mouseup', stopResize)
  }

  const onResize = (event: MouseEvent) => {
    if (!isResizing.value || !rulePx) return

    rules.value = draggedRules({
      startRules: dragStartRules,
      floor: minRules,
      deltaY: event.clientY - dragStartY,
      rulePx,
    })
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
  <label
    :class="['kcc-field kcc-field--area', { 'kcc-field--error': error }, 'relative w-full', attrs.class]"
    :style="[{ height: `calc(var(--bl) * ${rules})` }, attrs.style as StyleValue]"
  >
    <!-- `resize-none`: the drag handle below is this component's resize affordance, and the kit's
         `resize: vertical` default would put a second, competing grip in the same corner. -->
    <textarea
      ref="textareaRef"
      v-bind="textareaAttrs"
      v-model="model"
      :required
      :readonly
      :placeholder
      :aria-invalid="error || (textareaAttrs['aria-invalid'] as 'true' | 'false' | undefined) || undefined"
      class="h-full resize-none"
    ></textarea>
    <span class="absolute right-1 bottom-0">
      <i class="fa-duotone fa-grip-lines rotate-135 cursor-ns-resize text-ink-soft" @mousedown="startResize"></i>
    </span>
  </label>
</template>
