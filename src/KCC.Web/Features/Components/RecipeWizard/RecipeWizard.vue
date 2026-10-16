<script setup lang="ts">
  import { computed, nextTick, onBeforeUnmount, reactive, ref, watch, type ComponentPublicInstance } from 'vue'
  import { provideResourceStrings } from '~/Components/ResourceStrings'
  import WizardProgress from '~/Components/Wizard/WizardProgress.vue'
  import { listTearFor } from '~/Utilities/BrandColor'
  import { post } from '~/Utilities/Api'
  import BasicsStep from './BasicsStep.vue'
  import IngredientsStep from './IngredientsStep.vue'
  import InstructionsStep from './InstructionsStep.vue'
  import LeaveConfirm from './LeaveConfirm.vue'
  import { controlOfInertAncestor, guardedLink } from './leaveGuard'
  import MatchStep from './MatchStep.vue'
  import { encodePhoto, type EncodedPhoto } from './photoEncoding'
  import PhotoStep from './PhotoStep.vue'
  import PhotoStrip from './PhotoStrip.vue'
  import ReadingState from './ReadingState.vue'
  import type { ReadRequest, ReadResult } from './recipeImportApi'
  import {
    acceptMatch,
    addIngredient,
    addInstruction,
    addPendingPhotos,
    back,
    backToReview,
    canGoBack,
    canProceed,
    canStartRead,
    changeMatch,
    changePhotos,
    choosePhotos,
    chooseTyping,
    createWizardFlow,
    edit,
    editIngredient,
    editInstruction,
    editValue,
    finishRead,
    hasUnsavedWork,
    isImport,
    keepEdits,
    keepNewRecipe,
    next,
    offersMatch,
    pickRecipe,
    progress,
    removeIngredient,
    removeInstruction,
    removePhoto,
    showsPhotoStrip,
    startRead,
    submissionFor,
    type WizardMode,
    type WizardRecipe,
  } from './recipeWizardFlow'
  import ReviewStep from './ReviewStep.vue'
  import StartStep from './StartStep.vue'
  import { stepFlips } from './stepFlip'
  import { focusHeading } from './stepSheet'
  import SubmittedSheet, { type ClosingLink } from './SubmittedSheet.vue'
  import VariantStep from './VariantStep.vue'
  import { submitErrorKey, type SubmitErrorKey } from './wizardErrors'

  export interface ImportAvailability {
    enabled: boolean
    available: boolean
    availableAgainOn?: string
    maxImageEdge: number
  }

  export interface RecipeWizardProps {
    flow: WizardMode
    recipe?: WizardRecipe
    resourceStrings?: Record<string, string>
    recipesUrl?: string
    closingLink?: ClosingLink
    photoImport?: ImportAvailability
  }

  const IMPORT_OFF: ImportAvailability = { enabled: false, available: false, maxImageEdge: 0 }

  const props = defineProps<RecipeWizardProps>()

  const rs = provideResourceStrings(props.resourceStrings, 'RecipeWizard')

  const photoImport = computed(() => props.photoImport ?? IMPORT_OFF)

  const wizard = reactive(
    createWizardFlow({
      flow: props.flow,
      recipe: props.recipe,
      importEnabled: photoImport.value.enabled,
      availableAgainOn: photoImport.value.available ? null : photoImport.value.availableAgainOn,
    }),
  )

  const stepProgress = computed(() => progress(wizard))
  const currentStep = computed(() => stepProgress.value?.current ?? 1)
  const tear = computed(() => listTearFor(currentStep.value - 1))
  const flip = computed(() => stepFlips(currentStep.value))

  const returnsToReview = computed(() => wizard.returnTo === 'review')

  const readRequest = ref<ReadRequest | null>(null)

  const submitting = ref(false)
  const submitError = ref<SubmitErrorKey | null>(null)

  const leaving = ref<HTMLAnchorElement | null>(null)

  const stepSection = ref<HTMLElement | null>(null)
  const submittedSheet = ref<ComponentPublicInstance | null>(null)

  watch(
    () => wizard.step,
    async (step) => {
      submitError.value = null
      await nextTick()
      if (leaving.value !== null) return

      const holder: HTMLElement | null = step === 'submitted' ? submittedSheet.value?.$el : stepSection.value
      focusHeading(holder?.querySelector<HTMLElement>('h2'))
    },
  )

  function onLinkClick(event: MouseEvent) {
    const link = guardedLink(event, window.location.href)
    if (!link) return

    event.preventDefault()
    leaving.value = link
  }

  function onBeforeUnload(event: BeforeUnloadEvent) {
    event.preventDefault()
    // Older browsers prompt only for a return value, not for preventDefault().
    event.returnValue = true
  }

  function guardLeaving() {
    document.addEventListener('click', onLinkClick, true)
    window.addEventListener('beforeunload', onBeforeUnload)
  }

  function stopGuardingLeaving() {
    document.removeEventListener('click', onLinkClick, true)
    window.removeEventListener('beforeunload', onBeforeUnload)
  }

  watch(
    () => hasUnsavedWork(wizard),
    (unsaved) => (unsaved ? guardLeaving() : stopGuardingLeaving()),
  )

  onBeforeUnmount(stopGuardingLeaving)

  async function stay() {
    const link = leaving.value
    leaving.value = null
    // An open header card closes only when the click on Stay reaches the document, after this handler.
    await new Promise((resolve) => setTimeout(resolve))
    if (!link) return

    link.focus()
    if (document.activeElement !== link) controlOfInertAncestor(link)?.focus()
  }

  function leave() {
    const link = leaving.value
    leaving.value = null
    stopGuardingLeaving()
    if (link) window.location.assign(link.href)
  }

  // One photo at a time: decoding a whole pick at once can run a phone out of memory.
  function encodeInTurn(photos: Promise<Blob | null>[]): Promise<EncodedPhoto | null>[] {
    const { maxImageEdge } = photoImport.value
    let turn: Promise<unknown> = Promise.resolve()
    return photos.map((photo) => {
      const encoded = turn.then(() => photo).then((blob) => blob && encodePhoto(blob, maxImageEdge))
      turn = encoded.catch(() => null)
      return encoded
    })
  }

  function read(confirmed = false) {
    readRequest.value = startRead(wizard, confirmed)
  }

  function finish(result: ReadResult) {
    readRequest.value = null
    finishRead(wizard, result)
  }

  async function submit() {
    submitting.value = true
    submitError.value = null

    const { url, body } = submissionFor(wizard)
    const result = await post(url, body)
    submitting.value = false

    if (!result.success) {
      submitError.value = submitErrorKey(result)
      return
    }

    wizard.step = 'submitted'
  }
</script>

<template>
  <SubmittedSheet v-if="wizard.step === 'submitted'" ref="submittedSheet" :mode="wizard.mode" :recipes-url :closing-link />

  <section v-else ref="stepSection" class="mt-6">
    <WizardProgress
      v-if="stepProgress"
      :current="stepProgress.current"
      :total="stepProgress.total"
      :label="rs('ProgressLabel')"
    />

    <PhotoStrip v-if="showsPhotoStrip(wizard)" :photos="wizard.photos" class="mb-6" data-testid="wizard-photos" />

    <StartStep
      v-if="wizard.step === 'start'"
      :available-again-on="wizard.availableAgainOn"
      :tear
      :flip
      @photos="choosePhotos(wizard)"
      @typing="chooseTyping(wizard)"
    />

    <PhotoStep
      v-else-if="wizard.step === 'photos'"
      :photos="wizard.photos"
      :photo-errors="wizard.photoErrors"
      :read-error="wizard.readError"
      :no-recipe-found="wizard.noRecipeFound"
      :available-again-on="wizard.availableAgainOn"
      :can-go-back="canGoBack(wizard)"
      :can-read="canStartRead(wizard)"
      :returns-to-review
      :confirming-read="wizard.confirmingRead"
      :tear
      :flip
      @add="(photos, source) => addPendingPhotos(wizard, encodeInTurn(photos), source)"
      @remove="(id) => removePhoto(wizard, id)"
      @read="read()"
      @confirm-read="read(true)"
      @keep-edits="keepEdits(wizard)"
      @typing="chooseTyping(wizard)"
      @back="back(wizard)"
      @review="backToReview(wizard)"
    />

    <ReadingState v-else-if="wizard.step === 'reading' && readRequest" :request="readRequest" :tear :flip @finish="finish" />

    <MatchStep
      v-else-if="wizard.step === 'match' && wizard.read?.match"
      :match="wizard.read.match"
      :recipes="wizard.read.recipes"
      :chosen-key="wizard.recipe?.key"
      :possible-duplicate="wizard.read.possibleDuplicate"
      :can-go-back="canGoBack(wizard)"
      :tear
      :flip
      @accept="acceptMatch(wizard)"
      @pick="(recipe) => pickRecipe(wizard, recipe)"
      @keep="keepNewRecipe(wizard)"
      @back="back(wizard)"
    />

    <BasicsStep
      v-else-if="wizard.step === 'basics'"
      :values="wizard.values"
      :tear
      :flip
      :can-go-back="canGoBack(wizard)"
      :can-proceed="canProceed(wizard)"
      :returns-to-review
      @edit="(field, value) => editValue(wizard, field, value)"
      @next="next(wizard)"
      @back="back(wizard)"
      @review="backToReview(wizard)"
    />

    <VariantStep
      v-else-if="wizard.step === 'variant'"
      :mode="wizard.mode"
      :values="wizard.values"
      :tear
      :flip
      :can-go-back="canGoBack(wizard)"
      :can-proceed="canProceed(wizard)"
      :returns-to-review
      @edit="(field, value) => editValue(wizard, field, value)"
      @next="next(wizard)"
      @back="back(wizard)"
      @review="backToReview(wizard)"
    />

    <IngredientsStep
      v-else-if="wizard.step === 'ingredients'"
      :values="wizard.values"
      :tear
      :flip
      :can-go-back="canGoBack(wizard)"
      :can-proceed="canProceed(wizard)"
      :returns-to-review
      @edit="(index, change) => editIngredient(wizard, index, change)"
      @add="addIngredient(wizard)"
      @remove="(index) => removeIngredient(wizard, index)"
      @next="next(wizard)"
      @back="back(wizard)"
      @review="backToReview(wizard)"
    />

    <InstructionsStep
      v-else-if="wizard.step === 'instructions'"
      :values="wizard.values"
      :tear
      :flip
      :can-go-back="canGoBack(wizard)"
      :can-proceed="canProceed(wizard)"
      :returns-to-review
      @edit="(index, text) => editInstruction(wizard, index, text)"
      @add="addInstruction(wizard)"
      @remove="(index) => removeInstruction(wizard, index)"
      @next="next(wizard)"
      @back="back(wizard)"
      @review="backToReview(wizard)"
    />

    <ReviewStep
      v-else-if="wizard.step === 'review'"
      :mode="wizard.mode"
      :values="wizard.values"
      :other-recipes="wizard.read?.otherRecipes"
      :possible-duplicate="wizard.read?.possibleDuplicate"
      :changes-match="offersMatch(wizard)"
      :adding-to="wizard.mode === 'variant' && offersMatch(wizard) ? wizard.recipe?.name : null"
      :imported="isImport(wizard)"
      :can-read="canStartRead(wizard)"
      :confirming-read="wizard.confirmingRead"
      :tear
      :flip
      :submitting
      :error="submitError"
      @back="back(wizard)"
      @submit="submit"
      @edit="(section) => edit(wizard, section)"
      @change-match="changeMatch(wizard)"
      @change-photos="changePhotos(wizard)"
      @read="read()"
      @confirm-read="read(true)"
      @keep-edits="keepEdits(wizard)"
    />
  </section>

  <LeaveConfirm :open="leaving !== null" @stay="stay" @leave="leave" />
</template>
