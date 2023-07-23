import React, { useState } from 'react'

import { Form } from 'react-bootstrap/'
import './search.scoped.scss'
import Magnifying from '../../assets/icons/magnifying.svg'

export default function Search() {
  const [search, setSearch] = useState('')

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
        className=' shadow-none'
      />
      <Magnifying />
    </div>
  )
}
