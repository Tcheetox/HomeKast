import React from 'react'

import './episode.scoped.scss'
import notFound from '../../assets/notfound.png'
import Trigger from './Trigger'
import { Conditional } from '../../hoc'

export default function Episode({ media }) {
  return (
    <div className='episode'>
      {media.hasThumbnail ? (
        <img className='thumbnail' src={`${process.env.REACT_APP_BACKEND_URI}/media/${media.id}/thumbnail`} />
      ) : (
        <>
          <div className='title'>{media.name}</div>
          <img className='thumbnail not-found' src={notFound} />
        </>
      )}
      <div className='overlay'>
        <Trigger status={media.status} id={media.id} className={'mini'} />

        <Conditional test={media.episode !== null}>
          <div className='indicator' title={media.episode.name}>
            {media.episode.indicator}
          </div>
        </Conditional>
      </div>
    </div>
  )
}
