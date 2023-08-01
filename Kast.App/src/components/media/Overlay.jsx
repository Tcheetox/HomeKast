import React from 'react'

import './overlay.scoped.scss'
import { Case, Switch } from '../../hoc/'
import useMedia from '../../hooks/useMedia'
import { Play, Convert, Subtitles, Queued } from '../../assets/icons/index.js'

export default function Overlay({ id, status, children }) {
  const { startConversion, play } = useMedia(id)

  return (
    <div className={`overlay ${status}`}>
      <Switch test={status}>
        <Play value={'Playable'} onClick={play} />
        <Play value={'Streamable'} onClick={play} />
        <Case value={'MissingSubtitles'} onClick={startConversion}>
          <Convert />
          <Subtitles className='subtitles' />
        </Case>
        <Convert value={'Unplayable'} onClick={startConversion} />
        <Queued value={'Queued'} />
        <div value={'Converting'} className='rotate'>
          <Convert />
        </div>
      </Switch>
      {children ?? null}
    </div>
  )
}
