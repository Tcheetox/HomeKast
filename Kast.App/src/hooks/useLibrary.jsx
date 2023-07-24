import { useContextConsumer } from '../AppContext'

import useQuery from './useQuery'

export default function useLibrary() {
  const { refetch, isFetching } = useQuery('get-library')

  return {
    library: useContextConsumer('library') ?? [],
    search: useContextConsumer('search'),
    refetch,
    isFetching,
  }
}
