import React, { useState, useEffect } from 'react'

import './library.scoped.scss'
import { search as searchEngine } from 'ss-search'
import Loading from '../loading/Loading'
import useLibrary from '../../hooks/useLibrary'
import Media from '../media/Media'
import InfiniteScroll from 'react-infinite-scroller'

export default function Library() {
  const perPage = 18
  const { library, search, isFetching, isLoading } = useLibrary()
  const [searchResults, setSearchResults] = useState([])
  const [shownResults, setShownResults] = useState([])

  useEffect(() => setShownResults(p => searchResults.slice(0, p.length)), [searchResults, setShownResults])

  useEffect(() => {
    const flattenedLibrary = library.map(e => e[0])
    const results = searchEngine(flattenedLibrary, ['name', 'description'], search)
    const searchedResults = results.map(e => {
      const idx = flattenedLibrary.findIndex(i => i.name === e.name)
      return library[idx]
    })
    setSearchResults(searchedResults)
  }, [search, library, setSearchResults])

  return isLoading() ? (
    <Loading />
  ) : (
    <InfiniteScroll
      className='library'
      pageStart={0}
      loadMore={() => setShownResults(p => searchResults.slice(0, p.length + perPage))}
      hasMore={shownResults.length < searchResults.length}
    >
      {shownResults.map(c => (
        <Media key={c[0].name} collection={c} />
      ))}
    </InfiniteScroll>
  )
}
