import { useContextConsumer } from '../AppContext'

export default function useSettings() {
  return useContextConsumer('settings') ?? {}
}
