import React from 'react'

import './mediaExtension.scoped.scss'
import { Col, Row } from 'react-bootstrap'
import Expand from './Expand'
import useTimespan from '../../hooks/useTimespan'
import Conditional from '../../hoc/Conditional'

export default function MediaExtension({ collection, show, setShow }) {
  const media = collection[0]
  const serie = media.type !== 'Movie'
  const duration = useTimespan(media.length)

  return (
    <div className='extension' onClick={e => e.stopPropagation()}>
      <Row>
        <span className='title'>{media.name}</span>
      </Row>
      <Row className='metadata'>
        <Col className='timeAndRate'>
          <Row>
            <Conditional test={!serie}>{duration}</Conditional>
          </Row>
          <Row>{media.popularity ? `${media.popularity.toFixed(1)}/10` : null}</Row>
        </Col>
        <Col className='resolutionAndEpisode'>
          <Row>
            <Conditional test={media.resolution} values={[35, 36]}>
              <div className='resolution'>{media.resolution === 35 ? '720p' : '1080p'}</div>
            </Conditional>
          </Row>
          <Row>
            <Conditional test={serie}>{`${collection.length} Episodes`}</Conditional>
          </Row>
        </Col>
        <Col className='more'>
          <Expand collection={collection} serie={serie} show={show} setShow={setShow} />
        </Col>
      </Row>
    </div>
  )
}
