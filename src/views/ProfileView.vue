<script setup lang="ts">
import { storeToRefs } from 'pinia'
import useUserStore from '~/store/user'
import { signOut } from '~/utilities/auth'

const userStore = useUserStore()
const { user, isAuthenticated } = storeToRefs(userStore)
</script>

<template>
  <div>
    <h1>Profile</h1>
    <p v-if="isAuthenticated">You are signed in as {{ user?.email }}</p>
    <p v-else>You are not signed in.</p>
    <button v-if="isAuthenticated" class="btn btn-primary" @click="signOut()">Sign Out</button>
  </div>
</template>
