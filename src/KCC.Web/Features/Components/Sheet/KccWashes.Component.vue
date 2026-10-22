<script setup lang="ts">
  import { onUnmounted } from 'vue'
  import { useBloom } from '~/Bloom/bloomContext'
  import { isGlobalKey, planSheet, toFixed3, type PlannedWash, type WashSize } from '~/Bloom/washes'
  import type { Wash } from '~/Types/DesignSystem'

  const {
    washKey,
    size = 'card',
    chosen,
    washSlots,
  } = defineProps<{ washKey: string; size?: WashSize; chosen?: Wash; washSlots?: readonly number[] }>()

  const bloom = useBloom()
  const key = bloom.claimKey(washKey)
  onUnmounted(() => bloom.releaseKey(key))
  const sheet = { seed: bloom.seed, key, size, chosen }
  const planned = planSheet(sheet)
  const starts = bloom.lateStarts(planned.length)
  const washes = starts ? planSheet({ ...sheet, starts }) : planned

  const slots = isGlobalKey(key) ? washSlots : undefined
  for (const { id, index } of washes) bloom.collect({ id, key, index, slot: slots?.[index] })

  const styleOf = (wash: PlannedWash) =>
    Object.entries(wash.style)
      .map(([property, value]) => `${property}:${value}`)
      .join(';')

  // Bound through v-bind so a field set up before hydration prints no `style=""`.
  const fieldAttrs = starts ? { style: `--wash-lag:${toFixed3(bloom.lag())}s` } : {}
</script>

<template>
  <span v-if="washes.length" class="kcc-washes" v-bind="fieldAttrs" aria-hidden="true">
    <span v-for="wash in washes" :key="wash.id" class="kcc-wash" :data-wash="wash.id" :style="styleOf(wash)"></span>
  </span>
</template>
