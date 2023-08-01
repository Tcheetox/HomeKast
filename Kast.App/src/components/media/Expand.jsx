import React, { useState } from 'react'

import './expand.scoped.scss'
import { Details } from '../../assets/icons'
import { Button, Modal } from 'react-bootstrap'

export default function Expand({}) {
  const [show, setShow] = useState(false)
  return (
    <>
      <Button className='btn-unstyle' onClick={() => setShow(p => !p)}>
        <Details />
      </Button>
      <Modal className='expand' show={show} onHide={() => setShow(false)} size='xl' centered>
        <Modal.Header closeButton>
          <Modal.Title>Modal heading</Modal.Title>
        </Modal.Header>
        <Modal.Body>Woohoo, you are reading this text in a modal!</Modal.Body>
      </Modal>
    </>
  )
}
