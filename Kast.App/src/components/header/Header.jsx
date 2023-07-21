import React from 'react'

import { Container, Row, Col } from 'react-bootstrap/'
import './header.scoped.scss'
import Logo from '../../assets/kre.svg'
import Search from '../search/Search'

export default function Header() {
  return (
    <Container className='header' fluid>
      <Row>
        <Col className='application'>
          <a href='/'>
            <Logo className='logo' />
          </a>
          <div className='title'>HomeKast</div>
        </Col>
        <Col>
          <Search />
        </Col>
        <Col>3 of 3</Col>
      </Row>
    </Container>
  )
}
