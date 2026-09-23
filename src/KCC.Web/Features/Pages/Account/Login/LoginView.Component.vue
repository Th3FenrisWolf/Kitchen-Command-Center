<!-- #region LoginView Component Properties -->
<script lang="ts">
  import { ref, useId } from 'vue'
  import InputField from '~/Components/Forms/InputField.vue'
  import { ResourceString, provideResourceStrings } from '~/Components/ResourceStrings'
  import { post } from '~/Utilities/Api'

  /**
   * Sign-in and registration, flipping between the two on one sheet.
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
  import KccSheet from '~/Components/Sheet/KccSheet.vue'
  const props = defineProps<LoginViewProps>()

  const rs = provideResourceStrings(props.resourceStrings, 'Login')

  const isSignIn = ref(true)
  const isSubmitting = ref(false)

  const userName = ref(props.defaultUserName ?? '')
  const email = ref('')
  const password = ref(props.defaultPassword ?? '')
  const passwordConfirmation = ref('')
  const rememberMe = ref(props.defaultRememberMe ?? false)
  const formError = ref<string | null>(null)

  const rememberMeId = `${useId()}-remember-me`

  const switchMode = () => {
    isSignIn.value = !isSignIn.value
    userName.value = ''
    email.value = ''
    password.value = ''
    passwordConfirmation.value = ''
    formError.value = null
  }

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
  <KccSheet crisp :tear="4" pad="clamp(24px, 7.5vw, 48px)" icon="fa-duotone fa-key" class="mx-auto my-12 w-full max-w-md">
    <template #label><ResourceString :for="isSignIn ? 'SignIn' : 'SignUp'" /></template>

    <!-- The sheet's label carries the mode in print; the heading carries it in the document. -->
    <h2 class="sr-only">{{ isSignIn ? rs('SignIn') : rs('SignUp') }}</h2>

    <form class="flex flex-col gap-6" @submit.prevent="handleSubmit">
      <!-- The placeholder is the field's name here: the page has no printed labels to bind to. -->
      <InputField
        v-model="userName"
        required
        type="text"
        name="UserName"
        autocomplete="username"
        :placeholder="rs('UsernamePlaceholder')"
        :aria-label="rs('UsernamePlaceholder')"
      />

      <InputField
        v-if="!isSignIn"
        v-model="email"
        required
        type="email"
        name="Email"
        autocomplete="email"
        :placeholder="rs('EmailPlaceholder')"
        :aria-label="rs('EmailPlaceholder')"
      />

      <InputField
        v-model="password"
        required
        type="password"
        name="Password"
        :autocomplete="isSignIn ? 'current-password' : 'new-password'"
        :placeholder="rs('PasswordPlaceholder')"
        :aria-label="rs('PasswordPlaceholder')"
      />

      <InputField
        v-if="!isSignIn"
        v-model="passwordConfirmation"
        required
        type="password"
        name="PasswordConfirmation"
        autocomplete="new-password"
        :placeholder="rs('ConfirmPasswordPlaceholder')"
        :aria-label="rs('ConfirmPasswordPlaceholder')"
      />

      <ul v-if="isSignIn" class="kcc-check">
        <li class="cursor-pointer">
          <input :id="rememberMeId" v-model="rememberMe" type="checkbox" class="sr-only" name="RememberMe" value="true" />
          <label :for="rememberMeId" class="kcc-box" :class="{ 'kcc-box--on': rememberMe }"></label>
          <label :for="rememberMeId"><ResourceString for="RememberMe" /></label>
        </li>
      </ul>

      <p v-if="formError" class="kcc-well kcc-well--danger kcc-kick" role="alert">{{ formError }}</p>

      <Button type="submit" :disabled="isSubmitting">
        <ResourceString :for="isSignIn ? 'SignIn' : 'SignUp'" />
      </Button>
    </form>

    <div class="mt-12">
      <ResourceString :for="isSignIn ? 'NewHere' : 'HaveAccount'" as="p" class="kcc-kick" />
      <ResourceString
        :for="isSignIn ? 'NewHereDescription' : 'HaveAccountDescription'"
        as="p"
        class="kcc-body text-ink-soft"
      />

      <Button variant="text" class="mt-6" @click="switchMode">
        <ResourceString :for="isSignIn ? 'SignUp' : 'SignIn'" />
      </Button>
    </div>
  </KccSheet>
</template>
