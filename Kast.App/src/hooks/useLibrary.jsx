import { useContextConsumer } from '../AppContext'

import useQuery from './useQuery'

export default function useLibrary() {
  const { refetch, isFetching } = useQuery('get-library')
  const library = useContextConsumer('library') ?? []

  return {
    library,
    search: useContextConsumer('search'),
    refetch,
    isFetching,
    isLoading: isFetching() && library.length === 0,
  }
}
