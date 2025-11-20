<script setup lang="ts">
import { ref, computed, watch } from 'vue'
import cx from '~/utilities/cx'
import router from '~/router'
import { parseQueryString } from '~/utilities/query-functions'
import InputField from '~/components/shared/InputField.vue'

const { returnUrl } = parseQueryString<{ returnUrl: string }>()

const swap = ref(false)
const isSignIn = ref(true)
const signText = computed(() => (isSignIn.value ? 'Sign In' : 'Sign Up'))

watch(swap, () => {
  formError.value = null
  setTimeout(() => {
    clearForm()
    isSignIn.value = !isSignIn.value
  }, 250)
})

const email = ref('')
const password = ref('')
const formError = ref<string | null>(null)

const clearForm = () => {
  email.value = ''
  password.value = ''
}

const sendForm = async (
  action: (arg0: string, arg1: string) => Promise<{ success: boolean; message: string }>,
): Promise<boolean> => {
  const response = await action(email.value, password.value)
  if (!response.success) {
    formError.value = response.message
  }
  return response.success
}

const handleSubmit = async () => {
  // formError.value = null
  // const response = await sendForm(isSignIn.value ? signIn : signUp)
  // clearForm()
  // if (response) {
  //   router.push({ path: returnUrl ?? '/' })
  // }
}
</script>

<template>
  <section
    class="no-margin fixed left-[50dvw] top-[50dvh] w-3/4 -translate-x-1/2 -translate-y-1/2 place-items-center"
  >
    <div class="shadow-primary relative flex w-3/4 overflow-hidden rounded-3xl">
      <div
        :class="
          cx(
            'relative left-[0%] flex basis-[60%] flex-col justify-center gap-4 p-12 text-center transition-all duration-500',
            swap && 'left-[40%]',
          )
        "
      >
        <h2 class="text-heading">{{ signText }}</h2>

        <p
          :class="
            cx('text-maroon overflow-hidden transition-all duration-500', formError ? 'h-8' : 'h-0')
          "
        >
          {{ formError }}
        </p>

        <form class="grid grow-0 gap-8" @submit.prevent="handleSubmit">
          <InputField
            required
            type="email"
            v-model="email"
            autocomplete="email"
            placeholder="Email"
          />

          <InputField
            required
            type="password"
            v-model="password"
            :autocomplete="!isSignIn ? 'new-password' : 'current-password'"
            placeholder="Password"
          />

          <button
            class="bg-base text-bone w-max cursor-pointer justify-self-center rounded-2xl px-4 py-2"
            type="submit"
          >
            {{ signText }}
          </button>
        </form>
      </div>

      <div
        :class="
          cx(
            'bg-base relative right-[0%] basis-[40%] justify-items-center overflow-hidden p-12 text-center transition-all duration-500',
            swap && 'right-[60%]',
          )
        "
      >
        <div
          :class="
            cx(
              'relative flex h-full w-[400%] justify-between transition-all duration-500',
              swap ? 'left-[150%]' : 'left-[-150%]',
            )
          "
        >
          <div class="text-bone grid h-max w-1/4 gap-8 self-center" :aria-hidden="isSignIn">
            <h3 class="text-heading font-[APCasual]">Have an Account?</h3>
            <p>Sign in to continue commanding your kitchen</p>
            <button
              class="bg-bone text-onyx w-max cursor-pointer justify-self-center rounded-2xl px-4 py-2"
              @click="swap = !swap"
              type="button"
            >
              Sign In
            </button>
          </div>
          <div class="text-bone grid h-max w-1/4 gap-8 self-center" :aria-hidden="swap">
            <h3 class="text-heading font-[APCasual]">New Here?</h3>
            <p>Create an account to unlock the full potential of Kitchen Command Center</p>
            <button
              class="bg-bone text-onyx w-max cursor-pointer justify-self-center rounded-2xl px-4 py-2"
              @click="swap = !swap"
              type="button"
            >
              Sign Up
            </button>
          </div>
        </div>
      </div>
    </div>
  </section>
</template>
