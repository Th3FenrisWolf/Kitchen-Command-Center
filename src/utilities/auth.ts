import {
  createUserWithEmailAndPassword,
  signInWithEmailAndPassword,
  signOut as signOutUser,
} from 'firebase/auth'
import { FirebaseError } from 'firebase/app'
import { auth } from './firebase'

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

export const signOut = async () => {
  try {
    await signOutUser(auth)
    console.log('User signed out')
  } catch (error) {
    if (error instanceof FirebaseError) {
      console.error('Error signing out:', error.message)
    } else {
      console.error('Error signing out:', error)
    }
  }
}
