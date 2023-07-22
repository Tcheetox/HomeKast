import React, { useState, useEffect } from 'react'

import './library.scoped.scss'
import useLibrary from '../../hooks/useLibrary'
import Media from '../media/Media'
import InfiniteScroll from 'react-infinite-scroller'

export default function Library() {
  const perPage = 18
  const library = useLibrary()
  const [shown, setShown] = useState([])
  useEffect(() => setShown(p => library.slice(0, p.length)), [library, setShown])

  return (
    <InfiniteScroll
      className='library'
      pageStart={0}
      loadMore={() => setShown(p => library.slice(0, p.length + perPage))}
      hasMore={shown.length < library.length}
    >
      {shown.map(c => (
        <Media key={c[0].name} collection={c} />
      ))}
    </InfiniteScroll>
  )
}
