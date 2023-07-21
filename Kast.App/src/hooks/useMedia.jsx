import { useMutation } from '@tanstack/react-query'

export default function useMedia(id, receiverId = null) {
  const startConversion = useMutation(
    () =>
      fetch(`${process.env.REACT_APP_BACKEND_URI}/conversion/${id}/start`, {
        method: 'POST',
      }),
    {
      onError: error => console.error(error),
    }
  )

  const stopConversion = useMutation(
    () =>
      fetch(`${process.env.REACT_APP_BACKEND_URI}/conversion/${id}/stop`, {
        method: 'POST',
      }),
    {
      onError: error => console.error(error),
    }
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
