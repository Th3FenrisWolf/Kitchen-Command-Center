<script setup lang="ts">
  import { ref, watch } from 'vue'
  import { cx } from '~/Utilities/CX'
  // import { parseQueryString } from '~/Utilities/QueryFunctions'
  import InputField from '~/Components/Forms/InputField.vue'
  import { useResourceStrings } from '~/Utilities/UseStrings'

  const props = defineProps<{
    returnUrl?: string
    defaultUserName?: string
    defaultPassword?: string
    defaultRememberMe?: boolean
    antiforgeryToken?: string
    resourceStrings?: Record<string, string>
  }>()

  // rs() is still used for attribute bindings (placeholders) where a <ResourceString>
  // component can't be inlined. provide() inside useResourceStrings exposes the
  // dict to <ResourceString> descendants for text content.
  const rs = useResourceStrings(props.resourceStrings, 'Login')

  const swap = ref(false)
  const isSignIn = ref(true)

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
  }

  watch(swap, () => {
    formError.value = null
    setTimeout(() => {
      clearForm()
      isSignIn.value = !isSignIn.value
    }, 250)
  })
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
        <h2 class="text-heading"><ResourceString :k="isSignIn ? 'SignIn' : 'SignUp'" /></h2>

        <p :class="cx('overflow-hidden text-maroon transition-all duration-500', formError ? 'h-8' : 'h-0')">
          {{ formError }}
        </p>

        <form :action="isSignIn ? '/Account/Login' : '/Account/Register'" method="post" class="grid grow-0 gap-8">
          <input type="hidden" name="__RequestVerificationToken" :value="props.antiforgeryToken" />
          <input type="hidden" name="ReturnUrl" :value="props.returnUrl" />

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
            <ResourceString k="RememberMe" />
          </label>

          <button class="w-max cursor-pointer justify-self-center rounded-2xl bg-base px-4 py-2 text-bone" type="submit">
            <ResourceString :k="isSignIn ? 'SignIn' : 'SignUp'" />
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
            <h3 class="font-[APCasual] text-heading"><ResourceString k="HaveAccount" /></h3>
            <p><ResourceString k="HaveAccountDescription" /></p>
            <button
              class="w-max cursor-pointer justify-self-center rounded-2xl bg-bone px-4 py-2 text-onyx"
              @click="swap = !swap"
              type="button"
            >
              <ResourceString k="SignIn" />
            </button>
          </div>
          <div class="grid h-max w-1/4 gap-8 self-center text-bone" :aria-hidden="swap">
            <h3 class="font-[APCasual] text-heading"><ResourceString k="NewHere" /></h3>
            <p><ResourceString k="NewHereDescription" /></p>
            <button
              class="w-max cursor-pointer justify-self-center rounded-2xl bg-bone px-4 py-2 text-onyx"
              @click="swap = !swap"
              type="button"
            >
              <ResourceString k="SignUp" />
            </button>
          </div>
        </div>
      </div>
    </div>
  </section>
</template>
