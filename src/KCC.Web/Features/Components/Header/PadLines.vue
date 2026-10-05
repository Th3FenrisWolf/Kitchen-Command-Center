<script setup lang="ts">
  import { h } from 'vue'
  import type { NavRow } from '~/Types/Nav'
  import { isCurrentPage } from './isCurrentPage'

  export interface PadLinesProps {
    rows: NavRow[]
    from: number
    glyphs?: boolean
    twoColumns?: boolean
    currentPage?: string
  }

  const { rows, from, glyphs = false, twoColumns = false, currentPage } = defineProps<PadLinesProps>()

  const LAST_STAGGER = 7

  // Rendered by hand: a template would wrap every run of the label in a fragment's comment markers.
  const LineLabel = ({ text }: { text: string }) =>
    h(
      'span',
      { class: 'pad-line-label' },
      text.split(/(\d+)/).map((run, at) => (at % 2 ? h('span', { class: 'kcc-num' }, run) : run)),
    )
</script>

<template>
  <ul class="pad-lines" :class="{ 'pad-lines--glyphs': glyphs, 'pad-lines--two': twoColumns }">
    <li
      v-for="(row, index) in rows"
      :key="`${row.url} ${row.label}`"
      data-row
      :style="{ '--i': Math.min(from + index, LAST_STAGGER) }"
    >
      <a
        class="pad-line"
        :href="row.url"
        :target="row.target"
        :aria-current="isCurrentPage(row.url, currentPage) ? 'page' : undefined"
      >
        <i v-if="glyphs && row.icon" :class="row.icon" aria-hidden="true"></i>
        <span v-else-if="glyphs" aria-hidden="true"></span>
        <LineLabel :text="row.label" />
        <span class="kcc-num">{{ row.count }}</span>
      </a>
    </li>
  </ul>
</template>

<style>
  .pad-lines {
    list-style: none;
    margin: 0;
    padding: 0;
  }

  .pad-lines--two {
    display: grid;
    gap: 0 18px;
    grid-template-columns: 1fr 1fr;
  }

  .pad-line {
    align-items: center;
    color: var(--color-ink);
    display: grid;
    font-size: 15px;
    gap: 8px;
    grid-template-columns: minmax(0, 1fr) auto;
    line-height: var(--bl);
    min-height: var(--bl);
    text-decoration: none;
  }

  .pad-lines--glyphs .pad-line {
    grid-template-columns: 18px minmax(0, 1fr) auto;
  }

  .pad-line > i {
    color: var(--color-ink-soft);
    font-size: 13px;
    text-align: center;
  }

  .pad-line > .kcc-num {
    color: var(--color-ink-soft);
    font-size: 12px;
  }

  .pad-line-label {
    overflow: hidden;
    text-decoration: underline;
    text-decoration-color: transparent;
    text-overflow: ellipsis;
    text-underline-offset: 3px;
    transition: text-decoration-color 300ms;
    white-space: nowrap;
  }

  .pad-line-label .kcc-num {
    line-height: 1;
  }

  .pad-line:hover > .pad-line-label {
    text-decoration-color: currentColor;
  }
</style>
