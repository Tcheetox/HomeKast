import { useContextConsumer } from '../AppContext'

export default function useLibrary() {
  return {
    library: useContextConsumer('library') ?? [],
    search: useContextConsumer('search'),
  }
}
