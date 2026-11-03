<script setup lang="ts">
  import { computed, useId } from 'vue'
  import Field from '~/Components/Forms/Field.vue'
  import InputField from '~/Components/Forms/InputField.vue'
  import { ResourceString, provideResourceStrings } from '~/Components/ResourceStrings'
  import SmallHero from '~/Widgets/Hero/SmallHero.Component.vue'
  import type { RampSetting } from '~/Utilities/Ramp'
  import SignOutForm from '~/Components/Account/SignOutForm.vue'
  import Button from '~/Components/Button/Button.vue'
  import KccSheet from '~/Components/Sheet/KccSheet.vue'
  import SegmentedControl, { type SegmentOption } from '~/Components/Recipe/SegmentedControl.vue'
  import { useRampSetting } from './useRampSetting'
  import { usePasswordForm, useProfileForm, type PasswordField, type ProfileField } from './useSettingsForms'

  export interface AccountSettingsViewProps {
    firstName: string
    lastName: string
    email: string
    ramp: RampSetting
    backUrl: string
    logoutUrl: string
    resourceStrings?: Record<string, string>
  }

  const props = defineProps<AccountSettingsViewProps>()

  const rs = provideResourceStrings(props.resourceStrings, 'Account')

  // 7.5vw reaches the 48px a sheet read this closely wants at 640px, and floors at the sheet's own 24px.
  const SHEET_PAD = 'clamp(24px, 7.5vw, 48px)'

  const {
    firstName,
    lastName,
    submitting: profileSubmitting,
    message: profileMessage,
    fieldsInError: profileFieldsInError,
    save: saveProfile,
  } = useProfileForm({ firstName: props.firstName ?? '', lastName: props.lastName ?? '' }, rs)

  const {
    currentPassword,
    newPassword,
    confirmPassword,
    submitting: passwordSubmitting,
    message: passwordMessage,
    fieldsInError: passwordFieldsInError,
    change: changePassword,
  } = usePasswordForm(rs)

  const { setting: rampSetting, error: rampError, choose: chooseRamp } = useRampSetting(props.ramp)

  const rampOptions: SegmentOption<RampSetting>[] = [
    { value: 'Device', label: rs('AppearanceDevice'), testId: 'ramp-device' },
    { value: 'Light', label: rs('AppearanceLight'), testId: 'ramp-light' },
    { value: 'Dark', label: rs('AppearanceDark'), testId: 'ramp-dark' },
  ]

  const uid = useId()
  const ids = {
    firstName: `${uid}-first-name`,
    lastName: `${uid}-last-name`,
    email: `${uid}-email`,
    currentPassword: `${uid}-current-password`,
    newPassword: `${uid}-new-password`,
    confirmPassword: `${uid}-confirm-password`,
    profileMessage: `${uid}-profile-message`,
    passwordMessage: `${uid}-password-message`,
    rampHint: `${uid}-ramp-hint`,
  }

  const profileInError = (field: ProfileField) => profileFieldsInError.value.includes(field)
  const passwordInError = (field: PasswordField) => passwordFieldsInError.value.includes(field)

  const backHref = computed(() => props.backUrl)
</script>

<template>
  <SmallHero dark class="mt-rule-1">
    <template #title>
      <ResourceString for="AccountSettings" />
    </template>

    <template #action-button>
      <Button as="a" :href="backHref" variant="ghost">
        <i class="fa-duotone fa-arrow-left" aria-hidden="true"></i><ResourceString for="BackToProfile" />
      </Button>
    </template>
  </SmallHero>

  <div class="mt-rule-1 grid gap-x-7 gap-y-rule-1.5 lg:grid-cols-2">
    <KccSheet wash-key="settings-profile" margin crisp :tear="2" :pad="SHEET_PAD" icon="fa-duotone fa-user">
      <template #label><ResourceString for="Profile" /></template>

      <h2 class="sr-only">{{ rs('Profile') }}</h2>

      <form class="flex flex-col gap-y-rule-1" @submit.prevent="saveProfile">
        <Field :control-id="ids.firstName">
          <template #label><ResourceString for="FirstName" /></template>
          <InputField
            :id="ids.firstName"
            v-model="firstName"
            type="text"
            name="FirstName"
            autocomplete="given-name"
            :error="profileInError('FirstName')"
            :aria-describedby="profileInError('FirstName') ? ids.profileMessage : undefined"
          />
        </Field>

        <Field :control-id="ids.lastName">
          <template #label><ResourceString for="LastName" /></template>
          <InputField
            :id="ids.lastName"
            v-model="lastName"
            type="text"
            name="LastName"
            autocomplete="family-name"
            :error="profileInError('LastName')"
            :aria-describedby="profileInError('LastName') ? ids.profileMessage : undefined"
          />
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
          :id="ids.profileMessage"
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

    <KccSheet wash-key="settings-password" margin crisp :tear="5" :pad="SHEET_PAD" icon="fa-duotone fa-key">
      <template #label><ResourceString for="ChangePassword" /></template>

      <h2 class="sr-only">{{ rs('ChangePassword') }}</h2>

      <form class="flex flex-col gap-y-rule-1" @submit.prevent="changePassword">
        <Field :control-id="ids.currentPassword" required>
          <template #label><ResourceString for="CurrentPassword" /></template>
          <InputField
            :id="ids.currentPassword"
            v-model="currentPassword"
            required
            type="password"
            autocomplete="current-password"
            :error="passwordInError('CurrentPassword')"
            :aria-describedby="passwordInError('CurrentPassword') ? ids.passwordMessage : undefined"
          />
        </Field>

        <Field :control-id="ids.newPassword" required>
          <template #label><ResourceString for="NewPassword" /></template>
          <InputField
            :id="ids.newPassword"
            v-model="newPassword"
            required
            type="password"
            autocomplete="new-password"
            :error="passwordInError('NewPassword')"
            :aria-describedby="passwordInError('NewPassword') ? ids.passwordMessage : undefined"
          />
        </Field>

        <Field :control-id="ids.confirmPassword" required>
          <template #label><ResourceString for="ConfirmNewPassword" /></template>
          <InputField
            :id="ids.confirmPassword"
            v-model="confirmPassword"
            required
            type="password"
            autocomplete="new-password"
            :error="passwordInError('ConfirmPassword')"
            :aria-describedby="passwordInError('ConfirmPassword') ? ids.passwordMessage : undefined"
          />
        </Field>

        <p
          v-if="passwordMessage"
          :id="ids.passwordMessage"
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

    <KccSheet
      wash-key="settings-appearance"
      margin
      crisp
      flip
      :tear="1"
      :pad="SHEET_PAD"
      icon="fa-duotone fa-circle-half-stroke"
    >
      <template #label><ResourceString for="Appearance" /></template>

      <h2 class="sr-only">{{ rs('Appearance') }}</h2>

      <div class="flex flex-col gap-y-rule-1">
        <div class="flex flex-col">
          <div class="kcc-row">
            <SegmentedControl
              :model-value="rampSetting"
              :options="rampOptions"
              :aria-label="rs('Appearance')"
              :aria-describedby="ids.rampHint"
              @update:model-value="chooseRamp"
            />
          </div>
          <p :id="ids.rampHint" class="kcc-kick text-ink"><ResourceString for="AppearanceHint" /></p>
        </div>

        <p v-if="rampError" class="kcc-well kcc-well--danger kcc-kick" role="alert">{{ rampError }}</p>
      </div>
    </KccSheet>

    <KccSheet
      wash-key="settings-sign-out"
      margin
      crisp
      flip
      :tear="6"
      :pad="SHEET_PAD"
      icon="fa-duotone fa-right-from-bracket"
    >
      <template #label><ResourceString for="SignOut" /></template>

      <h2 class="sr-only">{{ rs('SignOut') }}</h2>

      <div class="flex justify-end">
        <SignOutForm :action="logoutUrl">
          <Button type="submit" variant="ghost">
            <ResourceString for="SignOut" />
          </Button>
        </SignOutForm>
      </div>
    </KccSheet>
  </div>
</template>
