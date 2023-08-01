import { useContextConsumer } from '../AppContext'

import useQuery from './useQuery'

export default function useConversions() {
  const { refetch, isFetching } = useQuery('get-conversions')
  const conversions = useContextConsumer('conversions') ?? []

  return {
    conversions,
    isConverting: conversions.length > 0,
    refetch,
    isFetching,
  }
}
