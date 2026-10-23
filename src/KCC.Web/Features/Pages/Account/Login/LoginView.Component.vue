<script setup lang="ts">
  import { useId } from 'vue'
  import InputField from '~/Components/Forms/InputField.vue'
  import { ResourceString, provideResourceStrings } from '~/Components/ResourceStrings'
  import Button from '~/Components/Button/Button.vue'
  import KccSheet from '~/Components/Sheet/KccSheet.vue'
  import { useLoginForm, type LoginField } from './useLoginForm'

  export interface LoginViewProps {
    returnUrl?: string
    register?: boolean
    defaultUserName?: string
    defaultPassword?: string
    defaultRememberMe?: boolean
    resourceStrings?: Record<string, string>
  }

  const props = defineProps<LoginViewProps>()

  const rs = provideResourceStrings(props.resourceStrings, 'Login')

  const {
    isSignIn,
    isSubmitting,
    userName,
    email,
    password,
    passwordConfirmation,
    rememberMe,
    formError,
    fieldsInError,
    switchMode,
    handleSubmit,
  } = useLoginForm(props)

  const formId = useId()
  const rememberMeId = `${formId}-remember-me`
  const errorId = `${formId}-error`

  const inError = (field: LoginField) => fieldsInError.value.includes(field)
  const describedBy = (field: LoginField) => (inError(field) ? errorId : undefined)
</script>

<template>
  <KccSheet
    wash-key="login"
    margin
    crisp
    :tear="4"
    pad="clamp(24px, 7.5vw, 48px)"
    icon="fa-duotone fa-key"
    class="mx-auto my-rule-2 w-full max-w-md"
  >
    <template #label><ResourceString :for="isSignIn ? 'SignIn' : 'SignUp'" /></template>

    <h2 class="sr-only">{{ isSignIn ? rs('SignIn') : rs('SignUp') }}</h2>

    <form class="flex flex-col gap-y-rule-1" @submit.prevent="handleSubmit">
      <!-- The placeholder is the field's name here: the page has no printed labels to bind to. -->
      <div class="flex min-h-rule-2 flex-col">
        <InputField
          v-model="userName"
          required
          type="text"
          name="UserName"
          autocomplete="username"
          :placeholder="rs('UsernamePlaceholder')"
          :aria-label="rs('UsernamePlaceholder')"
          :error="inError('UserName')"
          :aria-describedby="describedBy('UserName')"
        />
      </div>

      <div v-if="!isSignIn" class="flex min-h-rule-2 flex-col">
        <InputField
          v-model="email"
          required
          type="email"
          name="Email"
          autocomplete="email"
          :placeholder="rs('EmailPlaceholder')"
          :aria-label="rs('EmailPlaceholder')"
        />
      </div>

      <div class="flex min-h-rule-2 flex-col">
        <InputField
          v-model="password"
          required
          type="password"
          name="Password"
          :autocomplete="isSignIn ? 'current-password' : 'new-password'"
          :placeholder="rs('PasswordPlaceholder')"
          :aria-label="rs('PasswordPlaceholder')"
          :error="inError('Password')"
          :aria-describedby="describedBy('Password')"
        />
      </div>

      <div v-if="!isSignIn" class="flex min-h-rule-2 flex-col">
        <InputField
          v-model="passwordConfirmation"
          required
          type="password"
          name="PasswordConfirmation"
          autocomplete="new-password"
          :placeholder="rs('ConfirmPasswordPlaceholder')"
          :aria-label="rs('ConfirmPasswordPlaceholder')"
          :error="inError('PasswordConfirmation')"
          :aria-describedby="describedBy('PasswordConfirmation')"
        />
      </div>

      <ul v-if="isSignIn" class="kcc-check">
        <li class="cursor-pointer">
          <input :id="rememberMeId" v-model="rememberMe" type="checkbox" class="sr-only" name="RememberMe" value="true" />
          <label :for="rememberMeId" class="kcc-box" :class="{ 'kcc-box--on': rememberMe }"></label>
          <label :for="rememberMeId"><ResourceString for="RememberMe" /></label>
        </li>
      </ul>

      <p v-if="formError" :id="errorId" class="kcc-well kcc-well--danger kcc-kick" role="alert">{{ formError }}</p>

      <div class="kcc-row">
        <Button type="submit" class="w-full" :disabled="isSubmitting">
          <ResourceString :for="isSignIn ? 'SignIn' : 'SignUp'" />
        </Button>
      </div>
    </form>

    <div class="mt-rule-2">
      <ResourceString :for="isSignIn ? 'NewHere' : 'HaveAccount'" as="p" class="kcc-kick" />
      <ResourceString
        :for="isSignIn ? 'NewHereDescription' : 'HaveAccountDescription'"
        as="p"
        class="kcc-body text-ink-soft"
      />

      <Button variant="text" class="mt-rule-1" @click="switchMode">
        <ResourceString :for="isSignIn ? 'SignUp' : 'SignIn'" />
      </Button>
    </div>
  </KccSheet>
</template>
