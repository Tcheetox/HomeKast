import React from 'react'

import './overlay.scoped.scss'
import Switch from '../../hoc/Switch'
import Case from '../../hoc/Case'
import useMedia from '../../hooks/useMedia'
import { Play, Convert, Subtitles, Queued } from '../../assets/icons/index.js'

export default function Overlay({ id, status, children }) {
  const { startConversion, play } = useMedia(id)

  return (
    <div className={`overlay ${status}`}>
      <Switch test={status}>
        <Play value={1} onClick={play} />
        <Case value={2} onClick={startConversion}>
          <Convert />
          <Subtitles className='subtitles' />
        </Case>
        <Convert value={3} onClick={startConversion} />
        <Queued value={4} />
        <Convert value={5} />
      </Switch>
      {children ?? null}
    </div>
  )
}
