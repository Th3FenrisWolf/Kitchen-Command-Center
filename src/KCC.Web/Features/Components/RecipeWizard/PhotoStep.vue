<script setup lang="ts">
  import { computed, nextTick, ref, type ComponentPublicInstance } from 'vue'
  import Button from '~/Components/Button/Button.vue'
  import { ResourceString, useResourceStrings } from '~/Components/ResourceStrings'
  import KccSheet, { type Tear } from '~/Components/Sheet/KccSheet.vue'
  import CameraView from './CameraView.vue'
  import PhotoStrip from './PhotoStrip.vue'
  import ReadAgainWarning from './ReadAgainWarning.vue'
  import { MAX_PHOTOS, type PhotoErrorKey, type PhotoSource, type WizardPhoto } from './recipeWizardFlow'
  import { focusHeading, STEP_PAD } from './stepSheet'
  import type { ReadErrorKey } from './wizardErrors'
  import { fill, longDay } from './wizardText'

  export interface PhotoStepProps {
    photos: WizardPhoto[]
    photoErrors?: PhotoErrorKey[]
    readError?: ReadErrorKey | null
    noRecipeFound?: boolean
    availableAgainOn?: string | null
    canGoBack?: boolean
    canRead?: boolean
    returnsToReview?: boolean
    confirmingRead?: boolean
    tear?: Tear
  }

  const {
    photos,
    photoErrors = [],
    readError = null,
    noRecipeFound = false,
    availableAgainOn = null,
    canGoBack = false,
    canRead = false,
    returnsToReview = false,
    confirmingRead = false,
    tear,
  } = defineProps<PhotoStepProps>()

  const emit = defineEmits<{
    add: [photos: Promise<Blob | null>[], source: PhotoSource]
    remove: [id: string]
    read: []
    typing: []
    back: []
    review: []
    confirmRead: []
    keepEdits: []
  }>()

  const rs = useResourceStrings()

  const fileInput = ref<HTMLInputElement | null>(null)
  const readButton = ref<ComponentPublicInstance | null>(null)
  const heading = ref<HTMLElement | null>(null)

  const count = computed(() => fill(rs('PhotoCount'), String(photos.length), String(MAX_PHOTOS)))
  const photoMessages = computed(() => photoErrors.map((key) => ({ key, text: fill(rs(key), String(MAX_PHOTOS)) })))
  const offersTyping = computed(() => readError !== null || noRecipeFound || availableAgainOn !== null)

  async function keepEdits() {
    emit('keepEdits')
    await nextTick()
    const asker: HTMLButtonElement | undefined = readButton.value?.$el
    if (asker && !asker.disabled) asker.focus()
    else focusHeading(heading.value)
  }

  function addFromDevice() {
    const input = fileInput.value
    const files = [...(input?.files ?? [])]
    if (!input || !files.length) return

    input.value = ''
    const picked = files.map((file) => Promise.resolve(file))
    emit('add', picked, 'device')
  }
</script>

<template>
  <KccSheet crisp wide :tear :pad="STEP_PAD" icon="fa-duotone fa-camera">
    <template #label><ResourceString for="PhotosTitle" /></template>

    <h2 ref="heading" class="sr-only">{{ rs('PhotosTitle') }}</h2>

    <div class="flex flex-col gap-6">
      <template v-if="!availableAgainOn">
        <CameraView @capture="(still) => emit('add', [still], 'camera')" />

        <div class="flex flex-wrap items-center justify-between gap-3">
          <Button variant="ghost" @click="fileInput?.click()">
            <i class="fa-duotone fa-images" aria-hidden="true"></i><ResourceString for="AddPhotos" />
          </Button>
          <input
            ref="fileInput"
            type="file"
            accept="image/*"
            multiple
            class="sr-only"
            tabindex="-1"
            aria-hidden="true"
            data-testid="photo-input"
            @change="addFromDevice"
          />

          <p class="kcc-kick" aria-live="polite">{{ count }}</p>
        </div>
      </template>

      <p v-for="message in photoMessages" :key="message.key" class="kcc-well kcc-well--danger kcc-kick" role="alert">
        {{ message.text }}
      </p>

      <PhotoStrip v-if="photos.length" :photos removable @remove="(id) => emit('remove', id)" />

      <ResourceString for="PrivacyNote" as="p" class="kcc-body text-ink-soft" />

      <p v-if="availableAgainOn" class="kcc-well kcc-kick" role="alert" data-testid="import-unavailable">
        {{ fill(rs('ImportUnavailable'), longDay(availableAgainOn)) }}
      </p>
      <p v-else-if="readError" class="kcc-well kcc-well--danger kcc-kick" role="alert" data-testid="read-error">
        {{ rs(readError) }}
      </p>
      <ResourceString
        v-else-if="noRecipeFound"
        for="NoRecipeFound"
        as="p"
        class="kcc-well kcc-well--warning kcc-kick"
        role="alert"
      />

      <ReadAgainWarning v-if="confirmingRead" :can-confirm="canRead" @confirm="emit('confirmRead')" @cancel="keepEdits" />

      <div class="flex flex-wrap items-center justify-between gap-x-3 gap-y-6">
        <Button v-if="canGoBack" variant="ghost" @click="emit('back')">
          <i class="fa-duotone fa-arrow-left" aria-hidden="true"></i><ResourceString for="Back" />
        </Button>

        <div class="ml-auto flex flex-wrap justify-end gap-x-3 gap-y-6">
          <Button v-if="returnsToReview" variant="ghost" data-testid="back-to-review" @click="emit('review')">
            <i class="fa-duotone fa-clipboard-check" aria-hidden="true"></i><ResourceString for="BackToReview" />
          </Button>

          <Button v-if="offersTyping" variant="ghost" @click="emit('typing')">
            <i class="fa-duotone fa-keyboard" aria-hidden="true"></i><ResourceString for="TypeInstead" />
          </Button>

          <Button :disabled="!canRead" ref="readButton" data-testid="read-photos" @click="emit('read')">
            <template v-if="readError">
              <ResourceString for="TryAgain" /><i class="fa-duotone fa-rotate-right" aria-hidden="true"></i>
            </template>
            <template v-else>
              <ResourceString for="ReadRecipe" /><i class="fa-duotone fa-arrow-right" aria-hidden="true"></i>
            </template>
          </Button>
        </div>
      </div>
    </div>
  </KccSheet>
</template>
