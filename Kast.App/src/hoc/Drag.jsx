import React, { useState, useRef, useCallback } from 'react'

export default function Drag({ initialPosition = { x: 0, y: 0 }, children }) {
  const dragRef = useRef(null)
  const [handle, setHandle] = useState({
    position: initialPosition,
    offset: initialPosition,
    visibility: 'initial',
  })

  const compute = (e, h) => {
    const value = { x: e.clientX - h.offset.x, y: e.clientY - h.offset.y }
    if (value.x < 0) value.x = 0
    if (value.y < 0) value.y = 0
    const childWidth = dragRef.current?.scrollWidth
    const maxWidth = document?.documentElement?.clientWidth
    if (childWidth && maxWidth && childWidth + value.x > maxWidth) value.x = maxWidth - childWidth
    return value
  }

  const drag = useCallback(_ => setHandle(h => ({ ...h, visibility: 'hidden' })), [setHandle])
  const dragStart = useCallback(e => setHandle(h => ({ ...h, offset: compute(e, h) })), [setHandle, compute])
  const dragEnd = useCallback(
    e =>
      setHandle(h => {
        const endPosition = compute(e, h)
        return {
          offset: endPosition,
          position: endPosition,
          visibility: 'initial',
        }
      }),
    [setHandle, compute]
  )

  return (
    <div
      ref={dragRef}
      className='drag'
      onDragStart={dragStart}
      onDrag={drag}
      onDragEnd={dragEnd}
      style={{
        position: 'absolute',
        transform: `translate(${handle.position.x}px, ${handle.position.y}px)`,
        zIndex: '100',
        width: 'fit-content',
        visibility: handle.visibility,
      }}
      draggable
    >
      {children}
    </div>
  )
}
