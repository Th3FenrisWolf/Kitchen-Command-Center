import {
  createUserWithEmailAndPassword,
  signInWithEmailAndPassword,
  updateCurrentUser,
  signOut as signOutFirebase,
  setPersistence,
  browserSessionPersistence,
  onAuthStateChanged,
  type User,
} from 'firebase/auth'
import { FirebaseError } from 'firebase/app'
import { auth } from './firebase'
import useUserStore from '~/store/user'

export const signUp = async (email: string, password: string) => {
  try {
    const userCredential = await createUserWithEmailAndPassword(auth, email, password)
    return userCredential.user
      ? { success: true, message: 'Sign up successful' }
      : { success: false, message: 'Sign up failed' }
  } catch (error) {
    if (error instanceof FirebaseError) {
      return { success: false, message: error.message }
    }
    return { success: false, message: 'An unknown error occurred' }
  }
}

export const signIn = async (email: string, password: string) => {
  try {
    const userCredential = await signInWithEmailAndPassword(auth, email, password)
    return userCredential.user
      ? { success: true, message: 'Sign in successful' }
      : { success: false, message: 'Sign in failed' }
  } catch (error) {
    if (error instanceof FirebaseError) {
      return { success: false, message: error.message }
    }
    return { success: false, message: 'An unknown error occurred' }
  }
}

export const updateUser = async (user: User) => {
  try {
    await updateCurrentUser(auth, user)
    return { success: true, message: 'User updated successfully' }
  } catch (error) {
    if (error instanceof FirebaseError) {
      return { success: false, message: error.message }
    }
    return { success: false, message: 'An unknown error occurred' }
  }
}

export const signOut = async () => {
  try {
    await signOutFirebase(auth)
    return { success: true, message: 'Sign out successful' }
  } catch (error) {
    if (error instanceof FirebaseError) {
      return { success: false, message: error.message }
    }
    return { success: false, message: 'An unknown error occurred' }
  }
}

setPersistence(auth, browserSessionPersistence)

onAuthStateChanged(auth, (user) => {
  const { setUser, clearUser } = useUserStore()
  if (user) setUser(user)
  else clearUser()
})
