import React, { useState } from 'react'

import './media.scoped.scss'
import notFound from '../../assets/notfound.png'
import MediaExtension from './MediaExtension'
import Trigger from './Trigger'

export default function Media({ collection }) {
  const [show, setShow] = useState(false)
  const media = collection[0]
  const status = collection.length === 1 ? media.status : 'Multiple'

  return (
    <div className='media'>
      {media.hasThumbnail ? (
        <img className='thumbnail' src={`${process.env.REACT_APP_BACKEND_URI}/media/${media.id}/thumbnail`} />
      ) : (
        <>
          <div className='title'>{media.name}</div>
          <img className='thumbnail not-found' src={notFound} />
        </>
      )}
      <div
        className={`overlay ${status}`}
        onClick={() => {
          if (!show) setShow(true)
        }}
      >
        <Trigger id={media.id} status={status} />
        <MediaExtension collection={collection} show={show} setShow={setShow} />
      </div>
    </div>
  )
}
