<script setup lang="ts">
import { ref, computed, watch } from 'vue'
import cx from '~/utilities/cx'
import { signIn, signUp } from '~/utilities/auth'
import router from '~/router'

const swap = ref(false)
const isSignIn = ref(true)
const signText = computed(() => (isSignIn.value ? 'Sign In' : 'Sign Up'))

watch(swap, () => {
  formError.value = null
  setTimeout(() => {
    clearForm()
    isSignIn.value = !isSignIn.value
  }, 150)
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
  formError.value = null
  const response = await sendForm(isSignIn.value ? signIn : signUp)
  clearForm()
  if (response) {
    router.push({ name: 'Dashboard' })
  }
}
</script>

<template>
  <div class="h-full w-full place-items-center content-center">
    <div class="shadow-primary relative flex w-3/4 overflow-hidden rounded-3xl">
      <div
        :class="
          cx(
            'ease-normal relative left-[0%] flex basis-[60%] flex-col gap-4 p-12 text-center transition-all duration-300',
            swap && 'left-[40%]',
          )
        "
      >
        <h1>{{ signText }}</h1>

        <p
          :class="
            cx(
              'text-maroon ease-normal overflow-hidden transition-all duration-300',
              formError ? 'h-8' : 'h-0',
            )
          "
        >
          {{ formError }}
        </p>

        <form class="grid grow-0 gap-8" @submit.prevent="handleSubmit">
          <input
            class="bg-bone-dark placeholder:text-onyx-light rounded-2xl px-4 py-2"
            required
            type="email"
            v-model="email"
            autocomplete="email"
            placeholder="Email"
          />

          <input
            class="bg-bone-dark placeholder:text-onyx-light rounded-2xl px-4 py-2"
            required
            type="password"
            v-model="password"
            autocomplete="current-password"
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
            'bg-base ease-normal relative right-[0%] basis-[40%] justify-items-center p-12 transition-all duration-300',
            swap && 'right-[60%]',
          )
        "
      >
        <h2 class="text-heading text-bone">Test</h2>
        <button
          class="bg-bone text-onyx w-max cursor-pointer justify-self-center rounded-2xl px-4 py-2"
          @click="swap = !swap"
          type="button"
        >
          Switch
        </button>
      </div>
    </div>
  </div>
</template>
