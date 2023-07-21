export default function useTimespan(timespan) {
  const timeSpanParts = timespan.split(':')
  const hours = parseInt(timeSpanParts[0])
  const minutes = parseInt(timeSpanParts[1])
  const timeParts = timeSpanParts[2].split('.')
  const seconds = parseInt(timeParts[0])
  const milliseconds = parseInt(timeParts[1]) / 10000

  const totalMilliseconds = hours * 60 * 60 * 1000 + minutes * 60 * 1000 + seconds * 1000 + milliseconds

  const date = new Date(totalMilliseconds)
  const hoursFormatted = date.getUTCHours().toString()

  const minutesFormatted = date
    .getUTCMinutes()
    .toString()
    .padStart(2, '0')
  const secondsFormatted = date
    .getUTCSeconds()
    .toString()
    .padStart(2, '0')

  return `${hoursFormatted}:${minutesFormatted}:${secondsFormatted}`
}
