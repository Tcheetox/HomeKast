import React from 'react'

import './expand.scoped.scss'
import notFoundPoster from '../../assets/notfoundPoster.png'
import { Details } from '../../assets/icons'
import { Button, Modal, Row } from 'react-bootstrap'
import { Conditional, Case, Switch, DefaultCase } from '../../hoc/'
import Trigger from './Trigger'
import Gallery from './Gallery'

export default function Expand({ collection, serie, show, setShow }) {
  const media = collection[0]
  const imageSrc = media.hasImage ? `${process.env.REACT_APP_BACKEND_URI}/media/${media.id}/image` : notFoundPoster

  return (
    <>
      <Button className='btn-unstyle' onClick={() => setShow(p => !p)}>
        <Details />
      </Button>
      <Modal className='expand' show={show} onHide={() => setShow(false)} size='xl' centered>
        <Switch test={media.youtubeEmbed}>
          <Case value={null}>
            <img className='poster' src={imageSrc} />
          </Case>
          <DefaultCase>
            <iframe
              className='trailer'
              width='1140'
              height='641'
              src={`${media.youtubeEmbed}?autoplay=1&modestbranding=1&cc_load_policy=1&fs=0&rel=0&iv_load_policy=3`}
              allow='autoplay; encrypted-media;'
              title={media.name}
            />
          </DefaultCase>
        </Switch>
        <div className='metadata-overlay'>
          <Row>
            <Conditional test={!serie}>
              <Trigger id={media.id} status={media.status} className={'embed'} />
            </Conditional>
            <span className='title'>{media.name}</span>
            <span className='release'>{media.releasedYear}</span>
          </Row>
          <Row>
            <span className='description'>{media.description}</span>
          </Row>
        </div>
        <Conditional test={serie}>
          <Gallery episodes={collection} />
        </Conditional>
      </Modal>
    </>
  )
}
