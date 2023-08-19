import React, { useState } from 'react'

import './expand.scoped.scss'
import { Details } from '../../assets/icons'
import { Button, Modal, Row } from 'react-bootstrap'
import { Conditional } from '../../hoc/'
import Trigger from './Trigger'

// TODO: manage when no iframe ?
// TODO: isSerie is wrong test atm
// TODO: props passing is garbage atm

export default function Expand({ collection }) {
  const [show, setShow] = useState(false)
  const first = collection[0]
  const isSerie = collection.length > 1

  return (
    <>
      <Button className='btn-unstyle' onClick={() => setShow(p => !p)}>
        <Details />
      </Button>
      <Modal className='expand' show={show} onHide={() => setShow(false)} size='xl' centered>
        <Conditional test={first.youtubeEmbed !== null}>
          <iframe
            className='trailer'
            width='1140'
            height='641'
            src={`${first.youtubeEmbed}?autoplay=1&modestbranding=1&cc_load_policy=1&fs=0&rel=0&iv_load_policy=3`}
            allow='autoplay; encrypted-media;'
            title={first.name}
          />
          <div className='metadata-overlay'>
            <Row>
              <Conditional test={!isSerie}>
                <Trigger id={first.id} status={first.status} className={'embed'} />
              </Conditional>
              <span className='title'>{first.name}</span>
              <span className='release'>{first.releasedYear}</span>
            </Row>
            <Row>
              <span className='description'>{first.description}</span>
            </Row>
          </div>
        </Conditional>
      </Modal>
    </>
  )
}
