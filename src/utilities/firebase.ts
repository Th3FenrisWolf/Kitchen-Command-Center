// Import the functions you need from the SDKs you need
import { initializeApp } from 'firebase/app'
import { getAuth } from 'firebase/auth'
import { getFirestore } from 'firebase/firestore'
import { getAnalytics } from 'firebase/analytics'

// Your web app's Firebase configuration
// For Firebase JS SDK v7.20.0 and later, measurementId is optional
const firebaseConfig = {
  apiKey: 'AIzaSyC6dBTOaj_kHM28AS56e3zEVBuj4e29_AU',
  authDomain: 'kitchen-command-center-ce0a9.firebaseapp.com',
  projectId: 'kitchen-command-center-ce0a9',
  storageBucket: 'kitchen-command-center-ce0a9.firebasestorage.app',
  messagingSenderId: '1013496184825',
  appId: '1:1013496184825:web:852ff85d0176d279c44728',
  measurementId: 'G-K8D1JL6E4B',
}

// Initialize Firebase
const app = initializeApp(firebaseConfig)
const auth = getAuth(app)
const db = getFirestore(app)
const analytics = getAnalytics(app)

export { auth, db, analytics }
