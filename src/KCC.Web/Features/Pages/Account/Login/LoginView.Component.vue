<!-- #region LoginView Component Properties -->
<script lang="ts">
  import { ref, watch } from 'vue'
  import InputField from '~/Components/Forms/InputField.vue'
  import { ResourceString, provideResourceStrings } from '~/Components/ResourceStrings'
  import { post } from '~/Utilities/Api'

  /**
   * Sign-in and registration, flipping between the two on one card.
   */
  export default {
    name: 'LoginView',
  }

  export interface LoginViewProps {
    /**
     * Where to send the member after a successful sign-in.
     */
    returnUrl?: string
    /**
     * Prefills the sign-in form, so a failed server-side post comes back populated.
     */
    defaultUserName?: string
    defaultPassword?: string
    defaultRememberMe?: boolean
    /**
     * Localized text for this page, keyed by unprefixed name and provided to descendants.
     */
    resourceStrings?: Record<string, string>
  }
</script>
<!-- #endregion -->

<script setup lang="ts">
  import Button from '~/Components/Button/Button.vue'
  const props = defineProps<LoginViewProps>()

  const rs = provideResourceStrings(props.resourceStrings, 'Login')

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

    const result = await post<{ redirectUrl?: string }>(endpoint, body)
    isSubmitting.value = false

    if (!result.success) {
      formError.value = result.errorMessage
      return
    }

    window.location.href = result.data.redirectUrl ?? '/'
  }
</script>

<template>
  <section class="no-margin fixed top-[50dvh] left-[50dvw] grid w-3/4 -translate-x-1/2 -translate-y-1/2 place-items-center">
    <div v-ink="{ kind: 'sheet' }" class="sk-sheet relative flex w-3/4 overflow-hidden" style="--pad: 0">
      <div
        :class="[
          'relative left-[0%] flex basis-[60%] flex-col justify-center gap-4 p-12 text-center transition-all duration-500',
          swap && 'left-[40%]',
        ]"
      >
        <h2 class="text-4.5xl"><ResourceString :for="isSignIn ? 'SignIn' : 'SignUp'" /></h2>

        <p :class="['overflow-hidden text-danger-ink transition-all duration-500', formError ? 'h-8' : 'h-0']">
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

          <Button class="justify-self-center" :disabled="isSubmitting" type="submit">
            <ResourceString :for="isSignIn ? 'SignIn' : 'SignUp'" />
          </Button>
        </form>
      </div>

      <div
        :class="[
          'relative right-[0%] grid basis-[40%] justify-items-center overflow-hidden bg-paper-2 p-12 text-center transition-all duration-500',
          swap && 'right-[60%]',
        ]"
      >
        <div
          :class="[
            'relative flex h-full w-[400%] justify-between transition-all duration-500',
            swap ? 'left-[150%]' : 'left-[-150%]',
          ]"
        >
          <div class="grid h-max w-1/4 gap-8 self-center text-ink" :aria-hidden="isSignIn">
            <h3 class="font-casual text-4.5xl"><ResourceString for="HaveAccount" /></h3>
            <p><ResourceString for="HaveAccountDescription" /></p>
            <Button class="justify-self-center" variant="ghost" @click="swap = !swap">
              <ResourceString for="SignIn" />
            </Button>
          </div>
          <div class="grid h-max w-1/4 gap-8 self-center text-ink" :aria-hidden="swap">
            <h3 class="font-casual text-4.5xl"><ResourceString for="NewHere" /></h3>
            <p><ResourceString for="NewHereDescription" /></p>
            <Button class="justify-self-center" variant="ghost" @click="swap = !swap">
              <ResourceString for="SignUp" />
            </Button>
          </div>
        </div>
      </div>
    </div>
  </section>
</template>
