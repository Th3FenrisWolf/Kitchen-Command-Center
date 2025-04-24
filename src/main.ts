import { createApp } from 'vue'
import { createPinia } from 'pinia'
import { library } from '@fortawesome/fontawesome-svg-core'
import { faGripLines, faTrash, faSquarePen } from '@fortawesome/free-solid-svg-icons'
import router from '~/router'
import { initializeFirebase } from '~/utilities/firebase'
import App from '~/App.vue'

library.add(faGripLines, faTrash, faSquarePen)

const app = createApp(App)
app.use(createPinia())
app.use(router)

initializeFirebase().then(() => {
  app.mount('body')
})
