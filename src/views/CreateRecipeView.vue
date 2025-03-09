<script setup lang="ts">
import { storeToRefs } from 'pinia'
import { ref } from 'vue'
import SmallHero from '~/components/SmallHero.vue'
import { addRecipe } from '~/database/recipes'
import { useUserStore } from '~/store/user'

const userStore = useUserStore()
const { user } = storeToRefs(userStore)

const title = ref('')
const description = ref('')
const ingredients = ref('')

const handleSubmit = () => {
  if (!user.value) {
    console.error('User is not logged in')
    return
  }

  addRecipe(title.value, description.value, ingredients.value, user.value.uid)
  title.value = ''
  description.value = ''
  ingredients.value = ''
  console.log('Recipe added successfully!')
}
</script>

<template>
  <SmallHero dark>
    <template #title>Create Recipe</template>
  </SmallHero>

  <section>
    <form @submit.prevent="handleSubmit" class="flex flex-col gap-4">
      <label class="flex flex-col gap-2">
        <span class="text-lg font-bold">Title</span>
        <input
          v-model="title"
          required
          type="text"
          placeholder="Enter recipe title"
          class="border-base rounded-3xl border px-4 py-2 text-xl"
        />
      </label>

      <label class="flex flex-col gap-2">
        <span class="text-lg font-bold">Description</span>
        <textarea
          v-model="description"
          required
          placeholder="Enter recipe description"
          class="border-base rounded-3xl border px-4 py-2 text-xl"
        ></textarea>
      </label>

      <label class="flex flex-col gap-2">
        <span class="border-base font-bold">Ingredients</span>
        <textarea
          v-model="ingredients"
          required
          placeholder="Enter ingredients"
          class="border-base rounded-3xl border px-4 py-2 text-xl"
        ></textarea>
      </label>

      <button type="submit" class="bg-base text-bone cursor-pointer rounded-3xl px-4 py-2 text-xl">
        Create Recipe
      </button>
    </form>
  </section>
</template>
