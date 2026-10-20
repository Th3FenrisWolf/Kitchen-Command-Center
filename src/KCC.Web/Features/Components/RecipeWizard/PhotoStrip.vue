<script setup lang="ts">
  import { nextTick, ref } from 'vue'
  import Button from '~/Components/Button/Button.vue'
  import { useResourceStrings } from '~/Components/ResourceStrings'
  import PhotoViewer from './PhotoViewer.vue'
  import type { WizardPhoto } from './recipeWizardFlow'

  export interface PhotoStripProps {
    photos: WizardPhoto[]
    removable?: boolean
  }

  const { photos, removable = false } = defineProps<PhotoStripProps>()

  const emit = defineEmits<{ remove: [id: string] }>()

  const rs = useResourceStrings()

  const TILE_TEARS = 3

  const tileTear = (index: number) => `kcc-tear-tile-${(index % TILE_TEARS) + 1}`

  const viewing = ref<WizardPhoto | null>(null)
  let opener: HTMLElement | null = null

  function view(photo: WizardPhoto, event: MouseEvent) {
    opener = event.currentTarget as HTMLElement
    viewing.value = photo
  }

  async function closeViewer() {
    viewing.value = null
    await nextTick()
    opener?.focus()
  }
</script>

<template>
  <div>
    <ul class="flex flex-wrap gap-x-3 gap-y-rule-1" :aria-label="rs('PhotosTitle')">
      <li
        v-for="(photo, index) in photos"
        :key="photo.id"
        class="grid auto-rows-[calc(var(--bl)*2)] grid-rows-[calc(var(--bl)*3)] place-items-center"
      >
        <button
          type="button"
          class="kcc-torn block cursor-pointer"
          :class="tileTear(index)"
          data-testid="photo-tile"
          :aria-label="rs('ViewPhoto')"
          @click="view(photo, $event)"
        >
          <img :src="photo.url" alt="" class="block size-20 object-cover [clip-path:var(--tear)]" />
        </button>

        <Button
          v-if="removable"
          variant="ghost"
          class="kcc-btn--icon"
          data-testid="photo-remove"
          :aria-label="rs('RemovePhoto')"
          @click="emit('remove', photo.id)"
        >
          <i class="fa-duotone fa-xmark" aria-hidden="true"></i>
        </Button>
      </li>
    </ul>

    <PhotoViewer :photo="viewing" @close="closeViewer" />
  </div>
</template>
