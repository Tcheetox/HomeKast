import { useContextConsumer } from '../AppContext'

export default function useCaster() {
  const casters = useContextConsumer('casters') ?? []
  return casters
}
