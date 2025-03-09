import { doc, getDoc, getDocs, addDoc, collection } from 'firebase/firestore'
import { FirebaseError } from 'firebase/app'
import { db } from '~/utilities/firebase'
import type { Recipe } from '~/types/recipe'

export const addRecipe = async (
  title: string,
  description: string,
  ingredients: string,
  userId: string,
) => {
  try {
    const docRef = await addDoc(collection(db, 'recipes'), {
      title,
      description,
      ingredients,
      createdBy: userId,
    })
    console.log('Recipe added with ID:', docRef.id)
  } catch (error: unknown) {
    if (error instanceof FirebaseError) {
      console.error('Firebase Error:', error.code, error.message)
    } else {
      console.error('Unknown Error:', error)
    }
  }
}

export const getRecipe = async (recipeId: string): Promise<Recipe | null> => {
  try {
    const recipeRef = doc(db, 'recipes', recipeId)
    const recipeSnap = await getDoc(recipeRef)

    if (recipeSnap.exists()) {
      console.log('Recipe found:', recipeSnap.data())
      return recipeSnap.data() as Recipe // Type casting to Recipe
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

export const getAllRecipes = async (): Promise<Recipe[]> => {
  try {
    const querySnapshot = await getDocs(collection(db, 'recipes'))
    const recipes: Recipe[] = querySnapshot.docs.map((doc) => ({
      id: doc.id,
      ...doc.data(),
    })) as Recipe[]

    console.log('All Recipes:', recipes)
    return recipes
  } catch (error: unknown) {
    if (error instanceof Error) {
      console.error('Error fetching recipes:', error.message)
    } else {
      console.error('Unknown error occurred:', error)
    }
    return []
  }
}
