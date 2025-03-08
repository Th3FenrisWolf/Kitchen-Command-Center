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
    console.log('User signed up:', userCredential.user)
  } catch (error) {
    if (error instanceof FirebaseError) {
      console.error('Error signing up:', error.message)
    } else {
      console.error('Error signing up:', error)
    }
  }
}

export const signIn = async (email: string, password: string) => {
  try {
    const userCredential = await signInWithEmailAndPassword(auth, email, password)
    console.log('User signed in:', userCredential.user)
  } catch (error) {
    if (error instanceof FirebaseError) {
      console.error('Error signing in:', error.message)
    } else {
      console.error('Error signing in:', error)
    }
  }
}

export const updateUser = async (user: User) => {
  try {
    await updateCurrentUser(auth, user)
    console.log('User updated:', user)
  } catch (error) {
    if (error instanceof FirebaseError) {
      console.error('Error updating user:', error.message)
    } else {
      console.error('Error updating user:', error)
    }
  }
}

export const signOut = async () => {
  try {
    await signOutFirebase(auth)
    console.log('User signed out')
  } catch (error) {
    if (error instanceof FirebaseError) {
      console.error('Error signing out:', error.message)
    } else {
      console.error('Error signing out:', error)
    }
  }
}

setPersistence(auth, browserSessionPersistence)

onAuthStateChanged(auth, (user) => {
  const { setUser, clearUser } = useUserStore()
  if (user) setUser(user)
  else clearUser()
})
