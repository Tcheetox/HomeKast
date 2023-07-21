import React from 'react'

import './library.scoped.scss'
import useLibrary from '../../hooks/useLibrary'
import Media from '../media/Media'

export default function Library() {
  const library = useLibrary()

  return (
    <>
      {library.map(c => (
        <Media key={c[0].name} collection={c} />
      ))}
    </>
  )
}
