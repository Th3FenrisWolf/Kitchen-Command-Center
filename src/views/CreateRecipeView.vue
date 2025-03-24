<script setup lang="ts">
import { storeToRefs } from 'pinia'
import { ref } from 'vue'
import InputField from '~/components/shared/InputField.vue'
import TextAreaField from '~/components/shared/TextAreaField.vue'
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
        <InputField v-model="title" required type="text" placeholder="Enter recipe title" />
      </label>

      <label class="flex flex-col gap-2">
        <span class="text-lg font-bold">Description</span>
        <TextAreaField v-model="description" required placeholder="Enter recipe description" />
      </label>

      <label class="flex flex-col gap-2">
        <span class="border-base font-bold">Ingredients</span>
        <TextAreaField v-model="ingredients" required placeholder="Enter ingredients" />
      </label>

      <button type="submit" class="bg-base text-bone cursor-pointer rounded-3xl px-4 py-2 text-xl">
        Create Recipe
      </button>
    </form>
  </section>
</template>
