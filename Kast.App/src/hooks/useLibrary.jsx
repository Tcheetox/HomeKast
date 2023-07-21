import { useContextConsumer } from '../AppContext'

export default function useLibrary() {
  return useContextConsumer('library') ?? []
}
