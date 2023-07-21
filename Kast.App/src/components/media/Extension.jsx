import React from 'react'

import './extension.scoped.scss'
import { Col, Row } from 'react-bootstrap'
import { Details } from '../../assets/icons'
import useTimespan from '../../hooks/useTimespan'
import Conditional from '../../hoc/Conditional'

export default function Extension({ media }) {
  const duration = useTimespan(media.length)

  return (
    <div className='extension'>
      <Row>
        <span className='title'>{media.name}</span>
      </Row>
      <Row className='metadata'>
        <Col className='timeAndRate'>
          <Row>{duration}</Row>
          <Row>{media.popularity ? `${media.popularity.toFixed(1)}/10` : null}</Row>
        </Col>
        <Col className='resolutionAndEpisode'>
          <Row>
            <Conditional test={media.resolution} values={[35, 36]}>
              <div className='resolution'>{media.resolution === 35 ? '720p' : '1080p'}</div>
            </Conditional>
          </Row>
          <Row>4 Episodes</Row>
        </Col>
        <Col className='more'>
          <Details />
        </Col>
      </Row>
    </div>
  )
}
