<script setup lang="ts">
import Card from '~/components/Card.vue'
import UnderlineLink from '~/components/shared/UnderlineLink.vue'

const proteinCards = [
  {
    title: 'Beef',
    quipText: 'Bold · Juicy · Satisfying',
    description: 'Sizzle, sear, or slow-cook — bring out the bold flavors of beef!',
    theme: 'dark',
  },
  {
    title: 'Chicken',
    quipText: 'Versatile · Tasty · Essential',
    description: 'Grill, roast, or fry — turn this staple into something amazing!',
    theme: 'light',
  },
  {
    title: 'Pork',
    quipText: 'Savory · Tender · Irresistible',
    description: 'Chops, bacon, or pulled — discover the best of pork!',
    theme: 'light',
  },
  {
    title: 'Fish',
    quipText: 'Delicate · Fresh · Delicious',
    description: 'Light, crispy, or tender — find fresh fish favorites!',
    theme: 'dark',
  },
]

const dessertCards = [
  {
    title: 'Cakes',
    quipText: 'Rich',
  },
  {
    title: 'Cookies',
    quipText: 'Addictive',
  },
  {
    title: 'Pies',
    quipText: 'Comforting',
  },
  {
    title: 'Ice Cream',
    quipText: 'Dreamy',
  },
]
</script>

<template>
  <section class="text-center">
    <h1 class="text-6xl">Welcome to the Dashboard!</h1>
  </section>

  <section class="grid gap-8 lg:grid-cols-2">
    <Card v-for="protein in proteinCards" :key="protein.title" :dark="protein.theme === 'dark'">
      <div>
        <h2 class="text-heading">{{ protein.title }}</h2>
        <h3>{{ protein.quipText }}</h3>
      </div>

      <template #drawer>
        <p class="text-balance">{{ protein.description }}</p>
        <UnderlineLink
          :color="protein.theme === 'dark' ? 'bg-base' : 'bg-bone'"
          linkTo="/recipes?protein=beef"
        >
          View Recipes
        </UnderlineLink>
      </template>
    </Card>
  </section>

  <section class="breakout bg-base text-bone rounded-3xl p-8 text-center">
    <p class="text-heading">How About Something Sweeter?</p>
    <div class="grid gap-6 lg:grid-cols-4">
      <Card v-for="dessert in dessertCards" :key="dessert.title">
        <div>
          <h2 class="text-heading">{{ dessert.title }}</h2>
          <h3>{{ dessert.quipText }}</h3>
        </div>

        <template #drawer>
          <UnderlineLink color="bg-bone" linkTo="/recipes?protein=beef">
            View Recipes
          </UnderlineLink>
        </template>
      </Card>
    </div>
  </section>
</template>
