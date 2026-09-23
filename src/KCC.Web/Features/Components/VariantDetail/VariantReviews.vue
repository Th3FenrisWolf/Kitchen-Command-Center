<!-- #region VariantReviews Component Properties -->
<script lang="ts">
  import { computed, onMounted, ref } from 'vue'
  import type { Review, ReviewsResponse } from '~/Types/Recipe'
  import { get, put, del } from '~/Utilities/Api'
  import { ResourceString, useResourceStrings } from '~/Components/ResourceStrings'
  import KccSheet, { type Tear } from '~/Components/Sheet/KccSheet.vue'
  import Button from '~/Components/Button/Button.vue'
  import StarRating from '~/Components/StarRating/StarRating.vue'
  import RatingSummary from '~/Components/StarRating/RatingSummary.vue'
  import { listTearFor } from '~/Utilities/BrandColor'

  /**
   * Rating histogram and paged reviews for a variant, with the member's own review editable inline.
   */
  export default {
    name: 'VariantReviews',
  }

  export interface VariantReviewsProps {
    variantGuid: string
    /**
     * Server-rendered starting values; the API's response drives them once reviews load.
     * @default 0
     */
    averageRating?: number
    /**
     * @default 0
     */
    reviewCount?: number
    /**
     * Gates the review form; existing reviews are always readable.
     * @default false
     */
    isAuthenticated?: boolean
  }
</script>
<!-- #endregion -->

<script setup lang="ts">
  const { variantGuid, averageRating = 0, reviewCount = 0, isAuthenticated = false } = defineProps<VariantReviewsProps>()

  const t = useResourceStrings()
  const reviews = ref<Review[]>([])
  const average = ref(averageRating)
  const count = ref(reviewCount)
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
    const result = await get<ReviewsResponse>(`/api/variant/${variantGuid}/reviews`, { page: nextPage, pageSize })
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
    const result = await put(`/api/variant/${variantGuid}/review`, { rating: myRating.value, text: myText.value })
    if (!result.success) {
      error.value = result.errorMessage
      return
    }
    await load(0)
  }

  const remove = async () => {
    const result = await del(`/api/variant/${variantGuid}/review`)
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

  // The form sits under the last review slip and above the first sibling card, which is always a 1: a 2
  // clears both, and a 3 clears the one case where the slip above it is itself a 2.
  const formTear = computed<Exclude<Tear, 'hero'>>(() =>
    reviews.value.length && listTearFor(reviews.value.length - 1) === 2 ? 3 : 2,
  )

  const formatDate = (iso: string) => {
    const d = new Date(iso)
    return Number.isNaN(d.getTime())
      ? ''
      : d.toLocaleDateString(undefined, { year: 'numeric', month: 'short', day: 'numeric' })
  }

  onMounted(() => load(0))
</script>

<template>
  <section>
    <div class="kcc-secname">
      <ResourceString for="RatingsReviews" as="h2" />
      <p class="kcc-kick">
        <span class="kcc-num">{{ count }}</span> <ResourceString for="Reviews" />
      </p>
    </div>

    <div class="space-y-9">
      <KccSheet v-if="count > 0" :tear="5">
        <div class="flex flex-wrap items-start gap-x-7 gap-y-6">
          <RatingSummary :value="average" />

          <!-- Each fill is 6px centred in a 24px band, so the run of bars keeps to the rule. -->
          <div class="min-w-60 flex-1">
            <div v-for="row in distRows" :key="row.star">
              <p class="kcc-kick" role="img" :aria-label="`${row.star} of 5 stars: ${row.count}`">
                <span class="kcc-num">{{ row.star }}</span
                ><i class="fa-solid fa-star" aria-hidden="true"></i>
                <span class="kcc-num">{{ row.count }}</span>
              </p>
              <div class="flex h-6 items-center">
                <span class="block h-1.5 bg-peach" :style="{ width: row.pct + '%' }"></span>
              </div>
            </div>
          </div>
        </div>
      </KccSheet>

      <ul v-if="reviews.length" data-testid="reviews-list" class="grid gap-9">
        <KccSheet v-for="(review, i) in reviews" :key="i" as="li" :tear="listTearFor(i)" pad="16px">
          <StarRating :model-value="review.rating" readonly />
          <p class="kcc-kick">
            {{ review.authorName }} · <span class="kcc-num">{{ formatDate(review.created) }}</span>
          </p>
          <p v-if="review.text" class="kcc-body">{{ review.text }}</p>
        </KccSheet>
      </ul>
      <ResourceString v-else for="NoReviewsYet" as="p" class="kcc-body" />

      <Button v-if="hasMore()" variant="ghost" @click="load(page + 1)">
        <ResourceString for="LoadMore" />
      </Button>

      <KccSheet v-if="isAuthenticated" icon="fa-duotone fa-comment-pen" :tear="formTear" crisp>
        <template #label><ResourceString for="YourReview" /></template>

        <div class="space-y-6">
          <StarRating v-model="myRating" />

          <!-- The e2e suite fills this textarea by hook, so the hook stays on the control, not on the field. -->
          <label class="kcc-field kcc-field--area">
            <textarea v-model="myText" :placeholder="t('WriteReview')" rows="3" data-testid="review-input"></textarea>
          </label>

          <p v-if="error" class="kcc-well kcc-well--danger kcc-kick" role="alert">{{ error }}</p>

          <div class="flex flex-wrap items-center justify-end gap-x-7 gap-y-3">
            <Button v-if="myRating > 0" variant="text" data-testid="delete-review" @click="remove">
              <ResourceString for="DeleteReview" />
            </Button>
            <Button data-testid="submit-review" :disabled="myRating < 0.5" @click="submit">
              <ResourceString for="SubmitReview" />
            </Button>
          </div>
        </div>
      </KccSheet>
      <ResourceString v-else for="LogInToReview" as="p" class="kcc-body" />
    </div>
  </section>
</template>
