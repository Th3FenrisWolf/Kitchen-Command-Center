import { doc, getDoc, getDocs, addDoc, collection } from 'firebase/firestore'
import { FirebaseError } from 'firebase/app'
import { db } from '~/utilities/firebase'
import type { DBRecipe } from '~/types/recipe'

export const addRecipe = async (recipe: DBRecipe) => {
  try {
    delete recipe.id
    const docRef = await addDoc(collection(db, 'recipes'), recipe)
    console.log('Recipe added with ID:', docRef.id)
  } catch (error: unknown) {
    if (error instanceof FirebaseError) {
      console.error('Firebase Error:', error.code, error.message)
    } else {
      console.error('Unknown Error:', error)
    }
  }
}

export const getRecipe = async (recipeId: string): Promise<DBRecipe | null> => {
  try {
    const recipeRef = doc(db, 'recipes', recipeId)
    const recipeSnap = await getDoc(recipeRef)

    if (recipeSnap.exists()) {
      console.log('Recipe found:', recipeSnap.data())
      return recipeSnap.data() as DBRecipe
    } else {
      console.log('No such recipe!')
      return null
    }
  } catch (error: unknown) {
    if (error instanceof Error) {
      console.error('Error fetching recipe:', error.message)
    } else {
      console.error('Unknown error occurred:', error)
    }
    return null
  }
}

export const getAllRecipes = async (): Promise<DBRecipe[]> => {
  try {
    const querySnapshot = await getDocs(collection(db, 'recipes'))
    return querySnapshot.docs.map((doc) => ({
      id: doc.id,
      ...doc.data(),
    })) as DBRecipe[]
  } catch (error: unknown) {
    if (error instanceof Error) {
      console.error('Error fetching recipes:', error.message)
    } else {
      console.error('Unknown error occurred:', error)
    }
    return []
  }
}
