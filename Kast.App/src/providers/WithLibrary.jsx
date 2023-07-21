import { useQuery } from '@tanstack/react-query'
import { useContextUpdater } from '../AppContext'

export default function WithLibrary() {
  const update = useContextUpdater('library')
  const interval = parseInt(process.env.REACT_APP_LIBRARY_REFETCH_INTERVAL)

  useQuery({
    queryKey: ['get-library'],
    queryFn: () =>
      fetch(`${process.env.REACT_APP_BACKEND_URI}/media`, {
        headers: {
          'access-control-allow-origin': '*',
        },
      }).then(value => value.json()),
    onSuccess: data => update(data),
    initialData: [],
    // refetchInterval: refetch,
  })

  return null
}
