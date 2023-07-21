import React from 'react'

import { InputGroup, Form } from 'react-bootstrap/'
import './search.scoped.scss'

export default function Search() {
  return <Form.Control placeholder='Search' aria-label='Search' aria-describedby='search' />
}
