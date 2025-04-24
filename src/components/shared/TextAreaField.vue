<script setup lang="ts">
import { ref, onUnmounted, type TextareaHTMLAttributes } from 'vue'
import { FontAwesomeIcon } from '@fortawesome/vue-fontawesome'
import { faGripLines } from '@fortawesome/free-solid-svg-icons'

const { required, readonly, placeholder } = defineProps<{
  required?: TextareaHTMLAttributes['required']
  readonly?: TextareaHTMLAttributes['readonly']
  placeholder?: TextareaHTMLAttributes['placeholder']
}>()

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
  <div class="relative w-full" :style="{ height: `${height}px` }">
    <textarea
      ref="textareaRef"
      v-model="model"
      :required
      :readonly
      :placeholder
      class="bg-bone-dark placeholder:text-onyx-light h-full w-full resize-none rounded-2xl px-4 py-2"
    />
    <FontAwesomeIcon
      size="xs"
      :icon="faGripLines"
      class="color-base rotate-135 absolute bottom-1 right-1 cursor-ns-resize"
      @mousedown="startResize"
    />
  </div>
</template>
