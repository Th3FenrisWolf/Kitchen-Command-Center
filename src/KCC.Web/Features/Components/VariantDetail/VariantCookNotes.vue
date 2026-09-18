<!-- #region VariantCookNotes Component Properties -->
<script lang="ts">
  import { onMounted, ref } from 'vue'
  import type { CookNote, CookNotesResponse } from '~/Types/Recipe'
  import { get, post, del } from '~/Utilities/Api'
  import { ResourceString, useResourceStrings } from '~/Components/ResourceStrings'
  import KccSheet from '~/Components/Sheet/KccSheet.vue'
  import Button from '~/Components/Button/Button.vue'

  /**
   * Paged list of cooks' notes on a variant, with a compose box for signed-in members.
   */
  export default {
    name: 'VariantCookNotes',
  }

  export interface VariantCookNotesProps {
    variantGuid: string
    /**
     * Gates the compose box; the notes themselves are always readable.
     * @default false
     */
    isAuthenticated?: boolean
  }
</script>
<!-- #endregion -->

<script setup lang="ts">
  const { variantGuid, isAuthenticated = false } = defineProps<VariantCookNotesProps>()

  const t = useResourceStrings()
  const notes = ref<CookNote[]>([])
  const total = ref(0)
  const page = ref(0)
  const pageSize = 10
  const draft = ref('')
  const error = ref('')

  const load = async (nextPage = 0) => {
    const result = await get<CookNotesResponse>(`/api/variant/${variantGuid}/notes`, { page: nextPage, pageSize })
    if (!result.success) {
      error.value = result.errorMessage
      return
    }
    notes.value = nextPage === 0 ? result.data.notes : [...notes.value, ...result.data.notes]
    total.value = result.data.total
    page.value = result.data.page
  }

  const add = async () => {
    if (!draft.value.trim()) return
    error.value = ''
    const result = await post<{ id: number }>(`/api/variant/${variantGuid}/note`, draft.value)
    if (!result.success) {
      error.value = result.errorMessage
      return
    }
    draft.value = ''
    await load(0)
  }

  const remove = async (id: number) => {
    const result = await del(`/api/note/${id}`)
    if (!result.success) {
      error.value = result.errorMessage
      return
    }
    await load(0)
  }

  const hasMore = () => notes.value.length < total.value

  onMounted(() => load(0))
</script>

<template>
  <KccSheet as="section" icon="fa-duotone fa-pen-nib" :tear="4">
    <template #label><ResourceString for="CookNotes" /></template>

    <!-- The sheet's label carries the panel's name in print; the heading carries it in the document. -->
    <h2 class="sr-only">{{ t('CookNotes') }}</h2>

    <!-- The heading takes no room, so the rhythm is between the blocks, never above the first of them. -->
    <div class="space-y-6">
      <div v-if="isAuthenticated" class="space-y-6">
        <!-- The e2e suite fills this textarea by hook, so the hook stays on the control, not on the field. -->
        <label class="kcc-field kcc-field--area">
          <textarea
            v-model="draft"
            :placeholder="t('CookNotePlaceholder')"
            rows="3"
            data-testid="cook-note-input"
          ></textarea>
        </label>
        <p v-if="error" class="kcc-well kcc-well--danger kcc-kick" role="alert">{{ error }}</p>
        <Button data-testid="add-cook-note" :disabled="!draft.trim()" @click="add">
          <ResourceString for="AddCookNote" />
        </Button>
      </div>

      <ul v-if="notes.length" data-testid="cook-notes-list" class="space-y-6">
        <li v-for="note in notes" :key="note.id">
          <div class="flex items-baseline justify-between gap-3">
            <p class="kcc-kick">{{ note.authorName }}</p>
            <Button v-if="note.isMine" variant="text" @click="remove(note.id)">
              <ResourceString for="DeleteNote" />
            </Button>
          </div>
          <p class="kcc-body">{{ note.text }}</p>
        </li>
      </ul>
      <ResourceString v-else for="NoCookNotesYet" as="p" class="kcc-body" />

      <Button v-if="hasMore()" variant="ghost" @click="load(page + 1)">
        <ResourceString for="LoadMore" />
      </Button>
    </div>
  </KccSheet>
</template>
