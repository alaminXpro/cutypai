import { configureStore } from '@reduxjs/toolkit'
import { cutypai, setGlobalStore } from './index'

export const store = configureStore({
  reducer: {
    cutypai: cutypai.reducer,
  },
  middleware: getDefaultMiddleware => getDefaultMiddleware({ serializableCheck: false })
})

// Initialize global store for API client
setGlobalStore({ getState: store.getState, dispatch: store.dispatch })

export type RootState = ReturnType<typeof store.getState>
export type AppDispatch = typeof store.dispatch