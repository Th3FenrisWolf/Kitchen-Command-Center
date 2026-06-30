<script setup lang="ts">
  import { onMounted, ref } from 'vue'
  import type { CookNote, CookNotesResponse } from '~/Types/Recipe'
  import { get, post, del } from '~/Utilities/Api'
  import { ResourceString, useResourceStrings } from '~/Components/ResourceStrings'

  const props = withDefaults(defineProps<{ variantGuid: string; isAuthenticated?: boolean }>(), {
    isAuthenticated: false,
  })

  const t = useResourceStrings()
  const notes = ref<CookNote[]>([])
  const total = ref(0)
  const page = ref(0)
  const pageSize = 10
  const draft = ref('')
  const error = ref('')

  const load = async (nextPage = 0) => {
    const result = await get<CookNotesResponse>(`/api/variant/${props.variantGuid}/notes`, { page: nextPage, pageSize })
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
    const result = await post<{ id: number }>(`/api/variant/${props.variantGuid}/note`, { text: draft.value })
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
  <section class="mt-8">
    <h2 class="mb-4 flex items-center gap-2.5 font-casual text-2xl tracking-[1px]">
      <i class="fa-solid fa-lightbulb text-lg text-yellow"></i> <ResourceString for="CookNotes" />
    </h2>

    <div v-if="isAuthenticated" class="mb-6 rounded-3xl bg-bone p-4 shadow-light">
      <textarea
        v-model="draft"
        :placeholder="t('CookNotePlaceholder')"
        rows="3"
        data-testid="cook-note-input"
        class="w-full rounded-2xl border-none bg-bone-dark p-3 text-onyx outline-none"
      ></textarea>
      <p v-if="error" class="mt-1 text-sm text-maroon">{{ error }}</p>
      <button
        type="button"
        data-testid="add-cook-note"
        class="mt-2 cursor-pointer rounded-2xl bg-surface-500 px-4 py-2 text-bone disabled:opacity-50"
        :disabled="!draft.trim()"
        @click="add"
      >
        <ResourceString for="AddCookNote" />
      </button>
    </div>

    <ul v-if="notes.length" class="flex flex-col gap-3">
      <li v-for="note in notes" :key="note.id" class="rounded-2xl bg-bone p-4 shadow-light">
        <div class="flex items-center justify-between gap-2">
          <span class="font-bold">{{ note.authorName }}</span>
          <button v-if="note.isMine" type="button" class="cursor-pointer text-sm text-maroon" @click="remove(note.id)">
            <ResourceString for="DeleteNote" />
          </button>
        </div>
        <p class="mt-1 text-onyx-light">{{ note.text }}</p>
      </li>
    </ul>
    <div
      v-else
      class="grid place-items-center gap-3 rounded-3xl border-2 border-dashed border-bone-dark px-4 py-12 text-center text-onyx-light"
    >
      <p class="text-base"><ResourceString for="NoCookNotesYet" /></p>
    </div>

    <button
      v-if="hasMore()"
      type="button"
      class="mt-4 w-full cursor-pointer rounded-2xl bg-bone-dark py-2 text-onyx"
      @click="load(page + 1)"
    >
      <ResourceString for="LoadMore" />
    </button>
  </section>
</template>
