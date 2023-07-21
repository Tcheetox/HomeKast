import { useQueryClient } from '@tanstack/react-query'

export default function useQuery(key) {
  const queryClient = useQueryClient()
  return { refetch: () => queryClient.refetchQueries({ queryKey: key }) }
}
