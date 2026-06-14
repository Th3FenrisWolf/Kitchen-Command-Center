<script setup lang="ts">
  import { ref, watch } from 'vue'
  import { cx } from '~/Utilities/CX'
  import InputField from '~/Components/Forms/InputField.vue'
  import { ResourceString, useResourceStrings } from '~/Components/ResourceStrings'

  const props = defineProps<{
    returnUrl?: string
    defaultUserName?: string
    defaultPassword?: string
    defaultRememberMe?: boolean
    antiforgeryToken?: string
    resourceStrings?: Record<string, string>
  }>()

  const rs = useResourceStrings(props.resourceStrings, 'Login')

  const swap = ref(false)
  const isSignIn = ref(true)
  const isSubmitting = ref(false)

  const userName = ref(props.defaultUserName ?? '')
  const email = ref('')
  const password = ref(props.defaultPassword ?? '')
  const passwordConfirmation = ref('')
  const rememberMe = ref(props.defaultRememberMe ?? false)
  const formError = ref<string | null>(null)

  const clearForm = () => {
    userName.value = ''
    email.value = ''
    password.value = ''
    passwordConfirmation.value = ''
    formError.value = null
  }

  watch(swap, () => {
    formError.value = null
    setTimeout(() => {
      clearForm()
      isSignIn.value = !isSignIn.value
    }, 250)
  })

  const handleSubmit = async () => {
    formError.value = null

    if (!isSignIn.value && password.value !== passwordConfirmation.value) {
      formError.value = 'Passwords do not match.'
      return
    }

    isSubmitting.value = true
    const endpoint = isSignIn.value ? '/api/account/login' : '/api/account/register'
    const body = isSignIn.value
      ? { userName: userName.value, password: password.value, rememberMe: rememberMe.value, returnUrl: props.returnUrl }
      : { userName: userName.value, email: email.value, password: password.value }

    try {
      const response = await fetch(endpoint, {
        method: 'POST',
        headers: {
          'Content-Type': 'application/json',
          ...(props.antiforgeryToken ? { RequestVerificationToken: props.antiforgeryToken } : {}),
        },
        body: JSON.stringify(body),
      })

      const data = await response.json()

      if (!response.ok || !data.success) {
        formError.value = data.errors?.join(' ') ?? 'Sign in failed.'
        return
      }

      window.location.href = data.redirectUrl ?? '/'
    } catch {
      formError.value = 'An unexpected error occurred.'
    } finally {
      isSubmitting.value = false
    }
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
        <h2 class="text-4.5xl"><ResourceString :for="isSignIn ? 'SignIn' : 'SignUp'" /></h2>

        <p :class="cx('overflow-hidden text-red transition-all duration-500', formError ? 'h-8' : 'h-0')">
          {{ formError }}
        </p>

        <form @submit.prevent="handleSubmit" class="grid grow-0 gap-8">
          <InputField
            required
            type="text"
            v-model="userName"
            autocomplete="username"
            :placeholder="rs('UsernamePlaceholder')"
            name="UserName"
          />

          <InputField
            v-if="!isSignIn"
            required
            type="email"
            v-model="email"
            autocomplete="email"
            :placeholder="rs('EmailPlaceholder')"
            name="Email"
          />

          <InputField
            required
            type="password"
            v-model="password"
            :autocomplete="!isSignIn ? 'new-password' : 'current-password'"
            :placeholder="rs('PasswordPlaceholder')"
            name="Password"
          />

          <InputField
            v-if="!isSignIn"
            required
            type="password"
            v-model="passwordConfirmation"
            autocomplete="new-password"
            :placeholder="rs('ConfirmPasswordPlaceholder')"
            name="PasswordConfirmation"
          />

          <label v-if="isSignIn" class="flex items-center gap-2 justify-self-center">
            <input type="checkbox" v-model="rememberMe" name="RememberMe" value="true" />
            <ResourceString for="RememberMe" />
          </label>

          <button
            :disabled="isSubmitting"
            class="w-max cursor-pointer justify-self-center rounded-2xl bg-surface-500 px-4 py-2 text-bone disabled:opacity-50"
            type="submit"
          >
            <ResourceString :for="isSignIn ? 'SignIn' : 'SignUp'" />
          </button>
        </form>
      </div>

      <div
        :class="
          cx(
            'relative right-[0%] grid basis-[40%] justify-items-center overflow-hidden bg-surface-500 p-12 text-center transition-all duration-500',
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
            <h3 class="font-casual text-4.5xl"><ResourceString for="HaveAccount" /></h3>
            <p><ResourceString for="HaveAccountDescription" /></p>
            <button
              class="w-max cursor-pointer justify-self-center rounded-2xl bg-bone px-4 py-2 text-onyx"
              @click="swap = !swap"
              type="button"
            >
              <ResourceString for="SignIn" />
            </button>
          </div>
          <div class="grid h-max w-1/4 gap-8 self-center text-bone" :aria-hidden="swap">
            <h3 class="font-casual text-4.5xl"><ResourceString for="NewHere" /></h3>
            <p><ResourceString for="NewHereDescription" /></p>
            <button
              class="w-max cursor-pointer justify-self-center rounded-2xl bg-bone px-4 py-2 text-onyx"
              @click="swap = !swap"
              type="button"
            >
              <ResourceString for="SignUp" />
            </button>
          </div>
        </div>
      </div>
    </div>
  </section>
</template>
