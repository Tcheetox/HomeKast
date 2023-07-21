export default function Conditional({ test, value, values = [], children }) {
  if (value) values.push(value)
  return values.some(e => e === test) ? children : null
}
