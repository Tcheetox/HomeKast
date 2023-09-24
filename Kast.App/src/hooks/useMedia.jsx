import { useMutation } from '@tanstack/react-query'
import { useLibrary, useConversions, useCaster } from './'

export default function useMedia(id, receiverId = null) {
  const library = useLibrary()
  const conversions = useConversions()
  receiverId ??= useCaster().id

  const startConversion = useMutation(() =>
    fetch(`${process.env.REACT_APP_BACKEND_URI}/conversion/${id}/start`, {
      method: 'POST',
    }).then(async r => {
      if (r.ok) {
        library.refetch()
        conversions.refetch()
        return
      }
      throw Error(JSON.stringify(await r.json()))
    })
  )

  const stopConversion = useMutation(() =>
    fetch(`${process.env.REACT_APP_BACKEND_URI}/conversion/${id}/stop`, {
      method: 'POST',
    }).then(async r => {
      if (r.ok) {
        library.refetch()
        conversions.refetch()
        return
      }
      throw Error(JSON.stringify(await r.json()))
    })
  )

  const playMedia = useMutation(() => {
    if (receiverId) {
      fetch(`${process.env.REACT_APP_BACKEND_URI}/cast/${receiverId}/start/${id}`, {
        method: 'POST',
      })
      return
    }
    throw new Error(`playMedia requires a valid receiverId`)
  })

  return {
    startConversion: startConversion.mutate,
    stopConversion: stopConversion.mutate,
    play: playMedia.mutate,
  }
}
