<!-- #region AccountSettingsView Component Properties -->
<script lang="ts">
  import { ref } from 'vue'
  import InputField from '~/Components/Forms/InputField.vue'
  import { ResourceString, provideResourceStrings } from '~/Components/ResourceStrings'
  import SmallHero from '~/Widgets/Hero/SmallHero.Component.vue'
  import AppLink from '~/Components/Links/AppLink.Component.vue'
  import { post } from '~/Utilities/Api'

  /**
   * Two independent forms: the member's profile details, and a password change.
   */
  export default {
    name: 'AccountSettingsView',
  }

  export interface AccountSettingsViewProps {
    /**
     * Seeds the profile form; edits post to the API rather than round-tripping the page.
     */
    firstName: string
    lastName: string
    /**
     * Shown read-only — the form offers no way to change it.
     */
    email: string
    backUrl: string
    logoutUrl: string
    /**
     * Localized text for this page, keyed by unprefixed name and provided to descendants.
     */
    resourceStrings?: Record<string, string>
  }
</script>
<!-- #endregion -->

<script setup lang="ts">
  import Button from '~/Components/Button/Button.vue'
  import Sheet from '~/Components/Sketch/Sheet.vue'
  const props = defineProps<AccountSettingsViewProps>()

  const rs = provideResourceStrings(props.resourceStrings, 'Account')

  const firstName = ref(props.firstName ?? '')
  const lastName = ref(props.lastName ?? '')

  const currentPassword = ref('')
  const newPassword = ref('')
  const confirmPassword = ref('')

  const profileSubmitting = ref(false)
  const profileMessage = ref<{ ok: boolean; text: string } | null>(null)

  const passwordSubmitting = ref(false)
  const passwordMessage = ref<{ ok: boolean; text: string } | null>(null)

  const saveProfile = async () => {
    profileMessage.value = null
    profileSubmitting.value = true

    const result = await post('/api/profile', { firstName: firstName.value, lastName: lastName.value })
    profileSubmitting.value = false

    profileMessage.value = result.success ? { ok: true, text: rs('ProfileSaved') } : { ok: false, text: result.errorMessage }
  }

  const changePassword = async () => {
    passwordMessage.value = null
    if (newPassword.value !== confirmPassword.value) {
      passwordMessage.value = { ok: false, text: rs('PasswordsDoNotMatch') }
      return
    }
    passwordSubmitting.value = true

    const result = await post('/api/profile/password', {
      currentPassword: currentPassword.value,
      newPassword: newPassword.value,
    })
    passwordSubmitting.value = false

    if (!result.success) {
      passwordMessage.value = { ok: false, text: result.errorMessage }
      return
    }

    passwordMessage.value = { ok: true, text: rs('PasswordUpdated') }
    currentPassword.value = ''
    newPassword.value = ''
    confirmPassword.value = ''
  }
</script>

<template>
  <section class="grid w-full">
    <SmallHero dark>
      <template #title>
        <ResourceString for="AccountSettings" />
      </template>

      <template #action-button>
        <AppLink :href="backUrl" class="rounded-3xl bg-paper-2 px-4 py-2 text-xl text-ink">
          <i class="fa-solid fa-arrow-left fa-sm"></i>
          <ResourceString for="BackToProfile" />
        </AppLink>
      </template>
    </SmallHero>

    <div class="mb-8 flex gap-8 max-lg:flex-col">
      <!-- Profile card -->
      <Sheet as="form" pad="md" class="grid basis-full gap-4" @submit.prevent="saveProfile">
        <h2 class="text-xl"><ResourceString for="Profile" /></h2>

        <label class="grid gap-2">
          <span class="sk-lbl"><ResourceString for="FirstName" /></span>
          <InputField type="text" v-model="firstName" name="FirstName" autocomplete="given-name" />
        </label>
        <label class="grid gap-2">
          <span class="sk-lbl"><ResourceString for="LastName" /></span>
          <InputField type="text" v-model="lastName" name="LastName" autocomplete="family-name" />
        </label>

        <label class="grid gap-2">
          <span class="sk-lbl"> <ResourceString for="Email" /> &middot; <ResourceString for="EmailComingSoon" /> </span>
          <InputField readonly type="email" :model-value="email" class="cursor-default opacity-70" />
        </label>

        <p v-if="profileMessage" :class="profileMessage.ok ? 'text-success-ink' : 'text-danger-ink'">
          {{ profileMessage.text }}
        </p>

        <div class="flex justify-between">
          <span class="text-xs text-ink-soft"><ResourceString for="EmailComingSoonNote" /></span>
          <Button :disabled="profileSubmitting" type="submit" class="justify-self-end">
            <ResourceString for="SaveChanges" />
          </Button>
        </div>
      </Sheet>

      <!-- Password card -->
      <Sheet as="form" pad="md" class="grid basis-full gap-4" @submit.prevent="changePassword">
        <h2 class="text-xl"><ResourceString for="ChangePassword" /></h2>

        <label class="grid gap-2">
          <span class="sk-lbl"><ResourceString for="CurrentPassword" /></span>
          <InputField required type="password" v-model="currentPassword" autocomplete="current-password" />
        </label>
        <label class="grid gap-2">
          <span class="sk-lbl"><ResourceString for="NewPassword" /></span>
          <InputField required type="password" v-model="newPassword" autocomplete="new-password" />
        </label>
        <label class="grid gap-2">
          <span class="sk-lbl"><ResourceString for="ConfirmNewPassword" /></span>
          <InputField required type="password" v-model="confirmPassword" autocomplete="new-password" />
        </label>

        <p v-if="passwordMessage" :class="passwordMessage.ok ? 'text-success-ink' : 'text-danger-ink'">
          {{ passwordMessage.text }}
        </p>

        <Button :disabled="passwordSubmitting" type="submit" class="justify-self-end">
          <ResourceString for="UpdatePassword" />
        </Button>
      </Sheet>
    </div>

    <a
      :href="logoutUrl"
      v-ink="{ kind: 'button', color: 'var(--color-danger-ink)' }"
      class="sk-btn sk-btn--ghost justify-self-end text-danger-ink"
    >
      <ResourceString for="SignOut" />
    </a>
  </section>
</template>
