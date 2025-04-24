import { createApp } from 'vue'
import { createPinia } from 'pinia'
import router from '~/router'
import { initializeFirebase } from '~/utilities/firebase'
import App from '~/App.vue'

const app = createApp(App)
app.use(createPinia())
app.use(router)

initializeFirebase().then(() => {
  app.mount('body')
})
