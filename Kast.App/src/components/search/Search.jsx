import React from 'react'

import { useContextUpdater } from '../../AppContext'
import useLibrary from '../../hooks/useLibrary'
import { Form } from 'react-bootstrap/'
import './search.scoped.scss'
import Magnifying from '../../assets/icons/magnifying.svg'

export default function Search() {
  const { search } = useLibrary()
  const setSearch = useContextUpdater('search')

  return (
    <div className='search'>
      <Form.Control
        aria-label='Search'
        placeholder='Search...'
        value={search}
        onChange={e => setSearch(e.target.value)}
        type='string'
        aria-describedby='search'
        size='sm'
        className='shadow-none'
        maxLength={100}
      />
      <Magnifying />
    </div>
  )
}
