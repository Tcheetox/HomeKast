export default function Conditional({ test, value, values, children }) {
  if (value === undefined && values === undefined) return test === true ? children : null

  values = values ?? []
  if (value) values.push(value)
  return values.some(e => e === test) ? children : null
}
