<script setup lang="ts">
  import { onMounted, ref } from 'vue'
  import type { Review, ReviewsResponse } from '~/Types/Recipe'
  import { get, put, del } from '~/Utilities/Api'
  import { ResourceString, useResourceStrings } from '~/Components/ResourceStrings'
  import StarRating from '~/Components/StarRating/StarRating.vue'

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
    total.value = data.total
    page.value = data.page
    if (data.myReview) {
      myRating.value = data.myReview.rating
      myText.value = data.myReview.text ?? ''
    }
  }

  const submit = async () => {
    if (myRating.value < 1) return
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

  onMounted(() => load(0))
</script>

<template>
  <section class="mt-8">
    <h2 class="mb-4 flex items-center gap-2.5 font-casual text-2xl tracking-[1px]">
      <i class="fa-solid fa-star text-lg text-peach"></i> <ResourceString for="RatingsReviews" />
      <span v-if="count > 0" class="font-hazelnut text-lg text-onyx-light"> {{ average.toFixed(1) }} ({{ count }}) </span>
    </h2>

    <div v-if="isAuthenticated" class="mb-6 rounded-3xl bg-bone p-4 shadow-light">
      <p class="mb-2 font-bold"><ResourceString for="YourReview" /></p>
      <StarRating v-model="myRating" />
      <textarea
        v-model="myText"
        :placeholder="t('WriteReview')"
        rows="3"
        class="mt-2 w-full rounded-2xl border-none bg-bone-dark p-3 text-onyx outline-none"
      ></textarea>
      <p v-if="error" class="mt-1 text-sm text-maroon">{{ error }}</p>
      <div class="mt-2 flex gap-2">
        <button
          type="button"
          class="cursor-pointer rounded-2xl bg-surface-500 px-4 py-2 text-bone disabled:opacity-50"
          :disabled="myRating < 1"
          @click="submit"
        >
          <ResourceString for="SubmitReview" />
        </button>
        <button
          v-if="myRating > 0"
          type="button"
          class="cursor-pointer rounded-2xl bg-bone-dark px-4 py-2 text-onyx"
          @click="remove"
        >
          <ResourceString for="DeleteReview" />
        </button>
      </div>
    </div>
    <div v-else class="mb-6 rounded-3xl border-2 border-dashed border-bone-dark p-4 text-center text-onyx-light">
      <ResourceString for="LogInToReview" />
    </div>

    <ul v-if="reviews.length" class="flex flex-col gap-3">
      <li v-for="(review, i) in reviews" :key="i" class="rounded-2xl bg-bone p-4 shadow-light">
        <div class="flex items-center justify-between gap-2">
          <span class="font-bold">{{ review.authorName }}</span>
          <StarRating :model-value="review.rating" readonly />
        </div>
        <p v-if="review.text" class="mt-1 text-onyx-light">{{ review.text }}</p>
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
      class="mt-4 w-full cursor-pointer rounded-2xl bg-bone-dark py-2 text-onyx"
      @click="load(page + 1)"
    >
      <ResourceString for="LoadMore" />
    </button>
  </section>
</template>
