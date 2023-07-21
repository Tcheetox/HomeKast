import { useContextConsumer } from '../AppContext'

export default function useCaster() {
  const preferredCaster = useContextConsumer('preferredCaster')
  if (preferredCaster) return preferredCaster
  const casters = useContextConsumer('casters') ?? []
  return casters.length > 0 ? casters[0] : null
}
