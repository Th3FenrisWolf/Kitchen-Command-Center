<script setup lang="ts">
import { IgnoreCase } from '~/utilities/string.extensions'

const { title, options, selected } = defineProps<{
  title: string
  options: string[]
  selected?: string
}>()
</script>

<template>
  <p class="underline underline-offset-4">{{ title }}</p>
  <ul>
    <li v-for="option in options" :key="option">
      <a href="#" :class="{ 'text-blue-500': option.equals(selected, IgnoreCase) }">
        {{ option }}
      </a>
    </li>
  </ul>
</template>
