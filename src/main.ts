import { createApp } from 'vue'
import { createPinia } from 'pinia'
import { library } from '@fortawesome/fontawesome-svg-core'
import { faSquarePen } from '@fortawesome/free-solid-svg-icons'
import router from '~/router'
import { initializeFirebase } from '~/utilities/firebase'
import App from '~/App.vue'

library.add(faSquarePen)

const app = createApp(App)
app.use(createPinia())
app.use(router)

initializeFirebase().then(() => {
  app.mount('body')
})
