import React from 'react'

import './conversion.scoped.scss'
import { Popover, Row, Col, ProgressBar } from 'react-bootstrap'
import { Stop } from '../../assets/icons'
import { useMedia } from '../../hooks'

export default function ConversionPopover({ conversions }) {
  const current = conversions.find(i => i.status === 'Streamable') ?? conversions.find(i => i.status === 'Converting') ?? conversions[0]
  const { stopConversion } = useMedia(current.id)

  return (
    <>
      <Popover.Header>{`Converting ${conversions.length} item(s)...`}</Popover.Header>
      <Popover.Body>
        <hr />
        <Row className='row-control'>
          <Col className='col-name'>{current.name}</Col>
          <Col className='col-icon'>
            <Stop onClick={stopConversion} />
          </Col>
        </Row>
        <ProgressBar animated now={current.progress} label={`${current.progress}%`} min={0} max={100} />
      </Popover.Body>
    </>
  )
}
