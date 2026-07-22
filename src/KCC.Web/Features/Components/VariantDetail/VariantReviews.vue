<script setup lang="ts">
  import { computed, onMounted, ref } from 'vue'
  import type { Review, ReviewsResponse } from '~/Types/Recipe'
  import { get, put, del } from '~/Utilities/Api'
  import { brandColorFor } from '~/Utilities/BrandColor'
  import { ResourceString, useResourceStrings } from '~/Components/ResourceStrings'
  import StarRating from '~/Components/StarRating/StarRating.vue'
  import { formatRating } from '~/Components/StarRating/starDisplay'

  const props = withDefaults(
    defineProps<{
      variantGuid: string
      averageRating?: number
      reviewCount?: number
      isAuthenticated?: boolean
    }>(),
    { averageRating: 0, reviewCount: 0, isAuthenticated: false },
  )

  const t = useResourceStrings()
  const reviews = ref<Review[]>([])
  const average = ref(props.averageRating ?? 0)
  const count = ref(props.reviewCount ?? 0)
  const distribution = ref<number[]>([])
  const total = ref(0)
  const page = ref(0)
  const pageSize = 10
  const loading = ref(false)
  const myRating = ref(0)
  const myText = ref('')
  const error = ref('')

  const load = async (nextPage = 0) => {
    loading.value = true
    const result = await get<ReviewsResponse>(`/api/variant/${props.variantGuid}/reviews`, { page: nextPage, pageSize })
    loading.value = false
    if (!result.success) {
      error.value = result.errorMessage
      return
    }
    const data = result.data
    reviews.value = nextPage === 0 ? data.reviews : [...reviews.value, ...data.reviews]
    average.value = data.average
    count.value = data.count
    distribution.value = data.distribution ?? []
    total.value = data.total
    page.value = data.page
    if (data.myReview) {
      myRating.value = data.myReview.rating
      myText.value = data.myReview.text ?? ''
    }
  }

  const submit = async () => {
    if (myRating.value < 0.5) return
    error.value = ''
    const result = await put(`/api/variant/${props.variantGuid}/review`, { rating: myRating.value, text: myText.value })
    if (!result.success) {
      error.value = result.errorMessage
      return
    }
    await load(0)
  }

  const remove = async () => {
    const result = await del(`/api/variant/${props.variantGuid}/review`)
    if (!result.success) {
      error.value = result.errorMessage
      return
    }
    myRating.value = 0
    myText.value = ''
    await load(0)
  }

  const hasMore = () => reviews.value.length < total.value

  // Histogram rows, 5★ down to 1★. distribution[i] holds the count for (i + 1)★;
  // half-star bucketing (round-down) is decided server-side.
  const distRows = computed(() => {
    const dist = distribution.value
    const totalRatings = dist.reduce((sum, n) => sum + n, 0)
    return [5, 4, 3, 2, 1].map((star) => {
      const c = dist[star - 1] ?? 0
      return { star, count: c, pct: totalRatings ? Math.round((c / totalRatings) * 100) : 0 }
    })
  })

  // Two-letter monogram for a reviewer's avatar (no dedicated Avatar component in the web app).
  const initials = (name: string) =>
    name
      .split(/\s+/)
      .filter(Boolean)
      .slice(0, 2)
      .map((w) => w[0]!.toUpperCase())
      .join('') || '?'

  const formatDate = (iso: string) => {
    const d = new Date(iso)
    return Number.isNaN(d.getTime()) ? '' : d.toLocaleDateString(undefined, { year: 'numeric', month: 'short', day: 'numeric' })
  }

  onMounted(() => load(0))
</script>

<template>
  <section class="mt-8">
    <h2 class="mb-4 flex items-center gap-2.5 font-casual text-2xl tracking-[1px]">
      <i class="fa-solid fa-star text-lg text-peach" aria-hidden="true"></i> <ResourceString for="RatingsReviews" />
    </h2>

    <div class="rounded-3xl bg-bone p-6 shadow-primary">
      <!-- Summary: average + per-star distribution -->
      <div v-if="count > 0" class="flex flex-wrap items-center gap-8">
        <div class="flex-none text-center">
          <div class="font-casual text-[3.5rem] leading-[0.9]">{{ formatRating(average) }}</div>
          <div class="mt-1">
            <StarRating :model-value="average" readonly />
          </div>
          <div class="mt-1 text-sm text-onyx-light">{{ count }} <ResourceString for="Reviews" /></div>
        </div>
        <ul class="flex min-w-60 flex-1 flex-col gap-[7px]">
          <li v-for="d in distRows" :key="d.star" class="flex items-center gap-2.5">
            <span class="w-[26px] flex-none text-sm text-onyx-light">{{ d.star }}★</span>
            <span class="h-2.5 flex-1 overflow-hidden rounded-full bg-bone-dark">
              <span class="block h-full rounded-full bg-surface-500" :style="{ width: d.pct + '%' }"></span>
            </span>
            <span class="w-9 flex-none text-right text-sm text-onyx-light">{{ d.count }}</span>
          </li>
        </ul>
      </div>

      <hr v-if="count > 0" class="my-6 border-0 border-t border-bone-dark" />

      <!-- Review list -->
      <ul v-if="reviews.length" data-testid="reviews-list" class="flex flex-col gap-4">
        <li v-for="(review, i) in reviews" :key="i" class="flex gap-3">
          <span
            class="grid size-11 flex-none place-items-center rounded-full font-bold text-onyx"
            :class="`bg-${brandColorFor(review.authorName)}`"
            aria-hidden="true"
            >{{ initials(review.authorName) }}</span
          >
          <div class="min-w-0 flex-1">
            <div class="flex flex-wrap items-center gap-2.5">
              <span class="font-bold">{{ review.authorName }}</span>
              <StarRating :model-value="review.rating" readonly />
              <span class="text-sm text-onyx-light">{{ formatDate(review.created) }}</span>
            </div>
            <p v-if="review.text" class="mt-1.5 text-onyx">{{ review.text }}</p>
          </div>
        </li>
      </ul>
      <div
        v-else
        class="grid place-items-center gap-3 rounded-3xl border-2 border-dashed border-bone-dark px-4 py-16 text-center text-onyx-light"
      >
        <i class="fa-regular fa-star text-4xl opacity-50" aria-hidden="true"></i>
        <p class="text-base"><ResourceString for="NoReviewsYet" /></p>
      </div>

      <button
        v-if="hasMore()"
        type="button"
        class="mt-6 cursor-pointer border-none bg-transparent font-bold text-onyx underline underline-offset-4"
        @click="load(page + 1)"
      >
        <ResourceString for="LoadMore" />
      </button>

      <hr class="my-6 border-0 border-t border-bone-dark" />

      <!-- Write a review -->
      <div v-if="isAuthenticated" class="rounded-3xl bg-bone-dark p-6">
        <p class="mb-3 font-casual text-xl tracking-[1px]"><ResourceString for="YourReview" /></p>
        <div class="mb-3">
          <StarRating v-model="myRating" />
        </div>
        <textarea
          v-model="myText"
          :placeholder="t('WriteReview')"
          rows="3"
          data-testid="review-input"
          class="w-full rounded-2xl border-none bg-bone p-3 text-onyx outline-none"
        ></textarea>
        <p v-if="error" class="mt-1 text-sm text-maroon">{{ error }}</p>
        <div class="mt-3 flex justify-end gap-2">
          <button
            v-if="myRating > 0"
            type="button"
            data-testid="delete-review"
            class="cursor-pointer rounded-full bg-bone px-4 py-2 font-bold text-onyx"
            @click="remove"
          >
            <ResourceString for="DeleteReview" />
          </button>
          <button
            type="button"
            data-testid="submit-review"
            class="cursor-pointer rounded-full bg-surface-500 px-6 py-2 font-bold text-bone disabled:cursor-not-allowed disabled:opacity-50"
            :disabled="myRating < 0.5"
            @click="submit"
          >
            <ResourceString for="SubmitReview" />
          </button>
        </div>
      </div>
      <div v-else class="rounded-3xl border-2 border-dashed border-bone-dark p-4 text-center text-onyx-light">
        <ResourceString for="LogInToReview" />
      </div>
    </div>
  </section>
</template>
