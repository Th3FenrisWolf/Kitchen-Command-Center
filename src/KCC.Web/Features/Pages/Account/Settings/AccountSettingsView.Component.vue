<script setup lang="ts">
  import { ref } from 'vue'
  import InputField from '~/Components/Forms/InputField.vue'
  import { ResourceString, useResourceStrings } from '~/Components/ResourceStrings'
  import SmallHero from '~/Widgets/Hero/SmallHero.Component.vue'
  import AppLink from '~/Components/Links/AppLink.Component.vue'

  const props = defineProps<{
    firstName: string
    lastName: string
    email: string
    backUrl: string
    logoutUrl: string
    antiforgeryToken?: string
    resourceStrings?: Record<string, string>
  }>()

  const rs = useResourceStrings(props.resourceStrings, 'Account')

  const firstName = ref(props.firstName ?? '')
  const lastName = ref(props.lastName ?? '')

  const currentPassword = ref('')
  const newPassword = ref('')
  const confirmPassword = ref('')

  const profileSubmitting = ref(false)
  const profileMessage = ref<{ ok: boolean; text: string } | null>(null)

  const passwordSubmitting = ref(false)
  const passwordMessage = ref<{ ok: boolean; text: string } | null>(null)

  const post = async (url: string, body: Record<string, unknown>) => {
    const response = await fetch(url, {
      method: 'POST',
      headers: {
        'Content-Type': 'application/json',
        ...(props.antiforgeryToken ? { RequestVerificationToken: props.antiforgeryToken } : {}),
      },
      body: JSON.stringify(body),
    })
    return (await response.json()) as { success: boolean; errors?: string[] }
  }

  const saveProfile = async () => {
    profileMessage.value = null
    profileSubmitting.value = true
    try {
      const data = await post('/api/profile', { firstName: firstName.value, lastName: lastName.value })
      profileMessage.value = data.success
        ? { ok: true, text: rs('ProfileSaved') }
        : { ok: false, text: data.errors?.join(' ') ?? rs('UnexpectedError') }
    } catch {
      profileMessage.value = { ok: false, text: rs('UnexpectedError') }
    } finally {
      profileSubmitting.value = false
    }
  }

  const changePassword = async () => {
    passwordMessage.value = null
    if (newPassword.value !== confirmPassword.value) {
      passwordMessage.value = { ok: false, text: rs('PasswordsDoNotMatch') }
      return
    }
    passwordSubmitting.value = true
    try {
      const data = await post('/api/profile/password', {
        currentPassword: currentPassword.value,
        newPassword: newPassword.value,
      })
      if (data.success) {
        passwordMessage.value = { ok: true, text: rs('PasswordUpdated') }
        currentPassword.value = ''
        newPassword.value = ''
        confirmPassword.value = ''
      } else {
        passwordMessage.value = { ok: false, text: data.errors?.join(' ') ?? rs('UnexpectedError') }
      }
    } catch {
      passwordMessage.value = { ok: false, text: rs('UnexpectedError') }
    } finally {
      passwordSubmitting.value = false
    }
  }
</script>

<template>
  <section class="grid w-full">
    <SmallHero dark>
      <template #title>
        <ResourceString for="AccountSettings" />
      </template>

      <template #action-button>
        <AppLink :href="backUrl" class="rounded-3xl bg-bone px-4 py-2 text-xl text-onyx">
          <i class="fa-solid fa-arrow-left fa-sm"></i>
          <ResourceString for="BackToProfile" />
        </AppLink>
      </template>
    </SmallHero>

    <div class="mb-8 flex gap-8 max-lg:flex-col">
      <!-- Profile card -->
      <form class="grid basis-full gap-4 rounded-3xl bg-bone p-6 shadow-primary" @submit.prevent="saveProfile">
        <h2 class="text-xl"><ResourceString for="Profile" /></h2>

        <label class="grid gap-2">
          <span class="text-sm text-onyx-light"><ResourceString for="FirstName" /></span>
          <InputField type="text" v-model="firstName" name="FirstName" autocomplete="given-name" />
        </label>
        <label class="grid gap-2">
          <span class="text-sm text-onyx-light"><ResourceString for="LastName" /></span>
          <InputField type="text" v-model="lastName" name="LastName" autocomplete="family-name" />
        </label>

        <label class="grid gap-2">
          <span class="text-sm text-onyx-light">
            <ResourceString for="Email" /> &middot; <ResourceString for="EmailComingSoon" />
          </span>
          <InputField readonly type="email" :model-value="email" class="cursor-default opacity-70" />
        </label>

        <p v-if="profileMessage" :class="profileMessage.ok ? 'text-green' : 'text-maroon'">{{ profileMessage.text }}</p>

        <div class="flex justify-between">
          <span class="text-xs text-onyx-light"><ResourceString for="EmailComingSoonNote" /></span>
          <button
            :disabled="profileSubmitting"
            type="submit"
            class="cursor-pointer justify-self-end rounded-2xl bg-surface-500 px-4 py-2 text-bone disabled:opacity-50"
          >
            <ResourceString for="SaveChanges" />
          </button>
        </div>
      </form>

      <!-- Password card -->
      <form class="grid basis-full gap-4 rounded-3xl bg-bone p-6 shadow-primary" @submit.prevent="changePassword">
        <h2 class="text-xl"><ResourceString for="ChangePassword" /></h2>

        <label class="grid gap-2">
          <span class="text-sm text-onyx-light"><ResourceString for="CurrentPassword" /></span>
          <InputField required type="password" v-model="currentPassword" autocomplete="current-password" />
        </label>
        <label class="grid gap-2">
          <span class="text-sm text-onyx-light"><ResourceString for="NewPassword" /></span>
          <InputField required type="password" v-model="newPassword" autocomplete="new-password" />
        </label>
        <label class="grid gap-2">
          <span class="text-sm text-onyx-light"><ResourceString for="ConfirmNewPassword" /></span>
          <InputField required type="password" v-model="confirmPassword" autocomplete="new-password" />
        </label>

        <p v-if="passwordMessage" :class="passwordMessage.ok ? 'text-green' : 'text-maroon'">{{ passwordMessage.text }}</p>

        <button
          :disabled="passwordSubmitting"
          type="submit"
          class="cursor-pointer justify-self-end rounded-2xl bg-surface-500 px-4 py-2 text-bone disabled:opacity-50"
        >
          <ResourceString for="UpdatePassword" />
        </button>
      </form>
    </div>

    <a :href="logoutUrl" class="justify-self-end rounded-2xl border border-red px-4 py-2 text-red">
      <ResourceString for="SignOut" />
    </a>
  </section>
</template>
