import React, { useState } from 'react'

import './gallery.scoped.scss'
import Episode from './Episode'

export default function Gallery({ episodes }) {
  const [slider, setSlider] = useState({ offset: 0, className: 'left-locked' })

  const left = () =>
    setSlider(s => {
      const newOffset = s.offset - 1
      if (newOffset <= 0) return { offset: 0, className: 'left-locked' }
      return { offset: newOffset, className: '' }
    })

  const right = () =>
    setSlider(s => {
      const newOffset = s.offset + 1
      const maxOffset = episodes.length - 1
      if (newOffset >= maxOffset) return { offset: maxOffset, className: 'right-locked' }
      return { offset: newOffset, className: '' }
    })

  return (
    <div className={`gallery ${slider.className}`}>
      <div className='navigation'>
        <div className='left' onClick={left} />
      </div>
      <div className='overflow'>
        <div className='gallery-content' style={{ transform: `translateX(${-180 * slider.offset}px)` }}>
          {episodes.map(e => (
            <Episode key={e.id} media={e} />
          ))}
          <div className='separator' />
        </div>
      </div>
      <div className='navigation'>
        <div className='right' onClick={right} />
      </div>
    </div>
  )
}
