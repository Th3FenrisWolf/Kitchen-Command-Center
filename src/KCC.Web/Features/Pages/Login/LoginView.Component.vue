<script setup lang="ts">
import { ref, computed, watch } from 'vue'
import cx from '~/Utilities/CX'
// import { parseQueryString } from '~/Utilities/QueryFunctions'
import InputField from '~/Components/Forms/InputField.vue'

const props = defineProps<{
  returnUrl?: string
  defaultEmail?: string
  defaultPassword?: string
  defaultRememberMe?: boolean
}>()

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

const email = ref(props.defaultEmail ?? '')
const password = ref(props.defaultPassword ?? '')
const rememberMe = ref(props.defaultRememberMe ?? false)
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
  <section class="no-margin fixed top-[50dvh] left-[50dvw] grid w-3/4 -translate-x-1/2 -translate-y-1/2 place-items-center">
    <div class="relative flex w-3/4 overflow-hidden rounded-3xl shadow-primary">
      <div
        :class="
          cx(
            'relative left-[0%] flex basis-[60%] flex-col justify-center gap-4 p-12 text-center transition-all duration-500',
            swap && 'left-[40%]',
          )
        "
      >
        <h2 class="text-heading">{{ signText }}</h2>

        <p :class="cx('overflow-hidden text-maroon transition-all duration-500', formError ? 'h-8' : 'h-0')">
          {{ formError }}
        </p>

        <form class="grid grow-0 gap-8" @submit.prevent="handleSubmit">
          <InputField required type="email" v-model="email" autocomplete="email" placeholder="Email" />

          <InputField
            required
            type="password"
            v-model="password"
            :autocomplete="!isSignIn ? 'new-password' : 'current-password'"
            placeholder="Password"
          />

          <button class="w-max cursor-pointer justify-self-center rounded-2xl bg-base px-4 py-2 text-bone" type="submit">
            {{ signText }}
          </button>
        </form>
      </div>

      <div
        :class="
          cx(
            'relative right-[0%] grid basis-[40%] justify-items-center overflow-hidden bg-base p-12 text-center transition-all duration-500',
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
          <div class="grid h-max w-1/4 gap-8 self-center text-bone" :aria-hidden="isSignIn">
            <h3 class="font-[APCasual] text-heading">Have an Account?</h3>
            <p>Sign in to continue commanding your kitchen</p>
            <button
              class="w-max cursor-pointer justify-self-center rounded-2xl bg-bone px-4 py-2 text-onyx"
              @click="swap = !swap"
              type="button"
            >
              Sign In
            </button>
          </div>
          <div class="grid h-max w-1/4 gap-8 self-center text-bone" :aria-hidden="swap">
            <h3 class="font-[APCasual] text-heading">New Here?</h3>
            <p>Create an account to unlock the full potential of Kitchen Command Center</p>
            <button
              class="w-max cursor-pointer justify-self-center rounded-2xl bg-bone px-4 py-2 text-onyx"
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
