import React from 'react'

import Container from 'react-bootstrap/Container'
import './home.scoped.scss'
import Library from '../library/Library'

export default function Home() {
  return (
    <Container className='home' fluid>
      <Library />
    </Container>
  )
}
