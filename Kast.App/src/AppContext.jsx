import React, { useContext, createContext, useState } from 'react'

import { WithLibrary, WithCasters, WithSettings, WithConversions } from './providers/'
const AppContextConsumer = createContext()
const AppContextUpdater = createContext()

// TODO: much of this stuff probably requires useCallback and useMemo?

export function useContextConsumer(key = null) {
  const consumer = useContext(AppContextConsumer)
  if (consumer === undefined) throw new Error(`useContextConsumer must be used within a ContextProvider`)
  return key ? consumer[key] : consumer
}

export const useContextUpdater = (key = null) => {
  const updater = useContext(AppContextUpdater)
  if (!updater) throw new Error(`useContextUpdater must be used within a ContextProvider`)
  return key ? v => updater(_v => ({ ..._v, [key]: v })) : updater
}

export const AppContextProvider = props => {
  const [context, setContext] = useState({ library: [], casters: [] })

  return (
    <AppContextUpdater.Provider value={setContext}>
      <WithLibrary />
      <WithCasters />
      <WithSettings />
      <WithConversions />
      <AppContextConsumer.Provider value={context}>{props.children}</AppContextConsumer.Provider>
    </AppContextUpdater.Provider>
  )
}
