<script setup lang="ts">
import Card from '~/components/Card.vue'
import Stacker from '~/components/Stacker.vue'
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

const stackerCards = [
  {
    title: 'Vodka',
    description: 'Clean and pure distillation',
    color: 'bg-lavender',
  },
  {
    title: 'Tequila',
    description: 'Blue agave essence',
    color: 'bg-sky',
  },
  {
    title: 'Gin',
    description: 'Juniper-forward botanical blend',
    color: 'bg-green',
  },
  {
    title: 'Whiskey',
    description: 'Oak-aged grain elixir',
    color: 'bg-peach',
  },
  {
    title: 'Rum',
    description: 'Sweet, tropical cane delight',
    color: 'bg-maroon',
  },
  {
    title: 'Wine',
    description: 'Fermented grape nectar',
    color: 'bg-red',
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
    <p class="text-heading mb-4">How About Something Sweeter?</p>
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

  <section class="thin">
    <div class="grid grid-cols-2 items-start gap-8">
      <div class="sticky top-8 grid gap-4">
        <h2 class="text-heading">Delicious Drinks</h2>
        <p class="text-pretty">
          The perfect drink can elevate any dining experience, transforming a simple meal into a
          memorable occasion. Whether it's a crisp white wine paired with seafood, a bold red
          complementing a hearty steak, or a craft cocktail that brings out subtle flavors in your
          appetizer, beverages play an essential role in the culinary journey. Even non-alcoholic
          options like sparkling waters infused with herbs or fresh-pressed juices can enhance and
          balance the flavors of your dish, creating a harmonious dining experience that engages all
          your senses.
        </p>
        <img src="https://picsum.photos/500" class="w-full rounded-3xl" />
      </div>
      <div>
        <Stacker :cards="stackerCards" />
      </div>
    </div>
  </section>

  <section>
    <div class="grid gap-8 text-balance">
      <p>
        Lorem ipsum dolor sit amet, consectetur adipiscing elit. Sed do eiusmod tempor incididunt ut
        labore et dolore magna aliqua. Ut enim ad minim veniam, quis nostrud exercitation ullamco
        laboris nisi ut aliquip ex ea commodo consequat.
      </p>

      <p>
        Duis aute irure dolor in reprehenderit in voluptate velit esse cillum dolore eu fugiat nulla
        pariatur. Excepteur sint occaecat cupidatat non proident, sunt in culpa qui officia deserunt
        mollit anim id est laborum.
      </p>

      <p>
        Sed ut perspiciatis unde omnis iste natus error sit voluptatem accusantium doloremque
        laudantium, totam rem aperiam, eaque ipsa quae ab illo inventore veritatis et quasi
        architecto beatae vitae dicta sunt explicabo.
      </p>

      <p>
        Nemo enim ipsam voluptatem quia voluptas sit aspernatur aut odit aut fugit, sed quia
        consequuntur magni dolores eos qui ratione voluptatem sequi nesciunt. Neque porro quisquam
        est, qui dolorem ipsum quia dolor sit amet.
      </p>

      <p>
        At vero eos et accusamus et iusto odio dignissimos ducimus qui blanditiis praesentium
        voluptatum deleniti atque corrupti quos dolores et quas molestias excepturi sint occaecati
        cupiditate non provident, similique sunt in culpa qui officia deserunt mollitia animi, id
        est laborum et dolorum fuga.
      </p>

      <p>
        Lorem ipsum dolor sit amet, consectetur adipiscing elit. Sed do eiusmod tempor incididunt ut
        labore et dolore magna aliqua. Ut enim ad minim veniam, quis nostrud exercitation ullamco
        laboris nisi ut aliquip ex ea commodo consequat.
      </p>

      <p>
        Duis aute irure dolor in reprehenderit in voluptate velit esse cillum dolore eu fugiat nulla
        pariatur. Excepteur sint occaecat cupidatat non proident, sunt in culpa qui officia deserunt
        mollit anim id est laborum.
      </p>

      <p>
        Sed ut perspiciatis unde omnis iste natus error sit voluptatem accusantium doloremque
        laudantium, totam rem aperiam, eaque ipsa quae ab illo inventore veritatis et quasi
        architecto beatae vitae dicta sunt explicabo.
      </p>

      <p>
        Nemo enim ipsam voluptatem quia voluptas sit aspernatur aut odit aut fugit, sed quia
        consequuntur magni dolores eos qui ratione voluptatem sequi nesciunt. Neque porro quisquam
        est, qui dolorem ipsum quia dolor sit amet.
      </p>

      <p>
        At vero eos et accusamus et iusto odio dignissimos ducimus qui blanditiis praesentium
        voluptatum deleniti atque corrupti quos dolores et quas molestias excepturi sint occaecati
        cupiditate non provident, similique sunt in culpa qui officia deserunt mollitia animi, id
        est laborum et dolorum fuga.
      </p>
    </div>
  </section>
</template>
