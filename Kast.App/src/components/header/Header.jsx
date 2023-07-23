import React from 'react'

import { Container, Row, Col } from 'react-bootstrap/'
import './header.scoped.scss'
import Logo from '../../assets/kre.svg'
import Search from '../search/Search'
import Conversion from '../conversion/Conversion'
import Settings from '../settings/Settings'

export default function Header() {
  return (
    <Container className='header' fluid>
      <Row>
        <Col className='d-flex application'>
          <a href='/'>
            <Logo className='logo' />
          </a>
          <div className='title'>HomeKast</div>
        </Col>
        <Col className='d-flex' />
        <Col className='d-flex options'>
          <Search />
          <Conversion />
          <Settings />
        </Col>
      </Row>
    </Container>
  )
}
