export default function Switch({ test, children, fallback }) {
  children = Array.isArray(children) ? children : [children]
  return children.find(child => child.props.value === test) ?? children.find(child => child.type.name === 'DefaultCase') ?? fallback
}
