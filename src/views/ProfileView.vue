<script setup lang="ts">
import { FontAwesomeIcon } from '@fortawesome/vue-fontawesome'
import { faSquareCheck, faSquarePen } from '@fortawesome/free-solid-svg-icons'
import { updateProfile } from 'firebase/auth'
import { ref } from 'vue'
import InputField from '~/components/shared/InputField.vue'
import router from '~/router'
import useUserStore from '~/store/user'
import { signOut } from '~/utilities/auth'
import { cx } from '~/utilities/cx'

const { user, isAuthenticated } = useUserStore()
if (!isAuthenticated) router.push({ name: 'Login' })

const isEdit = ref(false)
const displayName = ref(user?.displayName ?? '')
const memberSince = ref(user?.metadata.creationTime ?? '')
const email = ref(user?.email ?? '')
const fakePassword = '********************'

const updateUsername = async () => {
  isEdit.value = false
  if (!user) return

  updateProfile(user, {
    displayName: displayName.value,
  })
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
      <label class="relative grid gap-2">
        <span>Display Name:</span>
        <InputField
          :readonly="!isEdit"
          v-model="displayName"
          :class="cx(!isEdit && 'cursor-default !outline-none')"
        />
        <FontAwesomeIcon
          size="lg"
          :icon="isEdit ? faSquareCheck : faSquarePen"
          class="color-base absolute bottom-6 right-3 z-10 translate-y-1/2 cursor-pointer"
          @click="() => (isEdit ? updateUsername() : (isEdit = true))"
        />
      </label>
      <label class="grid gap-2">
        <span>Member Since:</span>
        <InputField readonly v-model="memberSince" class="cursor-default !outline-none" />
      </label>
      <button
        type="button"
        class="bg-base text-bone w-max cursor-pointer justify-self-center rounded-2xl px-4 py-2"
        @click="handleSignOut"
      >
        Sign Out
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
    <div class="shadow-primary grid gap-4 rounded-3xl p-8">Content TBD</div>
  </section>
</template>
