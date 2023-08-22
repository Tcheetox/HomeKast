import { useQuery } from '@tanstack/react-query'
import { useContextUpdater } from '../AppContext'

export default function WithSettings() {
  const update = useContextUpdater('settings')

  useQuery({
    queryKey: ['get-settings'],
    queryFn: () =>
      fetch(`${process.env.REACT_APP_BACKEND_URI}/settings`, {
        headers: {
          'access-control-allow-origin': '*',
        },
      }).then(value => value.json()),
    onSuccess: data => update(data),
    initialData: {},
    refetchIntervalInBackground: false,
    refetchOnWindowFocus: false,
  })

  return null
}
