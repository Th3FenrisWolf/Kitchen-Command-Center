<!-- #region AccountSettingsView Component Properties -->
<script lang="ts">
  import { computed, ref, useId } from 'vue'
  import Field from '~/Components/Forms/Field.vue'
  import InputField from '~/Components/Forms/InputField.vue'
  import { ResourceString, provideResourceStrings } from '~/Components/ResourceStrings'
  import SmallHero from '~/Widgets/Hero/SmallHero.Component.vue'
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
  import KccSheet from '~/Components/Sheet/KccSheet.vue'
  const props = defineProps<AccountSettingsViewProps>()

  const rs = provideResourceStrings(props.resourceStrings, 'Account')

  // 7.5vw reaches the 48px a sheet read this closely wants at 640px, and floors at the sheet's own 24px.
  const SHEET_PAD = 'clamp(24px, 7.5vw, 48px)'

  const firstName = ref(props.firstName ?? '')
  const lastName = ref(props.lastName ?? '')

  const currentPassword = ref('')
  const newPassword = ref('')
  const confirmPassword = ref('')

  const profileSubmitting = ref(false)
  const profileMessage = ref<{ ok: boolean; text: string } | null>(null)

  const passwordSubmitting = ref(false)
  const passwordMessage = ref<{ ok: boolean; text: string } | null>(null)

  const uid = useId()
  const ids = {
    firstName: `${uid}-first-name`,
    lastName: `${uid}-last-name`,
    email: `${uid}-email`,
    currentPassword: `${uid}-current-password`,
    newPassword: `${uid}-new-password`,
    confirmPassword: `${uid}-confirm-password`,
  }

  const backHref = computed(() => props.backUrl.stripTilde())

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
  <SmallHero dark>
    <template #title>
      <ResourceString for="AccountSettings" />
    </template>

    <template #action-button>
      <Button as="a" :href="backHref" variant="ghost">
        <i class="fa-duotone fa-arrow-left" aria-hidden="true"></i><ResourceString for="BackToProfile" />
      </Button>
    </template>
  </SmallHero>

  <div class="mt-6 grid gap-x-7 gap-y-9 lg:grid-cols-2">
    <KccSheet crisp :tear="2" :pad="SHEET_PAD" icon="fa-duotone fa-user">
      <template #label><ResourceString for="Profile" /></template>

      <!-- The sheet's label carries the group's name in print; the heading carries it in the document. -->
      <h2 class="sr-only">{{ rs('Profile') }}</h2>

      <form class="flex flex-col gap-6" @submit.prevent="saveProfile">
        <Field :control-id="ids.firstName">
          <template #label><ResourceString for="FirstName" /></template>
          <InputField :id="ids.firstName" v-model="firstName" type="text" name="FirstName" autocomplete="given-name" />
        </Field>

        <Field :control-id="ids.lastName">
          <template #label><ResourceString for="LastName" /></template>
          <InputField :id="ids.lastName" v-model="lastName" type="text" name="LastName" autocomplete="family-name" />
        </Field>

        <Field :control-id="ids.email">
          <template #label><ResourceString for="Email" /></template>
          <template #hint><ResourceString for="EmailComingSoon" /> · <ResourceString for="EmailComingSoonNote" /></template>
          <template #default="{ describedby }">
            <InputField
              :id="ids.email"
              :model-value="email"
              :aria-describedby="describedby"
              readonly
              type="email"
              class="cursor-default"
            />
          </template>
        </Field>

        <p
          v-if="profileMessage"
          class="kcc-well kcc-kick"
          :class="profileMessage.ok ? 'kcc-well--success' : 'kcc-well--danger'"
          :role="profileMessage.ok ? 'status' : 'alert'"
        >
          {{ profileMessage.text }}
        </p>

        <div class="flex justify-end">
          <Button type="submit" :disabled="profileSubmitting">
            <ResourceString for="SaveChanges" />
          </Button>
        </div>
      </form>
    </KccSheet>

    <KccSheet crisp :tear="5" :pad="SHEET_PAD" icon="fa-duotone fa-key">
      <template #label><ResourceString for="ChangePassword" /></template>

      <h2 class="sr-only">{{ rs('ChangePassword') }}</h2>

      <form class="flex flex-col gap-6" @submit.prevent="changePassword">
        <Field :control-id="ids.currentPassword" required>
          <template #label><ResourceString for="CurrentPassword" /></template>
          <InputField
            :id="ids.currentPassword"
            v-model="currentPassword"
            required
            type="password"
            autocomplete="current-password"
          />
        </Field>

        <Field :control-id="ids.newPassword" required>
          <template #label><ResourceString for="NewPassword" /></template>
          <InputField :id="ids.newPassword" v-model="newPassword" required type="password" autocomplete="new-password" />
        </Field>

        <Field :control-id="ids.confirmPassword" required>
          <template #label><ResourceString for="ConfirmNewPassword" /></template>
          <InputField
            :id="ids.confirmPassword"
            v-model="confirmPassword"
            required
            type="password"
            autocomplete="new-password"
          />
        </Field>

        <p
          v-if="passwordMessage"
          class="kcc-well kcc-kick"
          :class="passwordMessage.ok ? 'kcc-well--success' : 'kcc-well--danger'"
          :role="passwordMessage.ok ? 'status' : 'alert'"
        >
          {{ passwordMessage.text }}
        </p>

        <div class="flex justify-end">
          <Button type="submit" :disabled="passwordSubmitting">
            <ResourceString for="UpdatePassword" />
          </Button>
        </div>
      </form>
    </KccSheet>
  </div>

  <KccSheet crisp :tear="6" :pad="SHEET_PAD" icon="fa-duotone fa-right-from-bracket" class="mt-9">
    <template #label><ResourceString for="SignOut" /></template>

    <h2 class="sr-only">{{ rs('SignOut') }}</h2>

    <div class="flex justify-end">
      <Button as="a" :href="logoutUrl" variant="ghost">
        <ResourceString for="SignOut" />
      </Button>
    </div>
  </KccSheet>
</template>
