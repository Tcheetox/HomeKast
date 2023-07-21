import React from 'react'

export default function Case({ value, children, onClick }) {
  if (onClick) return <div onClick={onClick}>{children}</div>
  return <>{children}</>
}
