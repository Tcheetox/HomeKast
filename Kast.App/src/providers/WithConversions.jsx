import { useQuery } from '@tanstack/react-query'
import { useContextUpdater } from '../AppContext'

export default function WithConversions() {
  const updateConversions = useContextUpdater('conversions')
  const interval = parseInt(process.env.REACT_APP_CONVERSIONS_REFETCH_INTERVAL)

  useQuery({
    queryKey: ['get-conversions'],
    queryFn: () =>
      fetch(`${process.env.REACT_APP_BACKEND_URI}/conversion`, {
        headers: {
          'access-control-allow-origin': '*',
        },
      }).then(value => value.json()),
    onSuccess: updateConversions,
    initialData: [],
    // refetchInterval: refetch,
  })

  return null
}
