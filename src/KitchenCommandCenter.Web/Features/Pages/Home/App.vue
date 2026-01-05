<script setup lang="ts">
import Card from '~/Widgets/Card/Card.vue'
import Stacker from '~/vue-components/Stacker.vue'
import UnderlineLink from '~/vue-components/shared/UnderlineLink.vue'
import type { BackgroundColor } from '~/types/design-system'

type Card = {
  backgroundColor: BackgroundColor
  heading: string
  subheading: string
  body?: string
}

const dessertCards: Card[] = [
  {
    heading: 'Cakes',
    subheading: 'Rich',
    backgroundColor: 'bg-maroon',
  },
  {
    heading: 'Cookies',
    subheading: 'Addictive',
    backgroundColor: 'bg-yellow',
  },
  {
    heading: 'Pies',
    subheading: 'Comforting',
    backgroundColor: 'bg-peach',
  },
  {
    heading: 'Ice Cream',
    subheading: 'Dreamy',
    backgroundColor: 'bg-teal',
  },
]

const stackerCards: Card[] = [
  {
    heading: 'Vodka',
    subheading: 'Clean and pure distillation',
    backgroundColor: 'bg-lavender',
  },
  {
    heading: 'Tequila',
    subheading: 'Blue agave essence',
    backgroundColor: 'bg-sky',
  },
  {
    heading: 'Gin',
    subheading: 'Juniper-forward botanical blend',
    backgroundColor: 'bg-green',
  },
  {
    heading: 'Whiskey',
    subheading: 'Oak-aged grain elixir',
    backgroundColor: 'bg-peach',
  },
  {
    heading: 'Rum',
    subheading: 'Sweet, tropical cane delight',
    backgroundColor: 'bg-maroon',
  },
  {
    heading: 'Wine',
    subheading: 'Fermented grape nectar',
    backgroundColor: 'bg-red',
  },
]
</script>

<template>
  <section class="text-center">
    <h1 class="text-6xl">Welcome to the Dashboard!</h1>
  </section>

  <section class="breakout rounded-3xl bg-base p-8 text-center text-bone">
    <p class="mb-4 text-heading">How About Something Sweeter?</p>
    <div class="grid gap-6 lg:grid-cols-4">
      <Card
        v-for="dessert in dessertCards"
        :key="dessert.heading"
        :card-color="dessert.backgroundColor"
        card-text-color="text-onyx"
        drawer-color="bg-bone"
        drawer-text-color="text-onyx"
      >
        <div>
          <h2 class="text-heading">{{ dessert.heading }}</h2>
          <h3>{{ dessert.subheading }}</h3>
        </div>

        <template #drawer>
          <UnderlineLink color="bg-onyx" linkTo="/recipes?protein=beef">
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
