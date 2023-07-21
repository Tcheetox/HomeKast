import React from 'react'

import './footer.scoped.scss'
import { Container, Row } from 'react-bootstrap'

export default function Footer() {
  return (
    <footer className='footer'>
      <Container fluid>
        <Row>
          <hr />
        </Row>
        <Row>
          <small>
            © 2023 Copyright{' '}
            <a href='https://thekecha.com' target='_blank'>
              Kévin Renier
            </a>
            . All rights reserved.
          </small>
        </Row>
      </Container>
    </footer>
  )
}
