import React from 'react'

import './media.scoped.scss'
import notFound from '../../assets/notfound.png'
import Overlay from './Overlay'
import Extension from './Extension'

export default function Media({ collection }) {
  const media = collection[0]

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
      <Overlay id={media.id} status={collection.length === 1 ? media.status : null}>
        <Extension collection={collection} />
      </Overlay>
    </div>
  )
}
