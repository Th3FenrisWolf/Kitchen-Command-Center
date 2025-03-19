<script setup lang="ts">
import { updateProfile } from 'firebase/auth'
import { ref } from 'vue'
import InputField from '~/components/shared/InputField.vue'
import router from '~/router'
import useUserStore from '~/store/user'
import { signOut } from '~/utilities/auth'

const { user, isAuthenticated } = useUserStore()
if (!isAuthenticated) router.push({ name: 'Login' })

const isEdit = ref(false)
const displayName = ref(user?.displayName ?? '')
const memberSince = ref(user?.metadata.creationTime ?? '')
const email = ref(user?.email ?? '')
const fakePassword = '********************'

const updateUserProfile = async () => {
  if (!user) return

  updateProfile(user, {
    displayName: displayName.value,
  }).then(() => (isEdit.value = false))
}

const handleSignOut = async () => {
  await signOut()
  router.push({ name: 'Dashboard' })
}
</script>

<template>
  <section class="grid grid-cols-2 grid-rows-2 gap-8">
    <div class="shadow-primary row-span-2 grid gap-4 rounded-3xl p-8">
      <img
        class="w-full rounded-2xl object-cover"
        src="https://picsum.photos/400/300"
        alt="Profile Picture"
        loading="lazy"
        height="300"
        width="400"
      />
      <label class="grid gap-2">
        <span>Display Name:</span>
        <InputField :readonly="!isEdit" v-model="displayName" />
      </label>
      <label class="grid gap-2">
        <span>Member Since:</span>
        <InputField readonly v-model="memberSince" />
      </label>
      <button
        type="button"
        class="bg-base text-bone mt-4 w-max cursor-pointer justify-self-center rounded-2xl px-4 py-2"
        @click="() => (isEdit ? updateUserProfile() : (isEdit = true))"
      >
        {{ isEdit ? 'Save' : 'Edit Profile' }}
      </button>
    </div>
    <div class="shadow-primary place-content-center rounded-3xl p-8">
      <div class="grid w-full gap-4">
        <label class="grid w-full gap-2">
          <span>Email:</span>
          <InputField :readonly="!isEdit" v-model="email" />
        </label>
        <label class="grid w-full gap-2">
          <span>Password:</span>
          <InputField readonly type="password" v-model="fakePassword" />
        </label>
      </div>
    </div>
    <div class="shadow-primary grid gap-4 rounded-3xl p-8">test</div>
  </section>
  <section class="flex justify-center">
    <button
      type="button"
      class="bg-base text-bone w-max cursor-pointer rounded-2xl px-4 py-2"
      @click="handleSignOut"
    >
      Sign Out
    </button>
  </section>
</template>
