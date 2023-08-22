import { useQuery } from '@tanstack/react-query'
import { useContextUpdater } from '../AppContext'

export default function WithCasters() {
  const update = useContextUpdater('casters')
  const interval = parseInt(process.env.REACT_APP_RECEIVERS_REFETCH_INTERVAL)

  useQuery({
    queryKey: ['get-casters'],
    queryFn: () =>
      fetch(`${process.env.REACT_APP_BACKEND_URI}/cast`, {
        headers: {
          'access-control-allow-origin': '*',
        },
      }).then(value => value.json()),
    onSuccess: data => update(data),
    initialData: [],
    refetchInterval: interval,
    refetchIntervalInBackground: false,
    refetchOnWindowFocus: false,
  })

  return null
}
