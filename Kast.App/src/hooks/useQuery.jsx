import { useQueryClient, useIsFetching } from '@tanstack/react-query'

export default function useQuery(key) {
  const queryClient = useQueryClient()

  return {
    refetch: () => queryClient.refetchQueries({ queryKey: [key] }),
    isFetching: () => useIsFetching({ queryKey: [key] }) > 0,
  }
}
